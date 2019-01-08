using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.CMQBus
{
    public class CMQBusOptions
    {
        public string SecrectId { get; set; }
        public string SecrectKey { get; set; }
        public string RegionHost { get; set; }
        public bool IsHttps { get; set; }
        public string SignatureMethod { get; set; }

        public string InstanceId { get; set; }
        public string PublishEventNamePrefix { get; set; }
    }
}
