using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.DouYinOpenApi
{
    public interface IUserApi
    {
        Task<Models.UserInfo> GetUserInfoAsync(string openId, string accessToken);

        Task<Models.Following.FollowingListResult> GetFollowingListAsync(string openId, string accessToken, int pageSize, long? cursor);

        Task<Models.Fans.FansListResult> GetFansListAsync(string openId, string accessToken, int pageSize, long? cursor);

        /// <summary>
        /// 获取用户粉丝数据（经实践得知此数据是调用时的前2天的粉丝数据）
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        Task<Models.Fans.DataResult> GetFansDataAsync(string openId, string accessToken);

    }
}
