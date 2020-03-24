using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi
{
    /// <summary>
    /// 状态码
    /// </summary>
    /// <remarks>
    /// https://open.douyin.com/platform/doc/OpenAPI-status_code
    /// </remarks>
    public static class ErrorCodes
    {
        /// <summary>
        /// 系统繁忙，此时请开发者稍候再试
        /// </summary>
        public const int SystemBusy = 2100004;

        /// <summary>
        /// 参数不合法
        /// </summary>
        public const int ArgumentError = 2100005;

        /// <summary>
        /// 无权限操作
        /// </summary>
        public const int NoPermission = 2100007;

        /// <summary>
        /// quota已用完
        /// </summary>
        public const int QuotaOut = 2190001;

        /// <summary>
        /// 用户被禁封使用该操作
        /// </summary>
        public const int UserForbidden = 2100009;

        /// <summary>
        /// 应用未获得该能力
        /// </summary>
        public const int AppIsNotGetPermission = 2190004;

    }
}
