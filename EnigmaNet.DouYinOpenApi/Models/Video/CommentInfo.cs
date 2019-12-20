using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.Video
{
    public class CommentInfo
    {
        public string CommentId { get; set; }
        public string CommentUserId { get; set; }
        public string Content { get; set; }
        public DateTime CreateTime { get; set; }
        public bool IsTop { get; set; }
        public int DiggCount { get; set; }
        public int ReplyTotal { get; set; }
    }
}
