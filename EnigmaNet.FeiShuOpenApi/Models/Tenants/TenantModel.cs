using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models.Tenants
{
    public class TenantModel
    {
        public string Name { get; set; }
        public string DisplayId { get; set; }
        public int TenantTag { get; set; }
        public string TenantKey { get; set; }
        public AvatarModel Avatar { get; set; }
    }
}
