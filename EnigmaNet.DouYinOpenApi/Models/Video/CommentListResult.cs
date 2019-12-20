using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.Video
{
    public class CommentListResult
    {
        public int Cursor { get; set; }
        public bool HasMore { get; set; }
        public List<CommentInfo> List { get; set; }
    }
}
