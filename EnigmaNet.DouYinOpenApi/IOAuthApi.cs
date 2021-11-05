using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnigmaNet.DouYinOpenApi.Models.OAuth;

namespace EnigmaNet.DouYinOpenApi
{
    public interface IOAuthApi
    {
        Task<string> GetOAuthConnectAsync(string clientKey, string[] scopes, string state, string redirectUrl);

        Task<string> GetOAuthConnectV2Async(string clientKey, string state, string redirectUrl);

        Task<AccessTokenResult> GetOAuthAccessTokenAsync(string clientKey, string clientSecret, string code);

        Task<RefreshTokenResult> RefreshOAuthTokenAsync(string clientKey, string refreshToken);

        Task<ClientTokenResult> ApplyClientTokenAsync(string clientKey, string clientSecret);

        Task<RenewRefreshTokenResult> RenewRefreshTokenAsync(string clientKey, string refreshToken)
        {
            throw new NotImplementedException();
        }
    }
}
