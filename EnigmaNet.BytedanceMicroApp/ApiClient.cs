using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using EnigmaNet.BytedanceMicroApp.Models;
using EnigmaNet.Extensions;
using System.Net.Http;

namespace EnigmaNet.BytedanceMicroApp
{
    public class ApiClient : IApiClient
    {
        abstract class DataBase
        {
            public int errcode { get; set; }
            public string errmsg { get; set; }
        }

        class JsCode2SessionModel : DataBase
        {
            public string session_key { get; set; }
            public string openid { get; set; }
            public string anonymous_openid { get; set; }
        }

        const string Api = "https://developer.toutiao.com";

        void ThrowExceptionIfError(DataBase result)
        {
            if (result.errcode != 0)
            {
                throw new BytedanceMicroAppException(result.errcode, $"errcode({result.errcode})" + result.errmsg);
            }
        }

        public ILoggerFactory LoggerFactory { get; set; }

        public async Task<JsCode2SessionResult> JsCode2SessionAsync(string appId, string secret, string code, string anonymousCode)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/api/apps/jscode2session"
               .AddQueryParam("appid", appId ?? string.Empty)
               .AddQueryParam("secret", secret ?? string.Empty)
               .AddQueryParam("code", code ?? string.Empty)
               .AddQueryParam("anonymous_code", anonymousCode ?? string.Empty);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"JsCode2SessionAsync,url:{url}");
            }

            JsCode2SessionModel result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"JsCode2SessionAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"JsCode2SessionAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<JsCode2SessionModel>();
            }

            ThrowExceptionIfError(result);

            return new JsCode2SessionResult
            {
                SessionKey = result.session_key,
                OpenId = result.openid,
                AnonymousOpenId = result.anonymous_openid,
            };
        }
    }
}
