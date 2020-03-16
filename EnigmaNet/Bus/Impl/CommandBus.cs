using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using EnigmaNet.Exceptions;
using Newtonsoft.Json;

namespace EnigmaNet.Bus.Impl
{
    public sealed class CommandBus : ICommandExecuter, ICommandSubscriber
    {
        class MessageModel
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }

        ILogger _log;
        ILogger Logger
        {
            get
            {
                if (LoggerFactory == null)
                {
                    return null;
                }
                if (_log == null)
                {
                    _log = LoggerFactory.CreateLogger<CommandBus>();
                }
                return _log;
            }
        }

        ConcurrentDictionary<Type, object> _handlers = new ConcurrentDictionary<Type, object>();

        ConcurrentDictionary<Type, MethodInfo> _handlerMethods = new ConcurrentDictionary<Type, MethodInfo>();

        CommandBusOptions _options;

        private async Task<TResult> ExecuteRemoteAsync<TResult>(ICommand<TResult> command, string address)
        {
            var typeString = command.GetType().FullName;

            var httpClient = HttpClientFactory.CreateClient();

            var commandString = JsonConvert.SerializeObject(command, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, });

            var response = await httpClient.PostAsync(address, new StringContent(commandString));

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                if (typeof(TResult) == typeof(Empty))
                {
                    return default;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await response.Content.ReadAsAsync<TResult>();
                }
                else
                {
                    return default;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var message = await response.Content.ReadAsAsync<MessageModel>();

                throw new BizException(message.Message);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = await response.Content.ReadAsAsync<MessageModel>();

                throw new ArgumentException(message.Message);
            }
            else
            {
                throw new Exception($"remote command handler error,command type:{typeString} http code:{response.StatusCode} address:{address}");
            }
        }

        public IOptionsMonitor<CommandBusOptions> Options
        {
            set
            {
                _options = value.CurrentValue;

                value.OnChange(options =>
                {
                    _options = options;
                });
            }
        }
        public ILoggerFactory LoggerFactory { get; set; }
        public IHttpClientFactory HttpClientFactory { get; set; }

        public Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command)
        {
            var type = command.GetType();

            _handlers.TryGetValue(type, out object handler);

            if (handler != null)
            {
                if (Logger?.IsEnabled(LogLevel.Trace) == true)
                {
                    Logger.LogTrace($"match handler,command type:{type} address:{handler.GetType()}");
                }

                var methodInfo = _handlerMethods.GetValueOrDefault(type);

                var task = (Task<TResult>)(methodInfo.Invoke(handler, new object[] { command }));

                return task;
            }

            if (_options?.RemoteCommandHandlerAddresses?.Count > 0)
            {
                var typeString = type.FullName;
                var addresses = _options.RemoteCommandHandlerAddresses
                    .Where(m => typeString.StartsWith(m.Key))
                    .OrderByDescending(m => m.Key.Length)//取最接近的
                    .FirstOrDefault().Value;

                if (addresses?.Count > 0)
                {
                    var address = addresses[DateTime.Now.Millisecond % addresses.Count];

                    if (Logger?.IsEnabled(LogLevel.Trace) == true)
                    {
                        Logger.LogTrace($"match remote handler address,command type:{typeString} address:{addresses}");
                    }

                    return ExecuteRemoteAsync(command, address);
                }
            }

            if (Logger?.IsEnabled(LogLevel.Error) == true)
            {
                Logger.LogError($"no match any handler,command type:{type}");
            }

            throw new NotImplementedException($"no handler for '{type}'");
        }

        public Task SubscribeAsync<TCommand, TResult>(ICommandHandler<TCommand, TResult> handler) where TCommand : ICommand<TResult>
        {
            var type = typeof(TCommand);

            var added = _handlers.TryAdd(type, handler);

            if (added == false)
            {
                if (Logger?.IsEnabled(LogLevel.Error) == true)
                {
                    Logger.LogError($"command had subscribe,command type:{type} handler:{handler.GetType()}");
                }
            }
            else
            {
                if (Logger?.IsEnabled(LogLevel.Trace) == true)
                {
                    Logger.LogTrace($"subscribe handler,command type:{type} handler:{handler.GetType()}");
                }

                var method = typeof(ICommandHandler<TCommand, TResult>).GetMethod(nameof(ICommandHandler<TCommand, TResult>.HandleAsync));

                _handlerMethods.TryAdd(type, method);
            }

            return Task.CompletedTask;
        }
    }
}
