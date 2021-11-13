using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.DouYinOpenApi.Models.OAuth
{
    public class RenewRefreshTokenResult
    {
        public string RefreshToken { get; set; }
        public int RefreshExpiresSeconds { get; set; }
    }
}
