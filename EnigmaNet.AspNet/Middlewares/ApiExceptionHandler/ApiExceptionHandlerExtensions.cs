using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.AspNet.Middlewares.ApiExceptionHandler
{
    public static class ApiExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseApiExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApiExceptionHandlerMiddleware>();
        }

        public static IApplicationBuilder UseApiExceptionHandler(this IApplicationBuilder app, ApiExceptionHandlerOptions options)
        {
            return app.UseMiddleware<ApiExceptionHandlerMiddleware>(Microsoft.Extensions.Options.Options.Create(options));
        }
    }
}
