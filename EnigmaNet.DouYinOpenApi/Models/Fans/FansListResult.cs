using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.Fans
{
    public class FansListResult
    {
        public int Cursor { get; set; }
        public bool HasMore { get; set; }
        public List<UserInfo> List { get; set; }
    }
}
