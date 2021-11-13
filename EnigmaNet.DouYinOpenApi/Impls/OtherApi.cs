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

namespace EnigmaNet.DouYinOpenApi.Impls
{
    public class OtherApi : Impls.ApiBase, IOtherApi
    {
        #region models

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

        class CommonResultModel : DataBase
        {

        }

        #endregion

        public async Task SendIMMessageAsync(string openId, string accessToken, string toUserId, bool isImageMessage, string messageContent)
        {
            if (string.IsNullOrEmpty(messageContent))
            {
                throw new ArgumentNullException(nameof(messageContent));
            }

            var logger = LoggerFactory.CreateLogger<OtherApi>();

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
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<OtherApi>();

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
            var httpClient = GetClient();
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
