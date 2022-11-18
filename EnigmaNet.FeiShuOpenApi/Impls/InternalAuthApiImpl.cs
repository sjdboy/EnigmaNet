using EnigmaNet.FeiShuOpenApi.Models.Auths;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Impls
{
    public class InternalAuthApiImpl : ApiBase, IInternalAuthApi
    {
        const string TenantApi = "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal";
        const string AppApi = "https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal";

        class AppAccessTokenResult : ResultBase
        {
            public string AppAccessToken { set; get; }
            public int Expire { get; set; }
        }

        class TenantAccessTokenResult : ResultBase
        {
            public string TenantAccessToken { set; get; }
            public int Expire { get; set; }
        }

        public async Task<AppAccessTokenModel> ApplyAppAccessTokenAsync(string appId, string appSecret)
        {
            var logger = LoggerFactory.CreateLogger<InternalAuthApiImpl>();

            var url = AppApi;

            var httpClient = GetClient();

            var requestContent = BuildContent(new
            {
                AppId = appId,
                AppSecret = appSecret,
            });

            if (logger.IsEnabled(LogLevel.Trace))
            {
                var text = await requestContent.ReadAsStringAsync();
                logger.LogTrace($"ApplyAppAccessTokenAsync,requestContent:{text}");
            }

            var response = await httpClient.PostAsync(url, requestContent);
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"ApplyAppAccessTokenAsync,url:{url} statusCode:{response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"ApplyAppAccessTokenAsync,url:{url} responseContent:{responseContent}");
            }

            var result = ReadResult<AppAccessTokenResult>(responseContent);

            ThrowExceptionIfError(result);

            return new AppAccessTokenModel
            {
                AppAccessToken = result.AppAccessToken,
                Expire = result.Expire,
            };
        }

        public async Task<TenantAccessTokenModel> ApplyTenantAccessTokenAsync(string appId, string appSecret)
        {
            var logger = LoggerFactory.CreateLogger<InternalAuthApiImpl>();

            var url = TenantApi;

            var httpClient = GetClient();

            var requestContent = BuildContent(new
            {
                AppId = appId,
                AppSecret = appSecret,
            });

            if (logger.IsEnabled(LogLevel.Trace))
            {
                var text = await requestContent.ReadAsStringAsync();
                logger.LogTrace($"ApplyTenantAccessTokenAsync,requestContent:{text}");
            }

            var response = await httpClient.PostAsync(url, requestContent);
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"ApplyTenantAccessTokenAsync,url:{url} statusCode:{response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"ApplyTenantAccessTokenAsync,url:{url} responseContent:{responseContent}");
            }

            var result = ReadResult<TenantAccessTokenResult>(responseContent);

            ThrowExceptionIfError(result);

            return new TenantAccessTokenModel
            {
                TenantAccessToken = result.TenantAccessToken,
                Expire = result.Expire,
            };
        }
    }
}
