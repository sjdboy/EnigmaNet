﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.DouYinOpenApi
{
    public interface IVideoApi
    {
        Task<Models.Video.UploadVideoResult> UploadVideoAsync(string openId, string accessToken, System.IO.Stream stream);

        Task<Models.Video.CreateVideoResult> CreateVideoAsync(string openId, string accessToken, string videoId, string text, string microAppId, string microAppTitle, string microAppUrl, double coverTime, string[] atUserOpenIds, string poiId, string poiName);

        Task<Models.Video.VideoListResult> GetVideoListAsync(string openId, string accessToken, int pageSize, long? cursor);

        Task<List<Models.Video.VideoItemInfo>> GetVideoDataAsync(string openId, string accessToken, string[] itemIds);

        Task DeleteVideoAsync(string openId, string accessToken, string itemId);

        /// <summary>
        /// 获取视频评论列表（企业号）
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="accessToken"></param>
        /// <param name="itemId"></param>
        /// <param name="pageSize"></param>
        /// <param name="cursor"></param>
        /// <returns></returns>
        Task<Models.Video.CommentListResult> GetVideoCommentListAsync(string openId, string accessToken, string itemId, int pageSize, long? cursor);

        /// <summary>
        /// 获取视频评论回复列表（企业号）
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="accessToken"></param>
        /// <param name="itemId"></param>
        /// <param name="commentId"></param>
        /// <param name="pageSize"></param>
        /// <param name="cursor"></param>
        /// <returns></returns>
        Task<Models.Video.CommentListResult> GetVideoCommentReplyListAsync(string openId, string accessToken, string itemId, string commentId, int pageSize, long? cursor);

        /// <summary>
        /// 回复视频的评论（企业号）
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="accessToken"></param>
        /// <param name="itemId"></param>
        /// <param name="commentId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task ReplyVideoCommentAsync(string openId, string accessToken, string itemId, string commentId, string content);

        /// <summary>
        /// 设置视频置顶（企业号）
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="accessToken"></param>
        /// <param name="itemId"></param>
        /// <param name="commentId"></param>
        /// <param name="isTop"></param>
        /// <returns></returns>
        Task SetVideoCommentTopStatusAsync(string openId, string accessToken, string itemId, string commentId, bool isTop);

        /// <summary>
        /// 获取普通用户视频评论列表
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="accessToken"></param>
        /// <param name="itemId"></param>
        /// <param name="pageSize"></param>
        /// <param name="cursor"></param>
        /// <returns></returns>
        Task<Models.Video.CommentListResult> GetGeneralUserVideoCommentListAsync(string openId, string accessToken, string itemId, int pageSize, long? cursor);

        /// <summary>
        /// 获取普通用户视频评论回复列表
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="accessToken"></param>
        /// <param name="itemId"></param>
        /// <param name="commentId"></param>
        /// <param name="pageSize"></param>
        /// <param name="cursor"></param>
        /// <returns></returns>
        Task<Models.Video.CommentListResult> GetGeneralUserVideoCommentReplyListAsync(string openId, string accessToken, string itemId, string commentId, int pageSize, long? cursor);

        /// <summary>
        /// 回复普通用户视频的评论
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="accessToken"></param>
        /// <param name="itemId"></param>
        /// <param name="commentId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task ReplyGeneralUserVideoCommentAsync(string openId, string accessToken, string itemId, string commentId, string content);

    }
}
