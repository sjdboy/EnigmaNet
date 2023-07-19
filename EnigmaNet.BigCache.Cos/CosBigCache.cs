using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

using EnigmaNet.BigCache.Cos.Options;
using Newtonsoft.Json;
using COSXML.Model.Object;
using COSXML.CosException;

namespace EnigmaNet.BigCache.Cos
{
    public class CosBigCache : IBigCache
    {
        COSXML.CosXmlServer CreateCosServer()
        {
            var builder = new COSXML.CosXmlConfig.Builder().IsHttps(true);
            builder = builder.SetRegion(CosOptionsValue.Region);

            var config = builder.Build();

            var cosCredentialProvider = new COSXML.Auth.DefaultQCloudCredentialProvider(CosOptionsValue.SecretId, CosOptionsValue.SecretKey, 600);

            return new COSXML.CosXmlServer(config, cosCredentialProvider);
        }

        public IOptionsMonitor<CosBigCacheOptions> CosOptions { get; set; }

        CosBigCacheOptions CosOptionsValue
        {
            get
            {
                return CosOptions.CurrentValue;
            }
        }

        public CosBigCache(IOptionsMonitor<CosBigCacheOptions> cosOptions)
        {
            CosOptions = cosOptions;
        }

        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var cosXml = CreateCosServer();

            var appId = CosOptionsValue.AppId;
            var bucket = CosOptionsValue.Bucket;

            GetObjectBytesResult result;
            try
            {
                result = await cosXml.ExecuteAsync<GetObjectBytesResult>(new GetObjectBytesRequest($"{bucket}-{appId}", key));
            }
            catch (CosServerException ex)
            {
                if (ex.statusCode == 404)
                {
                    return default;
                }
                else
                {
                    throw;
                }
            }

            if (!result.IsSuccessful())
            {
                return default;
            }

            var bytes = result.content;

            var content = System.Text.Encoding.UTF8.GetString(bytes);

            if (string.IsNullOrEmpty(content))
            {
                return default;
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)content;
            }
            else
            {
                var model = JsonConvert.DeserializeObject<T>(content);

                return model;
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var cosXml = CreateCosServer();

            var appId = CosOptionsValue.AppId;
            var bucket = CosOptionsValue.Bucket;

            await cosXml.ExecuteAsync<DeleteObjectResult>(new DeleteObjectRequest($"{bucket}-{appId}", key));
        }

        public async Task SetAsync<T>(string key, T obj)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string content;
            if (obj == null)
            {
                content = string.Empty;
            }
            else
            {
                if (typeof(T) == typeof(string))
                {
                    content = obj.ToString();
                }
                else
                {
                    content = JsonConvert.SerializeObject(obj);
                }
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(content);

            var appId = CosOptionsValue.AppId;
            var bucket = CosOptionsValue.Bucket;

            var request = new PutObjectRequest($"{bucket}-{appId}", key, bytes);

            var cosXml = CreateCosServer();

            await cosXml.ExecuteAsync<PutObjectResult>(request);
        }
    }
}