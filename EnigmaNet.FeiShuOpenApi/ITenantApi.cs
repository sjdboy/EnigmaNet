using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnigmaNet.FeiShuOpenApi.Models.Tenants;

namespace EnigmaNet.FeiShuOpenApi
{
    public interface ITenantApi
    {
        /// <summary>
        /// 获取企业信息
        /// </summary>
        /// <param name="tenantAccessToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/tenant-v2/tenant/query
        /// </remarks>
        Task<TenantModel> GetAsync(string tenantAccessToken);
    }
}
