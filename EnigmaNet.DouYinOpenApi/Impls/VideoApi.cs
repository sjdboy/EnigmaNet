using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using Microsoft.Extensions.Logging;

using EnigmaNet.Extensions;
using EnigmaNet.DouYinOpenApi.Models.Video;
using EnigmaNet.Utils;

namespace EnigmaNet.DouYinOpenApi.Impls
{
    public class VideoApi : ApiBase, IVideoApi
    {
        class CommonResultModel : DataBase
        {

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
            public int video_status { get; set; }
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

        public async Task<VideoListResult> GetVideoListAsync(string openId, string accessToken, int pageSize, long? cursor)
        {
            var logger = LoggerFactory.CreateLogger<VideoApi>();

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
            var httpClient = GetClient();
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
                    VideoStatus = Enum.Parse<VideoStatus>(m.video_status.ToString()),
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
            var logger = LoggerFactory.CreateLogger<VideoApi>();

            var url = Api + "/video/data/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"GetVideoDataAsync,url:{url}");
            }

            DefaultResultModel<VideoDataModel> result;
            var httpClient = GetClient();
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
                VideoStatus = Enum.Parse<VideoStatus>(m.video_status.ToString()),
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
            var logger = LoggerFactory.CreateLogger<VideoApi>();

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
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<VideoApi>();

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
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<VideoApi>();

            var url = Api + "/video/delete/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"DeleteVideoAsync,url:{url} itemId:{itemId}");
            }

            DefaultResultModel<CommonResultModel> result;
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<VideoApi>();

            var url = Api + "/video/create/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"CreateVideoAsync,url:{url}");
            }

            DefaultResultModel<CreateVideoModel> result;
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<VideoApi>();

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
            var httpClient = GetClient();
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

            var logger = LoggerFactory.CreateLogger<VideoApi>();

            var url = Api + "/video/comment/reply/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"ReplyVideoCommentAsync,url:{url}");
            }

            CommonResultModel result;
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<VideoApi>();

            var url = Api + "/video/comment/top/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"SetVideoCommentTopStatusAsync,url:{url}");
            }

            CommonResultModel result;
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<VideoApi>();

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
            var httpClient = GetClient();
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
            var logger = LoggerFactory.CreateLogger<VideoApi>();

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
            var httpClient = GetClient();
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

            var logger = LoggerFactory.CreateLogger<VideoApi>();

            var url = Api + "/item/comment/reply/"
               .AddQueryParam("access_token", accessToken)
               .AddQueryParam("open_id", openId);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace($"ReplyGeneralUserVideoCommentAsync,url:{url}");
            }

            CommonResultModel result;
            var httpClient = GetClient();
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
    }
}
