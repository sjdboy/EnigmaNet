using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.AspNet.Extensions
{
    public static class HttpRequestExtensions
    {
        public static string GetUserAgent(this HttpRequest httpRequest)
        {
            return httpRequest.Headers["User-Agent"];
        }

        public static bool IsMicroMessager(this HttpRequest httpRequest)
        {
            var userAgent = httpRequest.GetUserAgent();

            return userAgent.Contains("MicroMessenger")
                && !userAgent.Contains("wxwork") //非企业微信
                ;
        }

        public static bool IsWxWork(this HttpRequest httpRequest)
        {
            var userAgent = httpRequest.GetUserAgent();

            return userAgent.Contains("wxwork");
        }

        public static string GetRequestUrl(this HttpRequest source, bool withHost = false)
        {
            var path = $"{source.PathBase}{source.Path}{source.QueryString}";

            if (withHost)
            {
                return $"{(source.IsHttps ? "https" : "http")}://{source.Host}" + path;
            }
            else
            {
                return path;
            }
        }
    }
}
