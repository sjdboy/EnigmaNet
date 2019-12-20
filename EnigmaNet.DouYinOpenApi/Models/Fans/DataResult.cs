using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.Fans
{
    public class DataResult
    {
        /// <summary>
        /// 粉丝总数
        /// </summary>
        public int FansAmount { get; set; }
        /// <summary>
        /// 性别分布
        /// </summary>
        public List<KeyValuePair<string, int>> GenderDistributions { get; set; }
        /// <summary>
        /// 年龄分布
        /// </summary>
        public List<KeyValuePair<string, int>> AgetDistributions { get; set; }
        /// <summary>
        /// 地域分布
        /// </summary>
        public List<KeyValuePair<string, int>> GeographicalDistributions { get; set; }
        /// <summary>
        /// 活跃天数分布
        /// </summary>
        public List<KeyValuePair<string, int>> ActiveDaysDistributions { get; set; }
        /// <summary>
        /// 设备分布
        /// </summary>
        public List<KeyValuePair<string, int>> DeviceDistributions { get; set; }
        /// <summary>
        /// 兴趣分布
        /// </summary>
        public List<KeyValuePair<string, int>> InterestDistributions { get; set; }
        /// <summary>
        /// 粉丝流量贡献
        /// </summary>
        public List<ContributionModel> FlowContributions { get; set; }
    }
}
