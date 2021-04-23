using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EnigmaNet.Bus.Impl
{
    public class CommandBus : ICommandExecuter, ICommandSubscriber
    {
        ILogger _logger;

        protected ConcurrentDictionary<Type, object> Handlers = new ConcurrentDictionary<Type, object>();

        protected ConcurrentDictionary<Type, MethodInfo> HandlerMethods = new ConcurrentDictionary<Type, MethodInfo>();

        public CommandBus(ILogger<CommandBus> logger)
        {
            _logger = logger;
        }

        public virtual Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command)
        {
            var type = command.GetType();

            Handlers.TryGetValue(type, out object handler);

            if (handler != null)
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace($"match handler,command type:{type} handler:{handler.GetType()}");
                }

                var methodInfo = HandlerMethods.GetValueOrDefault(type);

                var task = (Task<TResult>)(methodInfo.Invoke(handler, new object[] { command }));

                return task;
            }

            throw new NotImplementedException($"no handler for '{type}'");
        }

        public Task SubscribeAsync<TCommand, TResult>(ICommandHandler<TCommand, TResult> handler) where TCommand : ICommand<TResult>
        {
            var type = typeof(TCommand);

            var added = Handlers.TryAdd(type, handler);

            if (added == false)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError($"command had subscribe,command type:{type} handler:{handler.GetType()}");
                }
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace($"subscribe handler,command type:{type} handler:{handler.GetType()}");
                }

                var method = typeof(ICommandHandler<TCommand, TResult>).GetMethod(nameof(ICommandHandler<TCommand, TResult>.HandleAsync));

                HandlerMethods.TryAdd(type, method);
            }

            return Task.CompletedTask;
        }
    }
}
