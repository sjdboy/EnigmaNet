using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnigmaNet.FeiShuOpenApi.Models.JsSdks;

namespace EnigmaNet.FeiShuOpenApi
{
    public interface IJsSdkApi
    {
        /// <summary>
        /// 获取 JSAPI 临时调用凭证
        /// </summary>
        /// <param name="tenantAccessToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://open.feishu.cn/document/ukTMukTMukTM/uYTM5UjL2ETO14iNxkTN/h5_js_sdk/authorization
        /// </remarks>
        Task<TicketModel> GetTicketAsync(string tenantAccessToken);
    }
}
