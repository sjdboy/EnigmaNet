using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using EnigmaNet.FeiShuOpenApi.Models.Tenants;

namespace EnigmaNet.FeiShuOpenApi.Impls
{
    public class TenantApiImpl : ApiBase, ITenantApi
    {
        const string Api = "https://open.feishu.cn/open-apis/tenant/v2/tenant/query";

        class TenantData
        {
            public TenantModel Tenant { get; set; }
        }

        public async Task<TenantModel> GetAsync(string tenantAccessToken)
        {
            var logger = LoggerFactory.CreateLogger<TenantApiImpl>();

            var url = Api;

            var httpClient = GetClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tenantAccessToken);

            var response = await httpClient.GetAsync(url);
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetAsync,url:{url} statusCode:{response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetAsync,url:{url} responseContent:{responseContent}");
            }

            var result = ReadDataResult<TenantData>(responseContent);

            ThrowExceptionIfError(result);

            return result.Data.Tenant;
        }
    }
}
