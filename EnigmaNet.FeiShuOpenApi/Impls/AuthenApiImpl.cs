using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using EnigmaNet.FeiShuOpenApi.Models.Authens;
using EnigmaNet.FeiShuOpenApi.Models.Auths;
using EnigmaNet.Extensions;

namespace EnigmaNet.FeiShuOpenApi.Impls
{
    public class AuthenApiImpl : ApiBase, IAuthenApi
    {
        const string AccessTokenApi = "https://open.feishu.cn/open-apis/authen/v1/access_token";
        const string UserInfoApi = "https://open.feishu.cn/open-apis/authen/v1/user_info";

        public async Task<AccessTokenModel> GetAccessTokenAsync(string appAccessToken, string code)
        {
            var logger = LoggerFactory.CreateLogger<AuthenApiImpl>();

            var url = AccessTokenApi;

            var httpClient = GetClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", appAccessToken);

            var requestContent = BuildContent(new
            {
                GrantType = "authorization_code",
                Code = code,
            });

            if (logger.IsEnabled(LogLevel.Trace))
            {
                var text = await requestContent.ReadAsStringAsync();
                logger.LogTrace($"GetAccessTokenAsync,requestContent:{text}");
            }

            var response = await httpClient.PostAsync(url, requestContent);
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetAccessTokenAsync,url:{url} statusCode:{response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetAccessTokenAsync,url:{url} responseContent:{responseContent}");
            }

            var result = ReadDataResult<AccessTokenModel>(responseContent);

            ThrowExceptionIfError(result);

            return result.Data;
        }

        public async Task<string> GetAuthUrlAsync(string redirectUrl, string appId, string state)
        {
            var url = "https://open.feishu.cn/open-apis/authen/v1/index"
                .AddQueryParam("redirect_uri",redirectUrl)
                .AddQueryParam("app_id", appId)
                .AddQueryParam("state", state);

            return url;
        }

        public async Task<UserInfoModel> GetUserInfoAsync(string userAccessToken)
        {
            var logger = LoggerFactory.CreateLogger<AuthenApiImpl>();

            var url = UserInfoApi;

            var httpClient = GetClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userAccessToken);

            var response = await httpClient.GetAsync(url);
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserInfoAsync,url:{url} statusCode:{response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserInfoAsync,url:{url} responseContent:{responseContent}");
            }

            var result = ReadDataResult<UserInfoModel>(responseContent);

            ThrowExceptionIfError(result);

            return result.Data;
        }
    }
}
