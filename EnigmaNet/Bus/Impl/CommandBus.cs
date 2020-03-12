using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.Bus.Impl
{
    /// <summary>
    /// 命令总线（订阅和发送命令）
    /// </summary>
    /// <remarks>
    /// 发送命令为同步处理方式
    /// </remarks>
    [Obsolete("请使用BusV2")]
    public sealed class CommandBus : ICommandSender, ICommandSubscriber
    {
        #region private

        ILogger _log;
        ILogger Log
        {
            get
            {
                if (_log == null)
                {
                    _log = LoggerFactory.CreateLogger<CommandBus>();
                }
                return _log;
            }
        }

        /// <summary>
        /// 命令处理器集合
        /// </summary>
        /// <remarks>
        /// key:命令类型
        /// value:处理器列表
        /// </remarks>
        Dictionary<Type, object> _commandHandlers = new Dictionary<Type, object>();
        /// <summary>
        /// 命令处理器集合操作锁
        /// </summary>
        object _locker = new object();

        #endregion

        public ILoggerFactory LoggerFactory { get; set; }

        public CommandBus() { }

        //public CommandBus(ILoggerFactory loggerFactory)
        //{
        //    _log = loggerFactory.CreateLogger<CommandBus>();
        //}

        #region ICommandSender

        public async Task SendAsync<T>(T command) where T : Command
        {
            var commandType = typeof(T);

            object handler;
            _commandHandlers.TryGetValue(commandType, out handler);

            if (handler != null)
            {
                if (Log.IsEnabled(LogLevel.Debug))
                {
                    Log.LogDebug("Send command start,commandType:{0} hanlder:{1}",  commandType, handler.GetType());
                }

                await ((ICommandHandler<T>)handler).HandleAsync(command);

                if (Log.IsEnabled(LogLevel.Debug))
                {
                    Log.LogDebug("Send command complete,commandType:{0} hanlder:{1}",  commandType, handler.GetType());
                }
            }
            else
            {
                if (Log.IsEnabled(LogLevel.Critical))
                {
                    Log.LogCritical("CommandHandler not impl,commandType:{0} ", commandType);
                }

                throw new NotImplementedException(string.Format("CommandHandler not impl,commandType:{0} ", commandType));
            }
        }

        #endregion

        #region ICommandSubscriber

        public void Subscribe<T>(ICommandHandler<T> handler) where T : Command
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            var commandType = typeof(T);

            lock (_locker)
            {
                if (_commandHandlers.ContainsKey(commandType))
                {
                    throw new NotSupportedException(string.Format("command handler already exists, commandType={0}", commandType));
                }
                else
                {
                    _commandHandlers.Add(commandType, handler);

                    if (Log.IsEnabled(LogLevel.Debug))
                    {
                        Log.LogDebug("Subscribe command handler,commandType:{0} handler:{1}", commandType, handler.GetType());
                    }
                }
            }
        }

        #endregion

    }
}
