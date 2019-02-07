using EnigmaNet.Exceptions;
using EnigmaNet.Extensions;
using EnigmaNet.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.QCloud.Cos.Impl
{
    public class CosClient : ICosClient
    {
        ILogger _log;
        ILogger Logger
        {
            get
            {
                if (_log == null)
                {
                    _log = LoggerFactory.CreateLogger<CosClient>();
                }
                return _log;
            }
        }

        const int DefaultExpiredSeconds = 60 * 60;
        const int CosApiValidMinutes = 10;
        const int UploadBufferSize = 1024 * 1024;

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

        string CreateAuthorization(string path, HttpMethod httpMethod, DateTime startDateTime, DateTime endDateTime)
        {
            var method = httpMethod.Method.ToLower();
            var httpAlgorithm = "sha1";

            string qSignTime;
            {
                var startTime = Math.Floor(DateTimeUtils.ToUnixTime(startDateTime));
                var endTime = Math.Floor(DateTimeUtils.ToUnixTime(endDateTime));
                qSignTime = $"{startTime};{endTime}";
            }

            string signture;
            {
                var httpStringHttpHeaders2 = string.Empty;
                var httpStringHttpParameters2 = string.Empty;

                var signTimeSign = HmacSha1(SecretKey, qSignTime);
                var httpStringSha1 = Sha1Hex($"{method}\n{path}\n{httpStringHttpParameters2}\n{httpStringHttpHeaders2}\n");
                signture = HmacSha1(signTimeSign, $"{httpAlgorithm}\n{qSignTime}\n{httpStringSha1}\n");
            }

            var authData = new Dictionary<string, string>();
            {
                var httpStringHttpHeaders = string.Empty;
                var httpStringHttpParameters = string.Empty;

                authData.Add("q-sign-algorithm", httpAlgorithm);
                authData.Add("q-ak", SecretId);
                authData.Add("q-sign-time", qSignTime);
                authData.Add("q-key-time", qSignTime);
                authData.Add("q-header-list", httpStringHttpHeaders);
                authData.Add("q-url-param-list", httpStringHttpParameters);
                authData.Add("q-signature", signture);
            }

            return string.Join("&", authData.Select(m => $"{m.Key}={m.Value}"));
        }

        string GetDownloadAuthoritionKey(string filePath, DateTime? expired)
        {
            var bucket = Bucket;
            var appId = AppId;
            var secretId = SecretId;
            var secretKey = SecretKey;

            int e;
            if (expired.HasValue)
            {
                e = Convert.ToInt32(Math.Floor(DateTimeUtils.ToUnixTime(expired.Value)));
            }
            else
            {
                e = 0;
            }

            string fileId = filePath;

            var rdm = new Random().Next(Int32.MaxValue);
            var plainText = "a=" + appId + "&k=" + secretId + "&e=" + e + "&t=" + Convert.ToInt32(Math.Floor(DateTimeUtils.ToUnixTime(DateTime.Now))) + "&r=" + rdm + "&f=" + fileId + "&b=" + bucket;

            using (var mac = new System.Security.Cryptography.HMACSHA1(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hash = mac.ComputeHash(Encoding.UTF8.GetBytes(plainText));
                var pText = Encoding.UTF8.GetBytes(plainText);
                var all = new byte[hash.Length + pText.Length];
                Array.Copy(hash, 0, all, 0, hash.Length);
                Array.Copy(pText, 0, all, hash.Length, pText.Length);
                return Convert.ToBase64String(all);
            }
        }

        string GetCDNHost()
        {
            //ny01-1253908385.file.myqcloud.com
            return $"{Bucket}-{AppId}.file.myqcloud.com";
        }

        string GetImageCDNHost()
        {
            //ny01-1253908385.image.myqcloud.com
            return $"{Bucket}-{AppId}.image.myqcloud.com";
        }

        string GetCosHost()
        {
            //ny01-1253908385.cos.ap-shanghai.myqcloud.com
            return $"{Bucket}-{AppId}.cos.{Region}.myqcloud.com";
        }

        public string SecretId { get; set; }
        public string SecretKey { get; set; }
        public string Bucket { get; set; }
        public string AppId { get; set; }
        public string Region { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }

        public Task CopyObjectAsync(string sourcePath, string targetPath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentNullException(nameof(sourcePath));
            }

            if (!sourcePath.StartsWith("/"))
            {
                throw new ArgumentException($"SourcePath is not start with /", nameof(sourcePath));
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                throw new ArgumentNullException(nameof(targetPath));
            }

            if (!targetPath.StartsWith("/"))
            {
                throw new ArgumentException($"TargetPath is not start with /", nameof(targetPath));
            }

            if (sourcePath == targetPath)
            {
                throw new ArgumentException($"targetPath == sourcePath");
            }

            var host = GetCosHost();
            var path = targetPath;
            var authorization = CreateAuthorization(path, HttpMethod.Put, DateTime.Now, DateTime.Now.AddMinutes(CosApiValidMinutes));
            var url = $"https://{host}{path}";

            Logger.LogDebug($"prepare to copy object,url:{url} source:{sourcePath}");

            var request = WebRequest.Create(url);
            {
                request.Headers.Add("Authorization", authorization);
                request.Headers.Add("x-cos-copy-source", $"{host}{sourcePath}");
                request.Method = "PUT";

                var response = (HttpWebResponse)await request.GetResponseAsync();
                if (!response.StatusCode.ToString("D").StartsWith("2"))
                {
                    var errorContent = await response.ReadAsStringAsync();

                    Logger.LogError($"copy object fail,url:{url} source:{sourcePath} response:{response.StatusCode} {errorContent}");

                    throw new BizException("复制云文件出错");
                }
            }
        }

        public Task DeleteObjectAsync(string path)
        {
            throw new NotImplementedException();
        }

        public string GetObjectAccessUrl(string path)
        {
            return GetObjectAccessUrl(LineType.Cos, path);
        }

        public string GetObjectAccessUrlWithAuthorization(string path, TimeSpan? expiredTimeSpan)
        {
            return GetObjectAccessUrlWithAuthorization(LineType.Cos, path, expiredTimeSpan);
        }

        public string GetObjectAccessUrl(LineType lineType, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.StartsWith("/"))
            {
                throw new ArgumentException($"path is not start with /", nameof(path));
            }

            string host;

            switch (lineType)
            {
                case LineType.Cos:
                    host = GetCosHost();
                    break;
                case LineType.CDN:
                    host = GetCDNHost();
                    break;
                case LineType.ImageCDN:
                    host = GetImageCDNHost();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lineType));
            }

            return $"https://{host}{path}";
        }

        public string GetObjectAccessUrlWithAuthorization(LineType lineType, string path, TimeSpan? expiredTimeSpan)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.StartsWith("/"))
            {
                throw new ArgumentException($"path is not start with /", nameof(path));
            }

            string host;
            switch (lineType)
            {
                case LineType.Cos:
                    host = GetCosHost();
                    break;
                case LineType.CDN:
                    host = GetCDNHost();
                    break;
                case LineType.ImageCDN:
                    host = GetImageCDNHost();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lineType));
            }

            var timeSpan = expiredTimeSpan ?? TimeSpan.FromSeconds(DefaultExpiredSeconds);
            var authorization = CreateAuthorization(path, HttpMethod.Get, DateTime.Now, DateTime.Now.Add(timeSpan));
            return $"https://{host}{path}?{authorization}";
        }

        public void GetObjectUploadInfo(string path, out string putUrl, out string authorization)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.StartsWith("/"))
            {
                throw new ArgumentException($"path is not start with /", nameof(path));
            }

            var host = GetCosHost();
            authorization = CreateAuthorization(path, HttpMethod.Put, DateTime.Now, DateTime.Now.AddMinutes(CosApiValidMinutes));
            putUrl = $"https://{host}{path}";
        }

        public async Task<long> GetObjectContentLengthAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.StartsWith("/"))
            {
                throw new ArgumentException($"path is not start with /", nameof(path));
            }

            var host = GetCosHost();
            var authorization = CreateAuthorization(path, HttpMethod.Head, DateTime.Now, DateTime.Now.AddMinutes(CosApiValidMinutes));
            var url = $"https://{host}{path}";

            Logger.LogDebug($"prepare to head object:{url}");

            var request = WebRequest.Create(url);
            {
                request.Headers.Add("Authorization", authorization);
                request.Method = "HEAD";
                var response = (HttpWebResponse)await request.GetResponseAsync();
                if (!response.StatusCode.ToString("D").StartsWith("2"))
                {
                    var errorContent = await response.ReadAsStringAsync();

                    Logger.LogError($"head object fail,url:{url} response:{response.StatusCode} {errorContent}");

                    throw new BizException("获取文件信息出错");
                }

                if (response.ContentLength <= 0)
                {
                    throw new BizException("获取文件信息出错,返回文件大小小于等于0");
                }

                return response.ContentLength;
            }
        }

        public async Task UploadObjectAsync(byte[] fileContent, string path, TimeSpan? expiredTimeSpan)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.StartsWith("/"))
            {
                throw new ArgumentException($"path is not start with /", nameof(path));
            }


            var host = GetCosHost();
            var expiredDateTime = expiredTimeSpan.HasValue ? DateTime.Now.Add(expiredTimeSpan.Value) : DateTime.Now.AddMinutes(CosApiValidMinutes);
            var authorization = CreateAuthorization(path, HttpMethod.Put, DateTime.Now, expiredDateTime);
            var url = $"https://{host}{path}";

            Logger.LogDebug($"prepare to copy object,url:{url} path:{path}");

            var request = WebRequest.Create(url);
            {
                request.Headers.Add("Authorization", authorization);
                request.Method = "PUT";

                var requestStream = await request.GetRequestStreamAsync();

                var bufferSize = UploadBufferSize;
                for (var i = 0; i < fileContent.Length; i += bufferSize)
                {
                    var size = fileContent.Length - bufferSize * i;
                    if (size > bufferSize)
                    {
                        size = bufferSize;
                    }

                    await requestStream.WriteAsync(fileContent, i, size);
                }

                var response = (HttpWebResponse)await request.GetResponseAsync();
                if (!response.StatusCode.ToString("D").StartsWith("2"))
                {
                    var errorContent = await response.ReadAsStringAsync();

                    Logger.LogError($"copy object fail,url:{url} path:{path} response:{response.StatusCode} {errorContent}");

                    throw new BizException("上传文件出错");
                }
            }
        }

        public Task<UploadInfoModel> GetObjectUploadInfo(HttpMethod httpMethod, string path, TimeSpan? expiredTimeSpan = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.StartsWith("/"))
            {
                throw new ArgumentException($"path is not start with /", nameof(path));
            }

            if (!expiredTimeSpan.HasValue)
            {
                expiredTimeSpan = TimeSpan.FromMinutes(CosApiValidMinutes);
            }

            return Task.FromResult(new UploadInfoModel
            {
                Bucket = Bucket,
                AppId = AppId,
                Region = Region,
                UploadUrl = $"https://{GetCosHost()}{path}",
                Authorization = CreateAuthorization(path, httpMethod, DateTime.Now, DateTime.Now.Add(expiredTimeSpan.Value)),
            });
        }
    }
}
