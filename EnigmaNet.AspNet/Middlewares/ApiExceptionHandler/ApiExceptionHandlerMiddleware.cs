using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using EnigmaNet.Exceptions;

namespace EnigmaNet.AspNet.Middlewares.ApiExceptionHandler
{
    public class ApiExceptionHandlerMiddleware
    {
        class MessageModel
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }

        const string JsonContentType = "application/json";

        RequestDelegate _next;
        ILogger _logger;

        public ApiExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ApiExceptionHandlerMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var path = httpContext.Request.Path.ToString();

            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                Exception exception;
                if (ex is AggregateException)
                {
                    exception = ((AggregateException)ex).InnerException;
                }
                else
                {
                    exception = ex;
                }

                if (exception is BizException)
                {
                    var biz = (BizException)exception;

                    var messageString = JsonConvert.SerializeObject(
                        new MessageModel
                        {
                            Code = biz.ErrorCode,
                            Message = biz.Message
                        },
                        new JsonSerializerSettings
                        {
                            ContractResolver = new DefaultContractResolver
                            {
                                NamingStrategy = new CamelCaseNamingStrategy()
                            }
                        });

                    httpContext.Response.Clear();
                    httpContext.Response.StatusCode = Convert.ToInt32(System.Net.HttpStatusCode.Conflict);
                    httpContext.Response.ContentType = JsonContentType;
                    await httpContext.Response.WriteAsync(messageString);
                }
                else if (exception is ArgumentException)
                {
                    var arg = (ArgumentException)exception;

                    _logger.LogWarning(arg, $"request info,path:{httpContext.Request.Path} queryString:{httpContext.Request.QueryString}");

                    var messageString = JsonConvert.SerializeObject(new MessageModel
                    {
                        Message = arg.Message
                    },
                    new JsonSerializerSettings
                    {
                        ContractResolver = new DefaultContractResolver
                        {
                            NamingStrategy = new CamelCaseNamingStrategy()
                        }
                    });

                    httpContext.Response.Clear();
                    httpContext.Response.StatusCode = Convert.ToInt32(System.Net.HttpStatusCode.BadRequest);
                    httpContext.Response.ContentType = JsonContentType;
                    await httpContext.Response.WriteAsync(messageString);
                }
                else
                {
                    _logger.LogWarning(exception, $"request info,path:{httpContext.Request.Path} queryString:{httpContext.Request.QueryString}");

                    var messageString = JsonConvert.SerializeObject(new MessageModel
                    {
                        Message = exception.Message
                    },
                    new JsonSerializerSettings
                    {
                        ContractResolver = new DefaultContractResolver
                        {
                            NamingStrategy = new CamelCaseNamingStrategy()
                        }
                    });

                    httpContext.Response.Clear();
                    httpContext.Response.StatusCode = Convert.ToInt32(System.Net.HttpStatusCode.InternalServerError);
                    httpContext.Response.ContentType = JsonContentType;
                    await httpContext.Response.WriteAsync(messageString);
                }
            }
        }
    }
}
