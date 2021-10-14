using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.DouYinOpenApi
{
    public interface IOAuthApi
    {
        Task<string> GetOAuthConnectAsync(string clientKey, string[] scopes, string state, string redirectUrl);

        Task<string> GetOAuthConnectV2Async(string clientKey, string state, string redirectUrl);

        Task<Models.OAuth.AccessTokenResult> GetOAuthAccessTokenAsync(string clientKey, string clientSecret,string code);

        Task<Models.OAuth.RefreshTokenResult> RefreshOAuthTokenAsync(string clientKey, string refreshToken);

        Task<Models.OAuth.ClientTokenResult> ApplyClientTokenAsync(string clientKey, string clientSecret);
    }
}
