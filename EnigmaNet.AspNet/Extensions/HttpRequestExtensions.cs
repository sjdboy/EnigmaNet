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

        public static bool IsIPhone(this HttpRequest httpRequest)
        {
            var userAgent = httpRequest.GetUserAgent(); if (string.IsNullOrEmpty(userAgent))
            {
                return false;
            }

            return userAgent.Contains("iPhone") || userAgent.Contains("iphone");
        }

        public static bool IsAndroid(this HttpRequest httpRequest)
        {
            var userAgent = httpRequest.GetUserAgent();
            if (string.IsNullOrEmpty(userAgent))
            {
                return false;
            }

            return userAgent.Contains("Android") || userAgent.Contains("android");
        }

        public static bool IsMicroMessager(this HttpRequest httpRequest)
        {
            var userAgent = httpRequest.GetUserAgent();
            if (string.IsNullOrEmpty(userAgent))
            {
                return false;
            }

            return userAgent.Contains("MicroMessenger")
                && !userAgent.Contains("wxwork") //非企业微信
                ;
        }

        public static bool IsMiniProgram(this HttpRequest httpRequest)
        {
            var userAgent = httpRequest.GetUserAgent();
            if (string.IsNullOrEmpty(userAgent))
            {
                return false;
            }

            return userAgent.Contains("miniProgram");
        }

        public static bool IsWxWork(this HttpRequest httpRequest)
        {
            var userAgent = httpRequest.GetUserAgent();
            if (string.IsNullOrEmpty(userAgent))
            {
                return false;
            }

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

        public static string GetRequestUrlWithoutProtocol(this HttpRequest source)
        {
            return $"//{source.Host}{source.PathBase}{source.Path}{source.QueryString}";
        }
    }
}
