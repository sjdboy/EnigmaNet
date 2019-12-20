using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.Video
{
    public class VideoStatistics
    {
        public int CommentCount { get; set; }
        /// <summary>
        /// 点击数
        /// </summary>
        public int DiggCount { get; set; }
        public int DownloadCount { get; set; }
        public int PlayCount { get; set; }
        public int ShareCount { get; set; }
        public int ForwardCount { get; set; }
    }
}
