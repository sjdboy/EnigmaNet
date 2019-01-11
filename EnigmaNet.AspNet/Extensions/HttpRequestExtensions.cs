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

            return userAgent.Contains("MicroMessenger");
        }
    }
}
