using EnigmaNet.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Primitives;

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
        ApiExceptionHandlerOptions _options;
        ILogger _logger;

        public ApiExceptionHandlerMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IOptions<ApiExceptionHandlerOptions> options)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ApiExceptionHandlerMiddleware>();
            _options = options.Value;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var path = httpContext.Request.Path.ToString();

            _logger.LogDebug($"apiExceptionHandler invoke:path:{path}");

            bool isApi;

            if (_options.ApiPathPrefixs?.Count > 0 &&
                _options.ApiPathPrefixs.Any(m => path.StartsWith(m, StringComparison.OrdinalIgnoreCase)))
            {
                isApi = true;
            }
            else if (httpContext.Request.Headers.TryGetValue("X-Requested-With", out StringValues values) &&
                   values.Count > 0 &&
                   "XMLHttpRequest" == values[0])
            {
                isApi = true;
            }
            else
            {
                isApi = false;
            }

            if (isApi)
            {
                try
                {
                    await _next(httpContext);
                }
                catch (BizException biz)
                {
                    var messageString = JsonConvert.SerializeObject(
                        new MessageModel
                        {
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
                catch (ArgumentException arg)
                {
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
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"request info,path:{httpContext.Request.Path} queryString:{httpContext.Request.QueryString}");

                    var messageString = JsonConvert.SerializeObject(new MessageModel
                    {
                        Message = ex.Message
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
            else
            {
                await _next(httpContext);
            }
        }

    }
}
