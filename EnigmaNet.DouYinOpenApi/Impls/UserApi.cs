using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using EnigmaNet.DouYinOpenApi.Models;
using EnigmaNet.DouYinOpenApi.Models.Fans;
using EnigmaNet.DouYinOpenApi.Models.Following;
using EnigmaNet.Extensions;

namespace EnigmaNet.DouYinOpenApi.Impls
{
    public class UserApi : OtherApi, IUserApi
    {
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

        public async Task<UserInfo> GetUserInfoAsync(string openId, string accessToken)
        {
            var logger = LoggerFactory.CreateLogger<OtherApi>();

            var url = Api + "​/oauth/userinfo/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetUserInfoAsync,url:{url}");
            }

            DefaultResultModel<UserInfoModel> result;
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<OtherApi>();

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
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<OtherApi>();

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
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<OtherApi>();

            var url = Api + "/fans/data/"
                .AddQueryParam("access_token", accessToken)
                .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetFansDataAsync,url:{url}");
            }

            DefaultResultModel<FanDataModel> result;
            var httpClient = GetClient();
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

    }
}
