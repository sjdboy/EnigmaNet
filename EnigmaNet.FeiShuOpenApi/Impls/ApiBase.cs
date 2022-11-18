using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Impls
{
    public abstract class ApiBase
    {
        const string HttpClientName = "feishuopenapi";

        Newtonsoft.Json.JsonSerializerSettings GetJsonSetting()
        {
            var setting = new Newtonsoft.Json.JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(),
                },
            };

            setting.Converters.Add(new StringEnumConverter(new SnakeCaseNamingStrategy()));

            return setting;
        }

        protected abstract class ResultBase
        {
            public int? Code { get; set; }
            public string Msg { get; set; }
        }

        protected class DataResult<T> : ResultBase where T : class
        {
            public T Data { get; set; }
        }

        protected DataResult<T> ReadDataResult<T>(string text) where T : class
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<DataResult<T>>(text, GetJsonSetting());
        }

        protected T ReadResult<T>(string text) where T : ResultBase
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text, GetJsonSetting());
        }

        protected string SerializeObject(object data)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(data, GetJsonSetting());
        }

        protected StringContent BuildContent(object data)
        {
            var json = SerializeObject(data);

            var content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            return content;
        }

        protected void ThrowExceptionIfError(ResultBase result)
        {
            if (result.Code != 0)
            {
                throw new FeiShuApiException(result.Code?.ToString(), result.Msg);
            }
        }

        protected HttpClient GetClient()
        {
            return HttpClientFactory.CreateClient(HttpClientName);
        }

        public ILoggerFactory LoggerFactory { get; set; }
        public IHttpClientFactory HttpClientFactory { get; set; }
    }
}
