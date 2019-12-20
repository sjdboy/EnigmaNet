using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnigmaNet.DouYinOpenApi
{
    public interface IApiClient
    {
        Task<string> GetOAuthConnectAsync(string[] scopes,string state);

        Task<Models.OAuth.AccessTokenResult> GetOAuthAccessTokenAsync(string code);

        Task<Models.OAuth.RefreshTokenResult> RefreshOAuthTokenAsync(string refreshToken);

        Task<Models.OAuth.ClientTokenResult> ApplyClientTokenAsync();

        Task<Models.UserInfo> GetUserInfoAsync(string openId, string accessToken);

        Task<Models.Following.FollowingListResult> GetFollowingListAsync(string openId, string accessToken, int pageSize, int? cursor);

        Task<Models.Fans.FansListResult> GetFansListAsync(string openId, string accessToken, int pageSize, int? cursor);

        Task<Models.Fans.DataResult> GetFansDataAsync(string openId, string accessToken);

        Task<Models.Video.UploadVideoResult> UploadVideoAsync(string openId, string accessToken, System.IO.Stream stream);

        Task<Models.Video.CreateVideoResult> CreateVideoAsync(string openId, string accessToken, string videoId, string text, string microAppId, string microAppTitle, string microAppUrl, double coverTime, string[] atUserOpenIds);

        Task<Models.Video.VideoListResult> GetVideoListAsync(string openId, string accessToken, int pageSize, int? cursor);

        Task<List<Models.Video.VideoItemInfo>> GetVideoDataAsync(string openId, string accessToken, string[] itemIds);

        Task DeleteVideoAsync(string openId, string accessToken, string[] itemIds);

        Task<Models.Video.CommentListResult> GetVideoCommentListAsync(string openId, string accessToken, string itemId, int pageSize, int? cursor);

        Task<Models.Video.CommentListResult> GetVideoCommentReplyListAsync(string openId, string accessToken, string itemId, string commentId, int pageSize, int? cursor);

        Task ReplyVideoCommentAsync(string openId, string accessToken, string itemId, string commentId, string content);

        Task SetVideoCommentTopStatusAsync(string openId, string accessToken, string itemId, string commentId, bool isTop);

        Task SendIMMessageAsync(string openId, string accessToken, string toUserId, bool isImageMessage, string messageContent);
    }
}
