using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.Following
{
    public class FollowingListResult
    {
        public long Cursor { get; set; }
        public bool HasMore { get; set; }
        public List<UserInfo> List { get; set; }
    }
}
