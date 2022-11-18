using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnigmaNet.FeiShuOpenApi.Models.Authens;

namespace EnigmaNet.FeiShuOpenApi
{
    public interface IAuthenApi
    {
        /// <summary>
        /// 获取 user_access_token
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/authen-v1/authen/access_token
        /// </remarks>
        Task<AccessTokenModel> GetAccessTokenAsync(string appAccessToken,string code);

        /// <summary>
        /// 获取登录用户信息
        /// </summary>
        /// <param name="userAccessToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/authen-v1/authen/user_info
        /// </remarks>
        Task<UserInfoModel> GetUserInfoAsync(string userAccessToken);

        /// <summary>
        /// 获取登录预授权码
        /// </summary>
        /// <param name="redirectUrl"></param>
        /// <param name="appId"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/ukTMukTMukTM/ukzN4UjL5cDO14SO3gTN
        /// </remarks>
        Task<string> GetAuthUrlAsync(string redirectUrl, string appId, string state);
    }
}
