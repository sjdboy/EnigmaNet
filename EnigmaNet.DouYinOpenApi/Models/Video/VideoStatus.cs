using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.DouYinOpenApi.Models.Video
{
    public enum VideoStatus
    {
        /// <summary>
        /// 已发布
        /// </summary>
        Published = 1,
        /// <summary>
        /// 不适宜公开
        /// </summary>
        NoSuitable = 2,
        /// <summary>
        /// 审核中
        /// </summary>
        UnderReview = 4,
        /// <summary>
        /// 公开视频
        /// </summary>
        Public = 5,
        /// <summary>
        /// 好友可见
        /// </summary>
        FriendVisible = 6,
        /// <summary>
        /// 私密视频
        /// </summary>
        Private = 7,
    }
}
