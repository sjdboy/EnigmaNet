using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.BigCache.Cos.Options
{
    public class CosBigCacheOptions
    {
        public string AppId { get; set; }
        public string SecretId { get; set; }
        public string SecretKey { get; set; }
        public string Region { get; set; }
        public string Bucket { get; set; }
    }
}
