using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnigmaNet.FeiShuOpenApi.Models.Auths;

namespace EnigmaNet.FeiShuOpenApi
{
    public interface IInternalAuthApi
    {
        /// <summary>
        /// 自建应用获取 tenant_access_token
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/ukTMukTMukTM/ukDNz4SO0MjL5QzM/auth-v3/auth/tenant_access_token_internal
        /// </remarks>
        Task<TenantAccessTokenModel> ApplyTenantAccessTokenAsync(string appId, string appSecret);

        /// <summary>
        /// 自建应用获取 app_access_token
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/ukTMukTMukTM/ukDNz4SO0MjL5QzM/auth-v3/auth/app_access_token_internal
        /// </remarks>
        Task<AppAccessTokenModel> ApplyAppAccessTokenAsync(string appId, string appSecret);
    }
}
