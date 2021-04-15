using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.QCloud.Cos.Options
{
    public class QCloudCosOptions
    {
        public string AppId { get; set; }
        public string SecretId { get; set; }
        public string SecretKey { get; set; }
        public string Bucket { get; set; }
        public string Region { get; set; }
    }
}
