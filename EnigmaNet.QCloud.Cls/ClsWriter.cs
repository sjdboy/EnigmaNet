using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Web;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using Google.Protobuf;

using Cls;
using EnigmaNet.Exceptions;
using EnigmaNet.Utils;
using EnigmaNet.Extensions;

namespace EnigmaNet.QCloud.Cls
{
    class ClsWriter
    {
        const int FirstWaitSeconds = 2;
        const int WaitSeconds = 5;
        const int ErrorWaitSeconds = 10;
        const int BatchAmount = 100;
        const int UploadBufferSize = 1024 * 1024;
        ConcurrentBag<LogInfo> _logBags = new ConcurrentBag<LogInfo>();
        ClsOptions _settings;
        ILoggerFactory _loggerFactory;
        bool _stopSync = false;

        string _hostIpOrName;

        string HmacSha1(string key, string text)
        {
            using (var mac = new System.Security.Cryptography.HMACSHA1(Encoding.UTF8.GetBytes(key)))
            {
                return mac.ComputeHash(Encoding.UTF8.GetBytes(text)).Aggregate(string.Empty, (s, e) => { return s + e.ToString("x2"); }, m => m);
            }
        }

        string Sha1Hex(string text)
        {
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                return sha1.ComputeHash(Encoding.UTF8.GetBytes(text)).Aggregate(string.Empty, (s, e) => { return s + e.ToString("x2"); }, m => m);
            }
        }

