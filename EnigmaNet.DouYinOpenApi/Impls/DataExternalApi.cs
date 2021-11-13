using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;

using EnigmaNet.DouYinOpenApi.Models.DataExternals;
using EnigmaNet.Extensions;

namespace EnigmaNet.DouYinOpenApi.Impls
{
    public class DataExternalApi : ApiBase, IDataExternalApi
    {
        class UserVideoDataModel : DataBase
        {
            public class ItemModel
            {
                public DateTime date { get; set; }
                public int new_issue { get; set; }
                public int new_play { get; set; }
                public int total_issue { get; set; }
            }

            public List<ItemModel> result_list { get; set; }
        }

        class UserFansDataModel : DataBase
        {
            public class ItemModel
            {
                public DateTime date { get; set; }
                public int total_fans { get; set; }
                public int new_fans { get; set; }
            }

            public List<ItemModel> result_list { get; set; }
        }

        class UserLikeDataModel : DataBase
        {
            public class ItemModel
            {
                public DateTime date { get; set; }
                public int new_like { get; set; }
            }

            public List<ItemModel> result_list { get; set; }
        }

        class UserCommentDataModel : DataBase
        {
            public class ItemModel
            {
                public DateTime date { get; set; }
                public int new_comment { get; set; }
            }

            public List<ItemModel> result_list { get; set; }
        }

        class UserShareDataModel : DataBase
        {
            public class ItemModel
            {
                public DateTime date { get; set; }
                public int new_share { get; set; }
            }

            public List<ItemModel> result_list { get; set; }
        }

        class UserProfileDataModel : DataBase
        {
            public class ItemModel
            {
                public DateTime date { get; set; }
                public int profile_uv { get; set; }
            }

            public List<ItemModel> result_list { get; set; }
        }

        public async Task<UserCommentDataResult> GetUserCommentDataAsync(string openId, string accessToken, int dataType)
        {
            var logger = LoggerFactory.CreateLogger<DataExternalApi>();

            var url = Api + "/data/external/user/comment/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("date_type", dataType);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserCommentDataAsync,url:{url}");
            }

            DefaultResultModel<UserCommentDataModel> result;
            var httpClient = GetClient();
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetUserCommentDataAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetUserCommentDataAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<UserCommentDataModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new UserCommentDataResult
            {
                List = data.result_list?.Select(m => new UserCommentDataItem
                {
                    Date = m.date,
                    NewComment = m.new_comment,
                }).ToList(),
            };
        }

        public async Task<UserFansDataResult> GetUserFansDataAsync(string openId, string accessToken, int dataType)
        {
            var logger = LoggerFactory.CreateLogger<DataExternalApi>();

            var url = Api + "/data/external/user/fans/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("date_type", dataType);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserFansDataAsync,url:{url}");
            }

            DefaultResultModel<UserFansDataModel> result;
            var httpClient = GetClient();
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetUserFansDataAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetUserFansDataAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<UserFansDataModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new UserFansDataResult
            {
                List = data.result_list?.Select(m => new UserFansDataItem
                {
                    Date = m.date,
                    NewFans = m.new_fans,
                    TotalFans = m.total_fans,
                }).ToList(),
            };
        }

        public async Task<UserLikeDataResult> GetUserLikeDataAsync(string openId, string accessToken, int dataType)
        {
            var logger = LoggerFactory.CreateLogger<DataExternalApi>();

            var url = Api + "/data/external/user/like/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("date_type", dataType);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserLikeDataAsync,url:{url}");
            }

            DefaultResultModel<UserLikeDataModel> result;
            var httpClient = GetClient();
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetUserLikeDataAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetUserLikeDataAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<UserLikeDataModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new UserLikeDataResult
            {
                List = data.result_list?.Select(m => new UserLikeDataItem
                {
                    Date = m.date,
                    NewLike = m.new_like,
                }).ToList(),
            };
        }

        public async Task<UserProfileDataResult> GetUserProfileDataAsync(string openId, string accessToken, int dataType)
        {
            var logger = LoggerFactory.CreateLogger<DataExternalApi>();

            var url = Api + "/data/external/user/profile/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("date_type", dataType);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserProfileDataAsync,url:{url}");
            }

            DefaultResultModel<UserProfileDataModel> result;
            var httpClient = GetClient();
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetUserProfileDataAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetUserProfileDataAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<UserProfileDataModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new UserProfileDataResult
            {
                List = data.result_list?.Select(m => new UserProfileDataItem
                {
                    Date = m.date,
                    ProfileUv = m.profile_uv,
                }).ToList(),
            };
        }

        public async Task<UserShareDataResult> GetUserShareDataAsync(string openId, string accessToken, int dataType)
        {
            var logger = LoggerFactory.CreateLogger<DataExternalApi>();

            var url = Api + "/data/external/user/share/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("date_type", dataType);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserShareDataAsync,url:{url}");
            }

            DefaultResultModel<UserShareDataModel> result;
            var httpClient = GetClient();
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetUserShareDataAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetUserShareDataAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<UserShareDataModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new UserShareDataResult
            {
                List = data.result_list?.Select(m => new UserShareDataItem
                {
                    Date = m.date,
                    NewShare = m.new_share,
                }).ToList(),
            };
        }

        public async Task<UserVideoDataResult> GetUserVideoDataAsync(string openId, string accessToken, int dataType)
        {
            var logger = LoggerFactory.CreateLogger<DataExternalApi>();

            var url = Api + "/data/external/user/item/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId)
                .AddQueryParam("date_type", dataType);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserVideoDataAsync,url:{url}");
            }

            DefaultResultModel<UserVideoDataModel> result;
            var httpClient = GetClient();
            {
                var response = await httpClient.GetAsync(url);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace($"GetUserVideoDataAsync,url:{url} statusCode:{response.StatusCode}");

                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogTrace($"GetUserVideoDataAsync,url:{url} content:{content}");
                }

                result = await response.Content.ReadAsAsync<DefaultResultModel<UserVideoDataModel>>();
            }

            var data = result.data;
            ThrowExceptionIfError(data);

            return new UserVideoDataResult
            {
                List = data.result_list?.Select(m => new UserVideoDataItem
                {
                    Date = m.date,
                    NewIssue = m.new_issue,
                    NewPlay = m.new_play,
                    TotalIssue = m.total_issue,
                }).ToList(),
            };
        }
    }
}
