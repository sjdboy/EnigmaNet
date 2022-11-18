using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models.Authens
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/authen-v1/authen/user_info
    /// </remarks>
    public class UserInfoModel
    {
        public string Name { get; set; }
        public string EnName { get; set; }
        public string AvatarUrl { get; set; }
        public string AvatarThumb { get; set; }
        public string AavatarMiddle { get; set; }
        public string AvatarBig { get; set; }
        public string OpenId { get; set; }
        public string UnionId { get; set; }
        public string Email { get; set; }
        public string EnterpriseEmail { get; set; }
        public string UserId { get; set; }
        public string Mobile { get; set; }
        public string TenantKey { get; set; }
    }
}
