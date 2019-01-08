using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;

using EnigmaNet.QCloud.CMQ.Models;

namespace EnigmaNet.QCloud.CMQ.Utils
{
    class CMQRequester
    {
        class PublishParamaters
        {
            public const string Action = "Action";
            public const string Timestamp = "Timestamp";
            public const string Nonce = "Nonce";
            public const string SecretId = "SecretId";
            public const string Signature = "Signature";
            public const string SignatureMethod = "SignatureMethod";
        }

        const string Path = "/v2/index.php";
        const string HttpMethod = HttpMethods.Post;
        Random _random = new Random();

        int GetTimestamp(DateTime dateTime)
        {
            return (int)(((dateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0))).TotalSeconds);
        }

        int CreateNonce()
        {
            return _random.Next(int.MaxValue);
        }

        string GetSignture(
          string httpMethod,
          string host,
          string path,
          IDictionary<string, string> data,
          string secrectKey,
          string signtureMethod)
        {
            var itemList = data
                .OrderBy(m => m.Key, StringComparer.Ordinal)
                .Select(m => $"{m.Key.Replace("_", ".")}={m.Value}");

            var signString = $"{httpMethod}{host}{path}?{string.Join("&", itemList)}";

            var logger = LoggerFactory?.CreateLogger<CMQRequester>();

            logger?.LogDebug($"GetSignture,signString:{signString} secrectKey:{secrectKey}");

            if (signtureMethod == SignatureMethods.HmacSHA1)
            {
                using (var mac = new HMACSHA1(Encoding.UTF8.GetBytes(secrectKey)))
                {
                    var hash = mac.ComputeHash(Encoding.UTF8.GetBytes(signString));
                    return Convert.ToBase64String(hash);
                }
            }
            else if (signtureMethod == SignatureMethods.HmacSHA256)
            {
                using (var mac = new HMACSHA256(Encoding.UTF8.GetBytes(secrectKey)))
                {
                    var hash = mac.ComputeHash(Encoding.UTF8.GetBytes(signString));
                    return Convert.ToBase64String(hash);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(signtureMethod));
            }
        }

        public string SignatureMethod { get; set; }
        public string SecrectId { get; set; }
        public string SecrectKey { get; set; }
        public string RegionHost { get; set; }
        public bool IsHttps { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }

        string GetHost(string action)
        {
            switch (action)
            {
                case Utils.Actions.CreateQueue:
                case Utils.Actions.SendMessage:
                case Utils.Actions.ReceiveMessage:
                case Utils.Actions.DeleteMessage:
                    return $"cmq-queue-{RegionHost}";

                case Utils.Actions.CreateTopic:
                case Utils.Actions.Subscribe:
                case Utils.Actions.PublishMessage:
                    return $"cmq-topic-{RegionHost}";

                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        async Task<T> RequestAsync<T>(string action, string httpMethod, IDictionary<string, string> parameters)
            where T : ResultModel
        {
            if (string.IsNullOrEmpty(action))
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (string.IsNullOrEmpty(httpMethod))
            {
                httpMethod = HttpMethods.Post;
            }

            var logger = LoggerFactory?.CreateLogger<CMQRequester>();

            var timestamp = GetTimestamp(DateTime.UtcNow).ToString();
            var nonce = CreateNonce().ToString();

            var host = GetHost(action);

            parameters.Add(PublishParamaters.Action, action);
            parameters.Add(PublishParamaters.Timestamp, timestamp);
            parameters.Add(PublishParamaters.Nonce, nonce);
            parameters.Add(PublishParamaters.SecretId, SecrectId);
            parameters.Add(PublishParamaters.SignatureMethod, SignatureMethod);
            //signture获取的顺序要先
            var signture = GetSignture(httpMethod, host, Path, parameters, SecrectKey, SignatureMethod);
            parameters.Add(PublishParamaters.Signature, signture);

            var url = $"{(IsHttps ? "https" : "http")}://{host}{Path}";
            var httpClient = new HttpClient();

            var requestData = string.Join("&", parameters.Select(m => $"{m.Key}={HttpUtility.UrlEncode(m.Value)}"));

            logger?.LogDebug($"start to request,method:{httpMethod} requestData:{requestData}");

            WebRequest webRequest;
            if (httpMethod == HttpMethods.Post)
            {
                webRequest = WebRequest.Create(url);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                var requestBytes = Encoding.UTF8.GetBytes(requestData);
                using (var requestStream = await webRequest.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(requestBytes, 0, requestBytes.Length);
                }
            }
            else if (httpMethod == HttpMethods.Get)
            {
                webRequest = WebRequest.Create($"{url}?{requestData}");
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(httpMethod));
            }

            var response = (HttpWebResponse)await webRequest.GetResponseAsync();
            logger?.LogDebug($"response StatusCode:{response.StatusCode}");
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException($"cmq request error,status code:{response.StatusCode}");
            }

            string responseContent;
            using (var responseStream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    responseContent = await reader.ReadToEndAsync();
                }
            }

            logger?.LogDebug($"response,method:{httpMethod} requestData:{requestData} ;response, StatusCode:{response.StatusCode} Content:{responseContent}");

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(
                responseContent,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                    {
                        NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy(),
                    }
                });
        }

        public async Task<T> GetAsync<T>(string action, IDictionary<string, string> parameters)
            where T : ResultModel
        {
            return await RequestAsync<T>(action, HttpMethods.Get, parameters);
        }

        public async Task<T> PostAsync<T>(string action, IDictionary<string, string> parameters)
            where T : ResultModel
        {
            return await RequestAsync<T>(action, HttpMethods.Post, parameters);
        }
    }
}
