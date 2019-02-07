using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.QCloud.Cos.Models
{
    public class UploadInfoModel
    {
        public string Bucket { get; set; }
        public string AppId { get; set; }
        public string Region { get; set; }
        public string UploadUrl { get; set; }
        public string Authorization { get; set; }
    }
}
