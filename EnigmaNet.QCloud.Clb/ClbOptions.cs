using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.QCloud.Clb
{
    public class ClbOptions
    {
        public class LocationInfo
        {
            public string LoadBalancerId { get; set; }
            public string ListenerId { get; set; }
            public string LocationId { get; set; }
        }

        public string SecretId { get; set; }
        public string SecretKey { get; set; }
        public string Region { get; set; }
        public List<LocationInfo> Locations { get; set; }
        public string IpSegment { get; set; }
        public int MaxTryCount { get; set; } = 10;
    }
}
