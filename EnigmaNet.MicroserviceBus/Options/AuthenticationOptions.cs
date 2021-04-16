using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnigmaNet.MicroserviceBus.Options
{
    public class AuthenticationOptions
    {
        public string ApiId { get; set; }
        public string ApiSecret { get; set; }
        public string TokenIssuer { get; set; }
    }
}
