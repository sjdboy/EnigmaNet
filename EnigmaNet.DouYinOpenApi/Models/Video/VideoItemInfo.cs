using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.Video
{
    public class VideoItemInfo
    {
        public string ItemId { get; set; }
        public string Title { get; set; }
        public string Cover { get; set; }
        public bool IsTop { get; set; }
        public DateTime CreateTime { get; set; }
        public bool IsReviewed { get; set; }
        public string ShareUrl { get; set; }
        public VideoStatistics Statistics { get; set; }
    }
}
