using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Primitives;

using EnigmaNet.AspNet.Middlewares;

namespace EnigmaNet.AspNet.Extensions
{
    public static class ApiExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseApiExceptionHandler(this IApplicationBuilder app, params string[] apiPathPrefixs)
        {
            return app.UseWhen(
                context =>
                {
                    var requestPath = context.Request.Path.ToString();

                    if (apiPathPrefixs?.Length > 0)
                    {
                        if (apiPathPrefixs.Any(pathPrefix => requestPath.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase)))
                        {
                            return true;
                        }
                    }

                    if (context.Request.Headers.TryGetValue("X-Requested-With", out StringValues xRequestedWithValues) &&
                      xRequestedWithValues.Count > 0 && "XMLHttpRequest" == xRequestedWithValues[0])
                    {
                        return true;
                    }

                    if (context.Request.Headers.TryGetValue("Accept", out StringValues acceptValues) &&
                        acceptValues.Count > 0 && acceptValues.Any(m => m.Contains("application/json")))
                    {
                        return true;
                    }

                    return false;
                },
                app2 => app2.UseMiddleware<ApiExceptionHandlerMiddleware>());
        }
    }
}
