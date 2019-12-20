using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.Video
{
    public class VideoListResult
    {
        public int Cursor { get; set; }
        public bool HasMore { get; set; }
        public List<VideoItemInfo> List { get; set; }
    }
}
