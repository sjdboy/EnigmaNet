using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnigmaNet.DouYinOpenApi
{
    public interface IApiClient
    {
        Task<string> GetOAuthConnectAsync(string[] scopes, string state, string redirectUrl);

        Task<string> GetOAuthConnectV2Async(string state, string redirectUrl);

        Task<Models.OAuth.AccessTokenResult> GetOAuthAccessTokenAsync(string code);

        Task<Models.OAuth.RefreshTokenResult> RefreshOAuthTokenAsync(string refreshToken);

        Task<Models.OAuth.ClientTokenResult> ApplyClientTokenAsync();

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

        Task<Models.Video.UploadVideoResult> UploadVideoAsync(string openId, string accessToken, System.IO.Stream stream);

        Task<Models.Video.CreateVideoResult> CreateVideoAsync(string openId, string accessToken, string videoId, string text, string microAppId, string microAppTitle, string microAppUrl, double coverTime, string[] atUserOpenIds, string poiId, string poiName);

        Task<Models.Video.VideoListResult> GetVideoListAsync(string openId, string accessToken, int pageSize, long? cursor);

        Task<List<Models.Video.VideoItemInfo>> GetVideoDataAsync(string openId, string accessToken, string[] itemIds);

        Task DeleteVideoAsync(string openId, string accessToken, string itemId);

        Task<Models.Video.CommentListResult> GetVideoCommentListAsync(string openId, string accessToken, string itemId, int pageSize, long? cursor);

        Task<Models.Video.CommentListResult> GetVideoCommentReplyListAsync(string openId, string accessToken, string itemId, string commentId, int pageSize, long? cursor);

        Task ReplyVideoCommentAsync(string openId, string accessToken, string itemId, string commentId, string content);

        Task SetVideoCommentTopStatusAsync(string openId, string accessToken, string itemId, string commentId, bool isTop);

        Task SendIMMessageAsync(string openId, string accessToken, string toUserId, bool isImageMessage, string messageContent);

        Task<Models.Poi.PoiListResult> SearchPoiAsync(string clientAccessToken, string cityName, string keyword, int pageSize, long? cursor);
    }
}
