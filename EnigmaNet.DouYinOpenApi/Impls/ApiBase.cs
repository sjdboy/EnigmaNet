using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EnigmaNet.DouYinOpenApi.Impls
{
    public abstract class ApiBase
    {
        public const string HttpClientName = "douyinopenapi";

        protected const string Api = "https://open.douyin.com";

        protected abstract class DataBase
        {
            public int error_code { get; set; }
            public string description { get; set; }
        }

        protected class DefaultResultModel<T> where T : DataBase
        {
            public T data { get; set; }
        }

        protected void ThrowExceptionIfError(DataBase result)
        {
            if (result.error_code != 0)
            {
                throw new DouYinApiException(result.error_code, $"errorCode({result.error_code})" + result.description);
            }
        }

        protected HttpClient GetClient()
        {
            return HttpClientFactory.CreateClient(HttpClientName);
        }

        protected bool? GetGender(int gender)
        {
            switch (gender)
            {
                case 0:
                    return null;
                case 1:
                    return true;
                case 2:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gender));
            }
        }

        public ILoggerFactory LoggerFactory { get; set; }
        public IHttpClientFactory HttpClientFactory { get; set; }

    }
}
