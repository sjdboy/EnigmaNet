﻿using System;
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

        class CreateVideoModel : DataBase
        {
            public string item_id { get; set; }
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
                throw new DouYinApiException(result.error_code, result.description);
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

        public async Task<AccessTokenResult> GetOAuthAccessTokenAsync(string code)
        {
            var logger = LoggerFactory.CreateLogger<ApiClient>();

            var url = Api + "/oauth/access_token/"
                .AddQueryParam("client_key", Key)
                .AddQueryParam("client_secret", Secret)
                .AddQueryParam("code", code)
                .AddQueryParam("grant_type", "authorization_code");

            logger.LogDebug($"GetOAuthAccessTokenAsync,url:{url}");

            DefaultResultModel<AccessTokenModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                logger.LogDebug($"GetOAuthAccessTokenAsync,StatusCode:{response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();

                logger.LogDebug($"GetOAuthAccessTokenAsync,content:{content}");

                result = await response.Content.ReadAsAsync<DefaultResultModel<AccessTokenModel>>();

                logger.LogDebug($"GetOAuthAccessTokenAsync,result:{(result != null ? Newtonsoft.Json.JsonConvert.SerializeObject(result) : "null")}");
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
            var url = Api + "/oauth/refresh_token/"
                .AddQueryParam("client_key", Key)
                .AddQueryParam("refresh_token ", refreshToken)
                .AddQueryParam("grant_type", "refresh_token");

            DefaultResultModel<RefreshTokenModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
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
            var url = Api + "​/oauth​/client_token​/"
                .AddQueryParam("client_key", Key)
                .AddQueryParam("client_secret", Secret)
                .AddQueryParam("grant_type", "client_credential");

            DefaultResultModel<ClientTokenModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
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
            var url = Api + "​/oauth/userinfo/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            DefaultResultModel<UserInfoModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
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
            var url = Api + "​/following/list/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId)
               .AddQueryParam("cursor", (cursor ?? 0).ToString())
               .AddQueryParam("count ", pageSize)
               ;

            DefaultResultModel<FollowingListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
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
            var url = Api + "​/fans/list/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId)
               .AddQueryParam("cursor",( cursor ?? 0).ToString())
               .AddQueryParam("count ", pageSize)
               ;

            DefaultResultModel<FanListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
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
            var url = Api + "/fans/data/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId);

            DefaultResultModel<FanDataModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                result = await response.Content.ReadAsAsync<DefaultResultModel<FanDataModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            var fansData = data.fans_data;

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
            var url = Api + "/video/list/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("cursor", (cursor ?? 0).ToString())
                .AddQueryParam("count", pageSize);

            DefaultResultModel<VideoListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                result = await response.Content.ReadAsAsync<DefaultResultModel<VideoListModel>>();
            }

            var data = result.data;
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
            var url = Api + "/video/data/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            DefaultResultModel<VideoDataModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync(url, new
                {
                    item_ids = itemIds
                });

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
            var url = Api + "/video/comment/list/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId)
               .AddQueryParam("cursor", (cursor ?? 0).ToString())
               .AddQueryParam("count", pageSize)
               .AddQueryParam("item_id", itemId);

            DefaultResultModel<VideoCommentListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
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
            var url = Api + "/video/comment/reply/list/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("cursor", (cursor ?? 0).ToString())
                .AddQueryParam("count", pageSize)
                .AddQueryParam("item_id", itemId)
                .AddQueryParam("comment_id", commentId);

            DefaultResultModel<VideoCommentListModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
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

        public async Task DeleteVideoAsync(string openId, string accessToken, string[] itemIds)
        {
            var url = Api + "/video/delete/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            DefaultResultModel<DataBase> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync(url, new
                {
                    item_id = itemIds
                });
                result = await response.Content.ReadAsAsync<DefaultResultModel<DataBase>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);
        }

        public async Task<CreateVideoResult> CreateVideoAsync(string openId, string accessToken, string videoId, string text, string microAppId, string microAppTitle, string microAppUrl, double coverTime, string[] atUserOpenIds)
        {
            var url = Api + "/video/create/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            DefaultResultModel<CreateVideoModel> result;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync(url, new
                {
                    video_id = videoId,
                    text = text,
                    micro_app_id = microAppId,
                    micro_app_title = microAppTitle,
                    micro_app_url = microAppUrl,
                    cover_tsp = coverTime,
                    at_users = atUserOpenIds,
                });
                result = await response.Content.ReadAsAsync<DefaultResultModel<CreateVideoModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new CreateVideoResult
            {
                ItemId = data.item_id,
            };
        }

        public Task<UploadVideoResult> UploadVideoAsync(string openId, string accessToken, Stream stream)
        {
            throw new NotImplementedException();
        }

        public Task ReplyVideoCommentAsync(string openId, string accessToken, string itemId, string commentId, string content)
        {
            throw new NotImplementedException();
        }

        public Task SendIMMessageAsync(string openId, string accessToken, string toUserId, bool isImageMessage, string messageContent)
        {
            throw new NotImplementedException();
        }

        public Task SetVideoCommentTopStatusAsync(string openId, string accessToken, string itemId, string commentId, bool isTop)
        {
            throw new NotImplementedException();
        }
    }
}