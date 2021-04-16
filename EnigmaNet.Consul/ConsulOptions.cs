using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnigmaNet.Consul
{
    public class ConsulOptions
    {
        public string Url { get; set; }
        public string Token { get; set; }
        public string CheckPath { get; set; }
        public List<string> Tags { get; set; }
        public IDictionary<string, string> Meta { get; set; }
        public string IpSegment { get; set; }
    }
}
