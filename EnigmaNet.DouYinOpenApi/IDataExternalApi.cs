using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnigmaNet.DouYinOpenApi.Models.DataExternals;

namespace EnigmaNet.DouYinOpenApi
{
    public interface IDataExternalApi
    {
        Task<UserVideoDataResult> GetUserVideoDataAsync(string openId,string accessToken,int dataType);

        Task<UserFansDataResult> GetUserFansDataAsync(string openId, string accessToken, int dataType);

        Task<UserLikeDataResult> GetUserLikeDataAsync(string openId, string accessToken, int dataType);

        Task<UserCommentDataResult> GetUserCommentDataAsync(string openId, string accessToken, int dataType);

        Task<UserShareDataResult> GetUserShareDataAsync(string openId, string accessToken, int dataType);

        Task<UserProfileDataResult> GetUserProfileDataAsync(string openId, string accessToken, int dataType);
    }
}
