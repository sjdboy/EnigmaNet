using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models
{
    public class UserInfo
    {
        public string OpenId { get; set; }
        public string UnionId { get; set; }
        public string NickName { get; set; }
        public string Avatar { get; set; }
        public string Country { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public bool? Gender { get; set; }
    }
}
