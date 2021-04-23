using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using System.Net;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;

using Newtonsoft.Json;

using EnigmaNet.Bus;
using EnigmaNet.Exceptions;

namespace EnigmaNet.MicroserviceBus.Middlewares
{
    class CommandExecuteHandler
    {
        class MessageModel
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }

        RequestDelegate _next;
        Options.AuthenticationOptions _authenticationOptions;
        ILogger logger;
        ICommandExecuter _commandExecuter;

        static async Task WriteJsonAsync(HttpResponse response, HttpStatusCode statusCode, object obj)
        {
            response.StatusCode = Convert.ToInt32(statusCode);
            response.ContentType = "application/json";
            await response.WriteAsync(JsonConvert.SerializeObject(obj));
        }

        bool CheckPermission(List<Claim> scopes, string commandTypeName)
        {
            if (scopes.Any(m => m.Value == commandTypeName))
            {
                return true;
            }

            foreach (var scope in scopes.Where(m => m.Value.EndsWith("*")))
            {
                var scopePrefix = scope.Value.TrimEnd('*');
                if (commandTypeName.StartsWith(scopePrefix))
                {
                    return true;
                }
            }

            return false;
        }

        public CommandExecuteHandler(RequestDelegate next, ILoggerFactory loggerFactory,
            IOptions<Options.AuthenticationOptions> options,
            ICommandExecuter commandExecuter)
        {
            _next = next;
            logger = loggerFactory.CreateLogger<CommandExecuteHandler>();
            _authenticationOptions = options.Value;
            _commandExecuter = commandExecuter;
        }

        public async Task Invoke(HttpContext context)
        {
            string name;
            if (context.Request.Query.ContainsKey("name"))
            {
                name = context.Request.Query["name"];
            }
            else
            {
                name = context.Request.Path.Value.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug($"receive a command,name:{name}");
            }

            ClaimsPrincipal user;
            {
                var jwtAuthResult = await context.AuthenticateAsync(Utils.AuthUtils.JwtSchemeName);
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug($"auth jwt result,succeeded:{jwtAuthResult?.Succeeded}");
                    if (jwtAuthResult.Succeeded == false)
                    {
                        logger.LogDebug($"auth jwt fail info:{jwtAuthResult?.Failure?.Message}");
                    }
                }

                if (jwtAuthResult.Succeeded)
                {
                    user = jwtAuthResult.Principal;
                }
                else
                {
                    var introspectionAuthResult = await context.AuthenticateAsync(Utils.AuthUtils.IntrospectionSchemeName);
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug($"auth introspection result,succeeded:{introspectionAuthResult?.Succeeded}");
                        if (introspectionAuthResult.Succeeded == false)
                        {
                            logger.LogDebug($"auth introspection fail info:{introspectionAuthResult?.Failure?.Message}");
                        }
                    }

                    if (introspectionAuthResult.Succeeded)
                    {
                        user = introspectionAuthResult.Principal;
                    }
                    else
                    {
                        user = null;
                    }
                }
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                if (user != null)
                {
                    logger.LogDebug($"user info,name:{user.Identity.Name} isAuthed:{user.Identity.IsAuthenticated} authType:{user.Identity.AuthenticationType}");

                    logger.LogDebug($"claims info, { string.Join("; ", user.Claims.Select(m => $"{m.Type}:{m.Value}"))}");
                }
                else
                {
                    logger.LogDebug("user is null");
                }
            }

            if ((user == null || !user.Identity.IsAuthenticated))
            {
                logger.LogDebug("user is null or unauth");
                context.Response.StatusCode = Convert.ToInt32(HttpStatusCode.Unauthorized);
                return;
            }

            var scopes = user.Claims.Where(m => m.Type == "scope").ToList();
            if (!CheckPermission(scopes, name))
            {
                logger.LogDebug("no permission");

                context.Response.StatusCode = Convert.ToInt32(HttpStatusCode.Forbidden);
                return;
            }

            string content;
            using (var reader = new StreamReader(context.Request.Body))
            {
                content = await reader.ReadToEndAsync();
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug($"receive a command,content:{content}");
            }

            var commandObject = JsonConvert.DeserializeObject(content, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            if (commandObject.GetType().FullName != name)
            {
                var messagetText = $"command name inconsistent:'{commandObject.GetType().FullName}','{name}'";

                logger.LogDebug("command name inconsistent");

                var message = new MessageModel
                {
                    Message = messagetText,
                };

                await WriteJsonAsync(context.Response, HttpStatusCode.BadRequest, message);

                return;
            }

            object result;

            try
            {
                result = await CommandUtils.ExecuteAsync(_commandExecuter, commandObject);
            }
            catch (Exception ex)
            {
                Exception exception;
                if (ex is AggregateException)
                {
                    var agg = ((AggregateException)ex);

                    exception = agg.Flatten().InnerExceptions
                        .Where(m => !(m is AggregateException))
                        .FirstOrDefault();
                }
                else
                {
                    exception = ex;
                }

                if (exception is BizException)
                {
                    var biz = (BizException)exception;

                    logger.LogDebug(biz, "biz error");

                    await WriteJsonAsync(context.Response,
                        HttpStatusCode.Conflict, new MessageModel
                        {
                            Message = biz.Message,
                        });

                    return;
                }
                else if (exception is ArgumentException)
                {
                    var arg = (ArgumentException)exception;

                    logger.LogDebug(arg, "arg error");

                    await WriteJsonAsync(context.Response,
                         HttpStatusCode.BadRequest,
                         new MessageModel
                         {
                             Message = arg.Message,
                         });

                    return;
                }
                else
                {
                    logger.LogError(exception, "error");

                    await WriteJsonAsync(context.Response,
                         HttpStatusCode.InternalServerError,
                         new MessageModel
                         {
                             Message = exception.Message,
                         });

                    return;
                }
            }

            logger.LogDebug("return result");

            if (result is Empty)
            {
                context.Response.StatusCode = Convert.ToInt32(HttpStatusCode.NoContent);
                return;
            }
            else
            {
                await WriteJsonAsync(
                    context.Response,
                     HttpStatusCode.OK,
                     result);

                return;
            }
        }
    }
}