        string UrlEncode(string temp, Encoding encoding)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < temp.Length; i++)
            {
                string t = temp[i].ToString();
                string k = HttpUtility.UrlEncode(t, encoding);
                if (t == k)
                {
                    stringBuilder.Append(t);
                }
                else
                {
                    stringBuilder.Append(k.ToUpper());
                }
            }
            return stringBuilder.ToString();
        }

        string CreateAuthorization(
            string secretId, string secretKey,
            string path, HttpMethod httpMethod,
            long startDateTime, long endDateTime,
            Dictionary<string, string> query,
            Dictionary<string, string> header
            )
        {
            var method = httpMethod.Method.ToLower();
            var httpAlgorithm = "sha1";
            var qSignTime = $"{startDateTime};{endDateTime}";

            string signture;
            {
                string queryString;
                if (query?.Count > 0)
                {
                    queryString = string.Join(
                        "&",
                        query.ToDictionary(m => m.Key.ToLower(), m => m.Value).OrderBy(m => m.Key).Select(m => $"{m.Key}={ UrlEncode(m.Value, Encoding.UTF8)}"));
                }
                else
                {
                    queryString = string.Empty;
                }

                string headString;
                if (header?.Count > 0)
                {
                    headString = string.Join(
                        "&",
                        header.ToDictionary(m => m.Key.ToLower(), m => m.Value).OrderBy(m => m.Key).Select(m => $"{m.Key}={ UrlEncode(m.Value, Encoding.UTF8)}"));
                }
                else
                {
                    headString = string.Empty;
                }

                var singKey = HmacSha1(secretKey, qSignTime);

                var httpRequestInfo = $"{method}\n{path}\n{queryString}\n{headString}\n";

                var httpRequestInfoHash = Sha1Hex(httpRequestInfo);

                var stringToSign = $"{httpAlgorithm}\n{qSignTime}\n{httpRequestInfoHash}\n";

                signture = HmacSha1(singKey, stringToSign);
            }

            var authData = new Dictionary<string, string>();
            {
                string queryString;
                if (query?.Count > 0)
                {
                    queryString = string.Join(
                        ";",
                        query.Select(m => m.Key.ToLower()).OrderBy(m => m));
                }
                else
                {
                    queryString = string.Empty;
                }

                string headString;
                if (header?.Count > 0)
                {
                    headString = string.Join(
                        ";",
                        header.Select(m => m.Key.ToLower()).OrderBy(m => m));
                }
                else
                {
                    headString = string.Empty;
                }

                authData.Add("q-sign-algorithm", httpAlgorithm);
                authData.Add("q-ak", secretId);
                authData.Add("q-sign-time", qSignTime);
                authData.Add("q-key-time", qSignTime);
                authData.Add("q-header-list", headString);
                authData.Add("q-url-param-list", queryString);
                authData.Add("q-signature", signture);
            }

            return string.Join("&", authData.Select(m => $"{m.Key}={m.Value}"));
        }

        StringBuilder GetExceptionMessage(Exception exception)
        {
            var content = new StringBuilder();

            if (exception.InnerException != null)
            {
                content.Append(GetExceptionMessage(exception.InnerException));
                content.Append("|");
            }

            content.Append($"{exception.Message} > {exception.StackTrace}");

            return content;
        }

        void SendLogs(List<LogInfo> logInfos)
        {
            if (!(logInfos?.Count > 0))
            {
                throw new ArgumentNullException(nameof(logInfos));
            }

            if (string.IsNullOrEmpty(_settings.Host) ||
                string.IsNullOrEmpty(_settings.TopicId) ||
                string.IsNullOrEmpty(_settings.SecretId) ||
                string.IsNullOrEmpty(_settings.SecretKey))
            {
                return;
            }

            var groupList = new LogGroupList();

            foreach (var logGroup in logInfos.GroupBy(m => m.CategoryName))
            {
                var group = new LogGroup();
                group.Filename = logGroup.Key;
                group.Source = _hostIpOrName;

                foreach (var logInfo in logGroup)
                {
                    var log = new Log();
                    log.Time = DateTimeUtils.ToUnixTime2(logInfo.DateTime);

                    if (logInfo.EventId.Id != 0)
                    {
                        log.Contents.Add(new Log.Types.Content
                        {
                            Key = "EventId",
                            Value = logInfo.EventId.Id.ToString(),
                        });
                    }

                    if (!string.IsNullOrEmpty(logInfo.EventId.Name))
                    {
                        log.Contents.Add(new Log.Types.Content
                        {
                            Key = "EventIdName",
                            Value = logInfo.EventId.Name,
                        });
                    }

                    log.Contents.Add(new Log.Types.Content
                    {
                        Key = "LogLevel",
                        Value = logInfo.LogLevel.ToString(),
                    });

                    log.Contents.Add(new Log.Types.Content
                    {
                        Key = "Content",
                        Value = logInfo.Content,
                    });

                    if (logInfo.Exception != null)
                    {
                        log.Contents.Add(new Log.Types.Content
                        {
                            Key = "Exception",
                            Value = GetExceptionMessage(logInfo.Exception).ToString()
                        });
                    }

                    if (!string.IsNullOrEmpty(logInfo.TraceId))
                    {
                        log.Contents.Add(new Log.Types.Content
                        {
                            Key = "TraceId",
                            Value = logInfo.TraceId
                        });
                    }

                    if (!string.IsNullOrEmpty(logInfo.SpanId))
                    {
                        log.Contents.Add(new Log.Types.Content
                        {
                            Key = "SpanId",
                            Value = logInfo.SpanId
                        });
                    }

                    if (!string.IsNullOrEmpty(logInfo.ParentId))
                    {
                        log.Contents.Add(new Log.Types.Content
                        {
                            Key = "ParentId",
                            Value = logInfo.ParentId
                        });
                    }

                    group.Logs.Add(log);
                }

                groupList.LogGroupList_.Add(group);
            }

            byte[] bytes;
            using (MemoryStream stream = new MemoryStream())
            {
                using (CodedOutputStream output = new CodedOutputStream(stream))
                {
                    groupList.WriteTo(output);
                }

                bytes = stream.ToArray();
            }

            var path = "/structuredlog";
            var queryString = $"topic_id={_settings.TopicId}";
            var url = $"https://{_settings.Host}{path}?{queryString}";

            var query = new Dictionary<string, string>();
            var header = new Dictionary<string, string>();
            query.Add("topic_id", _settings.TopicId);
            header.Add("host", _settings.Host);
            header.Add("Content-Type", "application/x-protobuf");

            var startTime = DateTimeUtils.ToUnixTime2(DateTime.Now);
            var endTime = DateTimeUtils.ToUnixTime2(DateTime.Now.AddSeconds(30));

            var authorization = CreateAuthorization(
                _settings.SecretId,
                _settings.SecretKey,
                path,
                HttpMethod.Post,
                startTime, endTime,
                query, header);

            var request = WebRequest.Create(url);
            {
                request.Headers.Add("Authorization", authorization);
                request.Headers.Add("Content-Type", "application/x-protobuf");
                request.Method = "Post";

                var requestStream = request.GetRequestStream();

                var bufferSize = UploadBufferSize;
                for (var i = 0; i < bytes.Length; i += bufferSize)
                {
                    int size;
                    if ((bytes.Length - i) > bufferSize)
                    {
                        size = bufferSize;
                    }
                    else
                    {
                        size = bytes.Length - i;
                    }

                    requestStream.Write(bytes, i, size);
                }

                HttpWebResponse response;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException webException)
                {
                    response = (HttpWebResponse)webException.Response;

                    if (response == null)
                    {
                        throw;
                    }
                }

                if (!response.StatusCode.ToString("D").StartsWith("2"))
                {
                    string errorContent;

                    using (var stream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            errorContent = reader.ReadToEnd();
                        }
                    }

                    throw new Exception($"上传日志出错,code:{response.StatusCode} con:{errorContent}");
                }
            }
        }

        void SyncTask()
        {
            Thread.CurrentThread.Join(1000 * FirstWaitSeconds);

            var logger = _loggerFactory.CreateLogger<ClsWriter>();

            logger.LogInformation("SyncTask start");

            while (true)
            {
                if (_stopSync)
                {
                    return;
                }

                var logInfos = new List<LogInfo>();

                while (logInfos.Count < BatchAmount)
                {
                    if (_logBags.TryTake(out LogInfo logInfo))
                    {
                        logInfos.Add(logInfo);
                    }
                    else
                    {
                        break;
                    }
                }

                if (logInfos?.Count > 0)
                {
                    try
                    {
                        SendLogs(logInfos);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"send logs error,log amount:{logInfos.Count}");

                        //save
                        foreach(var logInfo in logInfos)
                        {
                            _logBags.Add(logInfo);
                        }

                        Thread.CurrentThread.Join(1000 * ErrorWaitSeconds);
                    }
                }
                else
                {
                    //empty
                    Thread.CurrentThread.Join(1000 * WaitSeconds);
                }
            }
        }

        public void StopSyncTaskAndSendAllLog()
        {
            var logger = _loggerFactory.CreateLogger<ClsWriter>();

            logger.LogInformation("StopSyncTaskAndSendAllLog start");

            _stopSync = true;

            int sendedAmount = 0;

            while (true)
            {
                var logInfos = new List<LogInfo>();

                while (logInfos.Count < BatchAmount)
                {
                    if (_logBags.TryTake(out LogInfo logInfo))
                    {
                        logInfos.Add(logInfo);
                    }
                    else
                    {
                        break;
                    }
                }

                if (logInfos?.Count > 0)
                {
                    try
                    {
                        SendLogs(logInfos);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SendAllLog error,ex:{ex.Message} {ex.StackTrace}");
                    }
                }
                else
                {
                    return;
                }

                sendedAmount += logInfos.Count;

                if (sendedAmount >= 1000)
                {
                    //send to much => stop
                    return;
                }
            }
        }

        public void StartSyncTask()
        {
            var thread = new Thread(SyncTask);
            thread.IsBackground = true;
            thread.Start();
        }

        public ClsWriter(IOptionsMonitor<ClsOptions> options, ILoggerFactory loggerFactory)
        {
            _settings = options.CurrentValue;
            _loggerFactory = loggerFactory;
            options.OnChange(setting =>
            {
                _settings = setting;
            });

            var ipv4s = Utils.IpUtils.GetIpv4s();
            if (ipv4s?.Count > 0)
            {
                _hostIpOrName = string.Join(";", ipv4s);
            }
            else
            {
                _hostIpOrName = Environment.MachineName;
            }
        }

        public void AddLogInfo(LogInfo logInfo)
        {
            _logBags.Add(logInfo);
        }
    }
}
