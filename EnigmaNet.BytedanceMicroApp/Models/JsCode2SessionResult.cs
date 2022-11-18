using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.BytedanceMicroApp.Models
{
    public class JsCode2SessionResult
    {
        public string SessionKey { get; set; }
        public string OpenId { get; set; }
        public string AnonymousOpenId { get; set; }
        public string UnionId { get; set; }
    }
}
