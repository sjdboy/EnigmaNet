using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using Microsoft.Extensions.Logging;

using EnigmaNet.Extensions;
using EnigmaNet.DouYinOpenApi.Models;
using EnigmaNet.DouYinOpenApi.Models.Fans;
using EnigmaNet.DouYinOpenApi.Models.Following;
using EnigmaNet.DouYinOpenApi.Models.OAuth;
using EnigmaNet.DouYinOpenApi.Models.Video;
using EnigmaNet.Utils;
using EnigmaNet.DouYinOpenApi.Models.Poi;

namespace EnigmaNet.DouYinOpenApi
{
    public class ApiClient : IApiClient
    {
        #region models

        abstract class DataBase
        {
            public int error_code { get; set; }
            public string description { get; set; }
        }

        class CommonResultModel : DataBase
        {

        }

        class DefaultResultModel<T> where T : DataBase
        {
            public T data { get; set; }
        }

        class AccessTokenModel : DataBase
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
            public string open_id { get; set; }
            public string scope { get; set; }
        }

        class RefreshTokenModel : DataBase
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
            public string open_id { get; set; }
            public string scope { get; set; }
        }

        class ClientTokenModel : DataBase
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
        }

        class UserInfoModel : DataBase
        {
            public string open_id { get; set; }
            public string union_id { get; set; }
            public string nickname { get; set; }
            public string avatar { get; set; }
            public string city { get; set; }
            public string province { get; set; }
            public string country { get; set; }
            public int gender { get; set; }
        }

        class FollowingListModel : DataBase
        {
            public class Item
            {
                public string open_id { get; set; }
                public string union_id { get; set; }
                public string nickname { get; set; }
                public string avatar { get; set; }
                public string city { get; set; }
                public string province { get; set; }
                public string country { get; set; }
                public int gender { get; set; }
            }

            public long cursor { get; set; }
            public bool has_more { get; set; }
            public List<Item> list { get; set; }
        }

        class FanListModel : DataBase
        {
            public class Item
            {
                public string open_id { get; set; }
                public string union_id { get; set; }
                public string nickname { get; set; }
                public string avatar { get; set; }
                public string city { get; set; }
                public string province { get; set; }
                public string country { get; set; }
                public int gender { get; set; }
            }

            public long cursor { get; set; }
            public bool has_more { get; set; }
            public List<Item> list { get; set; }
        }

        class FanDataModel : DataBase
        {
            public class Distribution
            {
                public string item { get; set; }
                public int value { get; set; }
            }

            public class Contribution
            {
                public string flow { get; set; }
                public int fans_sum { get; set; }
                public int all_sum { get; set; }
            }

            public class DataModel
            {
                public int all_fans_num { get; set; }
                public List<Distribution> gender_distributions { get; set; }
                public List<Distribution> age_distributions { get; set; }
                public List<Distribution> geographical_distributions { get; set; }
                public List<Distribution> active_days_distributions { get; set; }
                public List<Distribution> device_distributions { get; set; }
                public List<Distribution> interest_distributions { get; set; }
                public List<Contribution> flow_contributions { get; set; }
            }

            public DataModel fans_data { get; set; }
        }

        class VideItemModel
        {
            public class StatisticsModel
            {
                public int comment_count { get; set; }
                public int digg_count { get; set; }
                public int download_count { get; set; }
                public int play_count { get; set; }
                public int share_count { get; set; }
                public int forward_count { get; set; }
            }

            public string item_id { get; set; }
            public string title { get; set; }
            public string cover { get; set; }
            public bool is_top { get; set; }
            public int create_time { get; set; }
            public bool is_reviewed { get; set; }
            public string share_url { get; set; }
            public StatisticsModel statistics { get; set; }
        }

        class VideoListModel : DataBase
        {
            public long cursor { get; set; }
            public bool has_more { get; set; }
            public List<VideItemModel> list { get; set; }
        }

        class VideoDataModel : DataBase
        {
            public List<VideItemModel> list { get; set; }
        }

        class VideoCommentListModel : DataBase
        {
            public class ItemModel
            {
                public string comment_id { get; set; }
                public string comment_user_id { get; set; }
                public string content { get; set; }
                public int create_time { get; set; }
                public bool top { get; set; }
                public int digg_count { get; set; }
                public int reply_comment_total { get; set; }
            }

            public long cursor { get; set; }
            public bool has_more { get; set; }
            public List<ItemModel> list { get; set; }
        }

        class PoiSearchListModel : DataBase
        {
            public class ItemModel
            {
                public string poi_id { get; set; }
                public string poi_name { get; set; }
                public string location { get; set; }
                public string country { get; set; }
                public string country_code { get; set; }
                public string province { get; set; }
                public string city { get; set; }
                public string city_code { get; set; }
                public string district { get; set; }
                public string address { get; set; }
            }

            public long cursor { get; set; }
            public bool has_more { get; set; }
            public List<ItemModel> pois { get; set; }
        }

        class CreateVideoModel : DataBase
        {
            public string item_id { get; set; }
        }

        class UploadVideoModel : DataBase
        {
            public class VideoModel
            {
                public string video_id { get; set; }
                public int width { get; set; }
                public int height { get; set; }
            }
            public VideoModel video { get; set; }
        }

        #endregion

        const string Api = "https://open.douyin.com";

        bool? GetGender(int gender)
        {
            switch (gender)
            {
                case 0:
                    return null;
                case 1:
                    return true;
                case 2:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gender));
            }
        }

        void ThrowExceptionIfError(DataBase result)
        {
            if (result.error_code != 0)
            {
                throw new DouYinApiException(result.error_code, $"errorCode({result.error_code})" + result.description);
            }
        }

        public string Key { get; set; }
        public string Secret { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }

        public Task<string> GetOAuthConnectAsync(string[] scopes, string state, string redirectUrl)
        {
            var url = Api + "/platform/oauth/connect/"
                .AddQueryParam("client_key", Key)
                .AddQueryParam("response_type", "code")
                .AddQueryParam("scope", string.Join(",", scopes))
                .AddQueryParam("state", state)
                .AddQueryParam("redirect_uri", redirectUrl);

            return Task.FromResult(url);
        }

        public Task<string> GetOAuthConnectV2Async(string state, string redirectUrl)
        {
            var url = "https://aweme.snssdk.com/oauth/authorize/v2/"
                .AddQueryParam("client_key", Key)
                .AddQueryParam("response_type", "code")
                .AddQueryParam("scope", "login_id")
                .AddQueryParam("state", state)
                .AddQueryParam("redirect_uri", redirectUrl);

            return Task.FromResult(url);
        }

        public async Task<AccessTokenResult> GetOAuthAccessTokenAsync(string code)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/oauth/access_token/"
                .AddQueryParam("client_key", Key)
                .AddQueryParam("client_secret", Secret)
                .AddQueryParam("code", code)
                .AddQueryParam("grant_type", "authorization_code");

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetOAuthAccessTokenAsync,url:{url}");
            }

            DefaultResultModel<AccessTokenModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetOAuthAccessTokenAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetOAuthAccessTokenAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<AccessTokenModel>>();
            }

            var data = result.data;

            ThrowExceptionIfError(data);

            return new AccessTokenResult
            {
                AccessToken = data.access_token,
                OpenId = data.open_id,
                ExpiresSeconds = data.expires_in,
                RefreshToken = data.refresh_token,
                Scopes = data?.scope.Split(",", StringSplitOptions.RemoveEmptyEntries),
            };
        }

        public async Task<RefreshTokenResult> RefreshOAuthTokenAsync(string refreshToken)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/oauth/refresh_token/"
                .AddQueryParam("client_key", Key)
                .AddQueryParam("refresh_token", refreshToken)
                .AddQueryParam("grant_type", "refresh_token");

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"RefreshOAuthTokenAsync,url:{url}");
            }

            DefaultResultModel<RefreshTokenModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"RefreshOAuthTokenAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"RefreshOAuthTokenAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<RefreshTokenModel>>();
            }

            var data = result.data;

            ThrowExceptionIfError(data);

            return new RefreshTokenResult
            {
                AccessToken = data.access_token,
                ExpiresSeconds = data.expires_in,
                OpenId = data.open_id,
                RefreshToken = data.refresh_token,
                Scopes = data?.scope.Split(",", StringSplitOptions.RemoveEmptyEntries),
            };
        }

        public async Task<ClientTokenResult> ApplyClientTokenAsync()
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/oauth/client_token/"
                .AddQueryParam("client_key", Key)
                .AddQueryParam("client_secret", Secret)
                .AddQueryParam("grant_type", "client_credential");

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"ApplyClientTokenAsync,url:{url}");
            }

            DefaultResultModel<ClientTokenModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"ApplyClientTokenAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"ApplyClientTokenAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<ClientTokenModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);
            return new ClientTokenResult
            {
                AccessToken = data.access_token,
                ExpiresSeconds = data.expires_in,
            };
        }

        public async Task<UserInfo> GetUserInfoAsync(string openId, string accessToken)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "​/oauth/userinfo/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserInfoAsync,url:{url}");
            }

            DefaultResultModel<UserInfoModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetUserInfoAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetUserInfoAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<UserInfoModel>>();
            }

            var data = result.data;

            ThrowExceptionIfError(data);

            return new UserInfo
            {
                Avatar = data.avatar,
                City = data.city,
                Country = data.country,
                Gender = GetGender(data.gender),
                NickName = data.nickname,
                OpenId = data.open_id,
                Province = data.province,
                UnionId = data.union_id,
            };
        }

        public async Task<FollowingListResult> GetFollowingListAsync(string openId, string accessToken, int pageSize, long? cursor)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "​/following/list/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId)
               .AddQueryParam("cursor", (cursor ?? 0).ToString())
               .AddQueryParam("count", pageSize)
               ;

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetFollowingListAsync,url:{url}");
            }

            DefaultResultModel<FollowingListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetFollowingListAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetFollowingListAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<FollowingListModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new FollowingListResult
            {
                Cursor = data.cursor,
                HasMore = data.has_more,
                List = data.list?.Select(m => new UserInfo
                {
                    Avatar = m.avatar,
                    City = m.city,
                    Country = m.country,
                    Gender = GetGender(m.gender),
                    NickName = m.nickname,
                    OpenId = m.open_id,
                    Province = m.province,
                    UnionId = m.union_id,
                }).ToList(),
            };
        }

        public async Task<FansListResult> GetFansListAsync(string openId, string accessToken, int pageSize, long? cursor)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "​/fans/list/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId)
               .AddQueryParam("cursor", (cursor ?? 0).ToString())
               .AddQueryParam("count", pageSize)
               ;

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetFansListAsync,url:{url}");
            }

            DefaultResultModel<FanListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetFansListAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetFansListAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<FanListModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new FansListResult
            {
                Cursor = data.cursor,
                HasMore = data.has_more,
                List = data.list?.Select(m => new UserInfo
                {
                    Avatar = m.avatar,
                    City = m.city,
                    Country = m.country,
                    Gender = GetGender(m.gender),
                    NickName = m.nickname,
                    OpenId = m.open_id,
                    Province = m.province,
                    UnionId = m.union_id,
                }).ToList(),
            };
        }

        public async Task<DataResult> GetFansDataAsync(string openId, string accessToken)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/fans/data/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetFansDataAsync,url:{url}");
            }

            DefaultResultModel<FanDataModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetFansDataAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetFansDataAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<FanDataModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            var fansData = data.fans_data;

            if (fansData == null)
            {
                return null;
            }

            return new DataResult
            {
                FansAmount = fansData.all_fans_num,
                GenderDistributions = fansData.gender_distributions?.Select(m => new KeyValuePair<string, int>(m.item, m.value)).ToList(),
                AgetDistributions = fansData.age_distributions?.Select(m => new KeyValuePair<string, int>(m.item, m.value)).ToList(),
                ActiveDaysDistributions = fansData.active_days_distributions?.Select(m => new KeyValuePair<string, int>(m.item, m.value)).ToList(),
                DeviceDistributions = fansData.device_distributions?.Select(m => new KeyValuePair<string, int>(m.item, m.value)).ToList(),
                GeographicalDistributions = fansData.geographical_distributions?.Select(m => new KeyValuePair<string, int>(m.item, m.value)).ToList(),
                InterestDistributions = fansData.interest_distributions?.Select(m => new KeyValuePair<string, int>(m.item, m.value)).ToList(),
                FlowContributions = fansData.flow_contributions?.Select(m => new ContributionModel
                {
                    Flow = m.flow,
                    FansSum = m.fans_sum,
                    AllSum = m.all_sum,
                }).ToList(),
            };
        }

        public async Task<VideoListResult> GetVideoListAsync(string openId, string accessToken, int pageSize, long? cursor)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/video/list/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("cursor", (cursor ?? 0).ToString())
                .AddQueryParam("count", pageSize);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetVideoListAsync,url:{url}");
            }

            DefaultResultModel<VideoListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetVideoListAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetVideoListAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<VideoListModel>>();
            }

            var data = result.data;
            if (data == null)
            {
                return null;
            }
            ThrowExceptionIfError(data);

            return new VideoListResult
            {
                Cursor = data.cursor,
                HasMore = data.has_more,
                List = data.list?.Select(m => new VideoItemInfo
                {
                    ItemId = m.item_id,
                    Cover = m.cover,
                    CreateTime = DateTimeUtils.ToDateTime(m.create_time),
                    IsReviewed = m.is_reviewed,
                    IsTop = m.is_top,
                    ShareUrl = m.share_url,
                    Title = m.title,
                    Statistics = new VideoStatistics
                    {
                        CommentCount = m.statistics.comment_count,
                        DiggCount = m.statistics.digg_count,
                        DownloadCount = m.statistics.download_count,
                        ForwardCount = m.statistics.forward_count,
                        PlayCount = m.statistics.play_count,
                        ShareCount = m.statistics.share_count,
                    }
                }).ToList(),
            };
        }

        public async Task<List<VideoItemInfo>> GetVideoDataAsync(string openId, string accessToken, string[] itemIds)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/video/data/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetVideoDataAsync,url:{url}");
            }

            DefaultResultModel<VideoDataModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync(url, new
                {
                    item_ids = itemIds
                });

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var requestContent = await response.RequestMessage.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetVideoDataAsync,url:{url} requestContent:{requestContent}");

                    logger.LogTrace($"GetVideoDataAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetVideoDataAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<VideoDataModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return data.list?.Select(m => new VideoItemInfo
            {
                ItemId = m.item_id,
                Cover = m.cover,
                CreateTime = DateTimeUtils.ToDateTime(m.create_time),
                IsReviewed = m.is_reviewed,
                IsTop = m.is_top,
                ShareUrl = m.share_url,
                Title = m.title,
                Statistics = new VideoStatistics
                {
                    CommentCount = m.statistics.comment_count,
                    DiggCount = m.statistics.digg_count,
                    DownloadCount = m.statistics.download_count,
                    ForwardCount = m.statistics.forward_count,
                    PlayCount = m.statistics.play_count,
                    ShareCount = m.statistics.share_count,
                }
            }).ToList();
        }

        public async Task<CommentListResult> GetVideoCommentListAsync(string openId, string accessToken, string itemId, int pageSize, long? cursor)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/video/comment/list/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId)
               .AddQueryParam("cursor", (cursor ?? 0).ToString())
               .AddQueryParam("count", pageSize)
               .AddQueryParam("item_id", itemId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetVideoCommentListAsync,url:{url}");
            }

            DefaultResultModel<VideoCommentListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetVideoCommentListAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetVideoCommentListAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<VideoCommentListModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new CommentListResult
            {
                Cursor = data.cursor,
                HasMore = data.has_more,
                List = data.list?.Select(m => new CommentInfo
                {
                    CommentId = m.comment_id,
                    CommentUserId = m.comment_user_id,
                    Content = m.content,
                    CreateTime = DateTimeUtils.ToDateTime(m.create_time),
                    DiggCount = m.digg_count,
                    IsTop = m.top,
                    ReplyTotal = m.reply_comment_total,
                }).ToList(),
            };
        }

        public async Task<CommentListResult> GetVideoCommentReplyListAsync(string openId, string accessToken, string itemId, string commentId, int pageSize, long? cursor)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/video/comment/reply/list/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("cursor", (cursor ?? 0).ToString())
                .AddQueryParam("count", pageSize)
                .AddQueryParam("item_id", itemId)
                .AddQueryParam("comment_id", commentId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetVideoCommentReplyListAsync,url:{url}");
            }

            DefaultResultModel<VideoCommentListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetVideoCommentReplyListAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetVideoCommentReplyListAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<VideoCommentListModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new CommentListResult
            {
                Cursor = data.cursor,
                HasMore = data.has_more,
                List = data.list?.Select(m => new CommentInfo
                {
                    CommentId = m.comment_id,
                    CommentUserId = m.comment_user_id,
                    Content = m.content,
                    CreateTime = DateTimeUtils.ToDateTime(m.create_time),
                    DiggCount = m.digg_count,
                    IsTop = m.top,
                    ReplyTotal = m.reply_comment_total,
                }).ToList(),
            };
        }

        public async Task DeleteVideoAsync(string openId, string accessToken, string itemId)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/video/delete/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"DeleteVideoAsync,url:{url} itemId:{itemId}");
            }

            DefaultResultModel<CommonResultModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync(url, new
                {
                    item_id = itemId
                });

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"DeleteVideoAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"DeleteVideoAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<CommonResultModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);
        }

        public async Task<CreateVideoResult> CreateVideoAsync(string openId, string accessToken, string videoId, string text, string microAppId, string microAppTitle, string microAppUrl, double coverTime, string[] atUserOpenIds, string poiId, string poiName)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/video/create/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"CreateVideoAsync,url:{url}");
            }

            DefaultResultModel<CreateVideoModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync(url, new
                {
                    video_id = videoId,
                    text = text ?? string.Empty,
                    poi_id = poiId ?? string.Empty,
                    poi_name = poiName ?? string.Empty,
                    micro_app_id = microAppId ?? string.Empty,
                    micro_app_title = microAppTitle ?? string.Empty,
                    micro_app_url = microAppUrl ?? string.Empty,
                    cover_tsp = coverTime,
                    at_users = atUserOpenIds ?? new string[] { },
                });

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var requestContent = await response.RequestMessage.Content.ReadAsStringAsync();
                    logger.LogTrace($"CreateVideoAsync,url:{url} requestContent:{requestContent}");

                    logger.LogTrace($"CreateVideoAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"CreateVideoAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<CreateVideoModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new CreateVideoResult
            {
                ItemId = data.item_id,
            };
        }

        public async Task<UploadVideoResult> UploadVideoAsync(string openId, string accessToken, Stream stream)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/video/upload/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"UploadVideoAsync,url:{url}");
            }

            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            var byteContent = new ByteArrayContent(bytes);

            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(byteContent, "video", "1.mp4");

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"UploadVideoAsync2,url:{url}, bytes len:{bytes.Length}");
            }

            DefaultResultModel<UploadVideoModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(url, multipartContent);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"video upload response,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"video upload response,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<UploadVideoModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new UploadVideoResult
            {
                VideoId = data.video.video_id,
                Width = data.video.width,
                Height = data.video.height,
            };
        }

        public async Task ReplyVideoCommentAsync(string openId, string accessToken, string itemId, string commentId, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException(nameof(content));
            }

            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/video/comment/reply/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"ReplyVideoCommentAsync,url:{url}");
            }

            CommonResultModel result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync(url, new
                {
                    item_id = itemId,
                    comment_id = commentId,
                    content = content,
                });

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var requestContent = await response.RequestMessage.Content.ReadAsStringAsync();
                    logger.LogTrace($"ReplyVideoCommentAsync,url:{url} requestContent:{requestContent}");

                    logger.LogTrace($"ReplyVideoCommentAsync,url:{url} statusCode:{response.StatusCode}");

                    var responseContent = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"ReplyVideoCommentAsync,url:{url} responseContent:{responseContent}");
                }

                result = await response.Content.ReadAsAsync<CommonResultModel>();
            }

            ThrowExceptionIfError(result);
        }

        public async Task SetVideoCommentTopStatusAsync(string openId, string accessToken, string itemId, string commentId, bool isTop)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/video/comment/top/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"SetVideoCommentTopStatusAsync,url:{url}");
            }

            CommonResultModel result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync(url, new
                {
                    item_id = itemId,
                    comment_id = commentId,
                    top = isTop,
                });

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var requestContent = await response.RequestMessage.Content.ReadAsStringAsync();
                    logger.LogTrace($"SetVideoCommentTopStatusAsync,url:{url} requestContent:{requestContent}");

                    logger.LogTrace($"SetVideoCommentTopStatusAsync,url:{url} statusCode:{response.StatusCode}");

                    var responseContent = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"SetVideoCommentTopStatusAsync,url:{url} responseContent:{responseContent}");
                }

                result = await response.Content.ReadAsAsync<CommonResultModel>();
            }

            ThrowExceptionIfError(result);
        }


        public async Task<CommentListResult> GetGeneralUserVideoCommentListAsync(string openId, string accessToken, string itemId, int pageSize, long? cursor)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/item/comment/list/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId)
               .AddQueryParam("cursor", (cursor ?? 0).ToString())
               .AddQueryParam("count", pageSize)
               .AddQueryParam("item_id", itemId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetGeneralUserVideoCommentListAsync,url:{url}");
            }

            DefaultResultModel<VideoCommentListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetGeneralUserVideoCommentListAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetGeneralUserVideoCommentListAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<VideoCommentListModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new CommentListResult
            {
                Cursor = data.cursor,
                HasMore = data.has_more,
                List = data.list?.Select(m => new CommentInfo
                {
                    CommentId = m.comment_id,
                    CommentUserId = m.comment_user_id,
                    Content = m.content,
                    CreateTime = DateTimeUtils.ToDateTime(m.create_time),
                    DiggCount = m.digg_count,
                    IsTop = m.top,
                    ReplyTotal = m.reply_comment_total,
                }).ToList(),
            };
        }

        public async Task<CommentListResult> GetGeneralUserVideoCommentReplyListAsync(string openId, string accessToken, string itemId, string commentId, int pageSize, long? cursor)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/item/comment/reply/list/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("cursor", (cursor ?? 0).ToString())
                .AddQueryParam("count", pageSize)
                .AddQueryParam("item_id", itemId)
                .AddQueryParam("comment_id", commentId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetGeneralUserVideoCommentReplyListAsync,url:{url}");
            }

            DefaultResultModel<VideoCommentListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetGeneralUserVideoCommentReplyListAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetGeneralUserVideoCommentReplyListAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<VideoCommentListModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new CommentListResult
            {
                Cursor = data.cursor,
                HasMore = data.has_more,
                List = data.list?.Select(m => new CommentInfo
                {
                    CommentId = m.comment_id,
                    CommentUserId = m.comment_user_id,
                    Content = m.content,
                    CreateTime = DateTimeUtils.ToDateTime(m.create_time),
                    DiggCount = m.digg_count,
                    IsTop = m.top,
                    ReplyTotal = m.reply_comment_total,
                }).ToList(),
            };
        }

        public async Task ReplyGeneralUserVideoCommentAsync(string openId, string accessToken, string itemId, string commentId, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException(nameof(content));
            }

            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/item/comment/reply/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"ReplyGeneralUserVideoCommentAsync,url:{url}");
            }

            CommonResultModel result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync(url, new
                {
                    item_id = itemId,
                    comment_id = commentId,
                    content = content,
                });

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var requestContent = await response.RequestMessage.Content.ReadAsStringAsync();
                    logger.LogTrace($"ReplyGeneralUserVideoCommentAsync,url:{url} requestContent:{requestContent}");

                    logger.LogTrace($"ReplyGeneralUserVideoCommentAsync,url:{url} statusCode:{response.StatusCode}");

                    var responseContent = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"ReplyGeneralUserVideoCommentAsync,url:{url} responseContent:{responseContent}");
                }

                result = await response.Content.ReadAsAsync<CommonResultModel>();
            }

            ThrowExceptionIfError(result);
        }

        public async Task SendIMMessageAsync(string openId, string accessToken, string toUserId, bool isImageMessage, string messageContent)
        {
            if (string.IsNullOrEmpty(messageContent))
            {
                throw new ArgumentNullException(nameof(messageContent));
            }

            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/im/message/send/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"SendIMMessageAsync,url:{url}");
            }

            string messageType;
            if (isImageMessage)
            {
                messageType = "image";
            }
            else
            {
                messageType = "text";
            }

            CommonResultModel result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync(url, new
                {
                    to_user_id = toUserId,
                    message_type = messageType,
                    content = messageContent,
                });

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var requestContent = await response.RequestMessage.Content.ReadAsStringAsync();
                    logger.LogTrace($"SendIMMessageAsync,url:{url} requestContent:{requestContent}");

                    logger.LogTrace($"SendIMMessageAsync,url:{url} statusCode:{response.StatusCode}");

                    var responseContent = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"SendIMMessageAsync,url:{url} responseContent:{responseContent}");
                }

                result = await response.Content.ReadAsAsync<CommonResultModel>();
            }

            ThrowExceptionIfError(result);
        }

        public async Task<PoiListResult> SearchPoiAsync(string clientAccessToken, string cityName, string keyword, int pageSize, long? cursor)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/poi/search/keyword/"
                .AddQueryParam("access_token", clientAccessToken)
                .AddQueryParam("keyword", keyword)
                .AddQueryParam("city", cityName)
                .AddQueryParam("cursor", (cursor ?? 0).ToString())
                .AddQueryParam("count", pageSize);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"SearchPoiAsync,url:{url}");
            }

            DefaultResultModel<PoiSearchListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"SearchPoiAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"SearchPoiAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<PoiSearchListModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new PoiListResult
            {
                Cursor = data.cursor,
                HasMore = data.has_more,
                List = data.pois?.Select(m => new PoiModel
                {
                    PoiId = m.poi_id,
                    PoiName = m.poi_name,
                    Address = m.address,
                    City = m.city,
                    CityCode = m.city_code,
                    Province = m.province,
                    Country = m.country,
                    CountryCode = m.country_code,
                    District = m.district,
                    Location = m.location,
                }).ToList(),
            };
        }
    }
}
