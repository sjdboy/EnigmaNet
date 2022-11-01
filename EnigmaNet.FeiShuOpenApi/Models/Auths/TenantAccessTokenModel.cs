using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models.Auths
{
    public class TenantAccessTokenModel
    {
        public string TenantAccessToken { set; get; }
        public int Expire { get; set; }
    }
}
