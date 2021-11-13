using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using EnigmaNet.DouYinOpenApi.Models.OAuth;
using EnigmaNet.Extensions;

namespace EnigmaNet.DouYinOpenApi.Impls
{
    public class OAuthApi : ApiBase, IOAuthApi
    {
        class AccessTokenModel : DataBase
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
            public int refresh_expires_in { get; set; }
            public string open_id { get; set; }
            public string scope { get; set; }
        }

        class RefreshTokenModel : DataBase
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
            public int refresh_expires_in { get; set; }
            public string open_id { get; set; }
            public string scope { get; set; }
        }

        class RenewRefreshTokenModel : DataBase
        {
            public string refresh_token { get; set; }
            public int expires_in { get; set; }
        }

        class ClientTokenModel : DataBase
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
        }

        /// <summary>
        /// 获取授权码
        /// </summary>
        /// <param name="clientKey"></param>
        /// <param name="scopes"></param>
        /// <param name="state"></param>
        /// <param name="redirectUrl"></param>
        /// <returns></returns>
        /// <remarks>
        /// 文档：https://open.douyin.com/platform/doc/6848834666171009035
        /// </remarks>
        public Task<string> GetOAuthConnectAsync(string clientKey, string[] scopes, string state, string redirectUrl)
        {
            var url = Api + "/platform/oauth/connect/"
                .AddQueryParam("client_key", clientKey)
                .AddQueryParam("response_type", "code")
                .AddQueryParam("scope", string.Join(",", scopes))
                .AddQueryParam("state", state)
                .AddQueryParam("redirect_uri", redirectUrl);

            return Task.FromResult(url);
        }

        public Task<string> GetOAuthConnectV2Async(string clientKey, string state, string redirectUrl)
        {
            var url = "https://aweme.snssdk.com/oauth/authorize/v2/"
                .AddQueryParam("client_key", clientKey)
                .AddQueryParam("response_type", "code")
                .AddQueryParam("scope", "login_id")
                .AddQueryParam("state", state)
                .AddQueryParam("redirect_uri", redirectUrl);

            return Task.FromResult(url);
        }

        /// <summary>
        /// 获取access_token
        /// </summary>
        /// <param name="clientKey"></param>
        /// <param name="clientSecret"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        /// <remarks>
        /// 文档：https://open.douyin.com/platform/doc/6848806493387606024
        /// </remarks>
        public async Task<AccessTokenResult> GetOAuthAccessTokenAsync(string clientKey, string clientSecret, string code)
        {
            var logger = LoggerFactory.CreateLogger<OtherApi>();

            var url = Api + "/oauth/access_token/"
                .AddQueryParam("client_key", clientKey)
                .AddQueryParam("client_secret", clientSecret)
                .AddQueryParam("code", code)
                .AddQueryParam("grant_type", "authorization_code");

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetOAuthAccessTokenAsync,url:{url}");
            }

            DefaultResultModel<AccessTokenModel> result;
            var httpClient = GetClient();
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetOAuthAccessTokenAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetOAuthAccessTokenAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<AccessTokenModel>>();
            }

            var data = result.data;

            ThrowExceptionIfError(data);

            return new AccessTokenResult
            {
                AccessToken = data.access_token,
                OpenId = data.open_id,
                ExpiresSeconds = data.expires_in,
                RefreshToken = data.refresh_token,
                RefreshExpiresSeconds = data.refresh_expires_in,
                Scopes = data?.scope.Split(",", StringSplitOptions.RemoveEmptyEntries),
            };
        }

        public async Task<RefreshTokenResult> RefreshOAuthTokenAsync(string clientKey, string refreshToken)
        {
            var logger = LoggerFactory.CreateLogger<OtherApi>();

            var url = Api + "/oauth/refresh_token/"
                .AddQueryParam("client_key", clientKey)
                .AddQueryParam("refresh_token", refreshToken)
                .AddQueryParam("grant_type", "refresh_token");

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"RefreshOAuthTokenAsync,url:{url}");
            }

            DefaultResultModel<RefreshTokenModel> result;
            var httpClient = GetClient();
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"RefreshOAuthTokenAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"RefreshOAuthTokenAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<RefreshTokenModel>>();
            }

            var data = result.data;

            ThrowExceptionIfError(data);

            return new RefreshTokenResult
            {
                AccessToken = data.access_token,
                ExpiresSeconds = data.expires_in,
                OpenId = data.open_id,
                RefreshToken = data.refresh_token,
                RefreshExpiresSeconds = data.refresh_expires_in,
                Scopes = data?.scope.Split(",", StringSplitOptions.RemoveEmptyEntries),
            };
        }

        public async Task<ClientTokenResult> ApplyClientTokenAsync(string clientKey, string clientSecret)
        {
            var logger = LoggerFactory.CreateLogger<OtherApi>();

            var url = Api + "/oauth/client_token/"
                .AddQueryParam("client_key", clientKey)
                .AddQueryParam("client_secret", clientSecret)
                .AddQueryParam("grant_type", "client_credential");

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"ApplyClientTokenAsync,url:{url}");
            }

            DefaultResultModel<ClientTokenModel> result;
            var httpClient = GetClient();
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"ApplyClientTokenAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"ApplyClientTokenAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<ClientTokenModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);
            return new ClientTokenResult
            {
                AccessToken = data.access_token,
                ExpiresSeconds = data.expires_in,
            };
        }

        public async Task<RenewRefreshTokenResult> RenewRefreshTokenAsync(string clientKey, string refreshToken)
        {
            var logger = LoggerFactory.CreateLogger<OtherApi>();

            var url = Api + "/oauth/renew_refresh_token/"
                .AddQueryParam("client_key", clientKey)
                .AddQueryParam("refresh_token", refreshToken);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"RenewRefreshTokenAsync,url:{url}");
            }

            DefaultResultModel<RenewRefreshTokenModel> result;
            var httpClient = GetClient();
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"RenewRefreshTokenAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"RenewRefreshTokenAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<RenewRefreshTokenModel>>();
            }

            var data = result.data;

            ThrowExceptionIfError(data);

            return new RenewRefreshTokenResult
            {
                RefreshToken = data.refresh_token,
                RefreshExpiresSeconds = data.expires_in,
            };
        }
    }
}
