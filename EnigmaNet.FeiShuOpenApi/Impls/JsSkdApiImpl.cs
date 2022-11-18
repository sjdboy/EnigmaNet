using EnigmaNet.FeiShuOpenApi.Models.Authens;
using EnigmaNet.FeiShuOpenApi.Models.JsSdks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Impls
{
    public class JsSkdApiImpl : ApiBase, IJsSdkApi
    {
        const string Api = "https://open.feishu.cn/open-apis/jssdk/ticket/get";

        public async Task<TicketModel> GetTicketAsync(string accessToken)
        {
            var logger = LoggerFactory.CreateLogger<JsSkdApiImpl>();

            var url = Api;

            var httpClient = GetClient();

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.PostAsync(url, null);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetTicketAsync,url:{url} statusCode:{response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetTicketAsync,url:{url} content:{content}");
            }

            var result = ReadDataResult<TicketModel>(content);

            ThrowExceptionIfError(result);

            return result.Data;
        }

    }
}
