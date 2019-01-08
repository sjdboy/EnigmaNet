using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EnigmaNet.Bus.Impl
{
    /// <summary>
    /// 事件总线（订阅和发布事件）
    /// </summary>
    /// <remarks>
    /// 发布事件为异步处理方式
    /// </remarks>
    public sealed class EventBus2 : IEventPublisher, IEventSubscriber
    {
        #region private

        ILogger _log;
        ILogger Log
        {
            get
            {
                if (_log == null)
                {
                    _log = LoggerFactory.CreateLogger<EventBus2>();
                }
                return _log;
            }
        }

        /// <summary>
        /// 事件处理器集合
        /// </summary>
        /// <remarks>
        /// key:事件类型
        /// value:处理器列表
        /// </remarks>
        Dictionary<Type, List<object>> _eventHandlers = new Dictionary<Type, List<object>>();
        /// <summary>
        /// 事件处理器集合操作锁
        /// </summary>
        object _eventHandlersLocker = new object();

        ///// <summary>
        ///// 是否初始化
        ///// </summary>
        //bool _isInit = false;
        ///// <summary>
        ///// 初始化的操作锁
        ///// </summary>
        //object _initLocker = new object();

        ///// <summary>
        ///// 事件存储队列
        ///// </summary>
        //ConcurrentQueue<Event> _eventQueue = new ConcurrentQueue<Event>(); //Queue<Event> _eventQueue = new Queue<Event>();

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="event"></param>
        void HandleEvent(Event @event)
        {
            var eventType = @event.GetType();

            Log.LogDebug("Begin handle event,eventType={0}", eventType);

            List<object> handlerList;
            _eventHandlers.TryGetValue(eventType, out handlerList);
            if (handlerList != null && handlerList.Count > 0)
            {
                foreach (var item in handlerList)
                {
                    Log.LogDebug("Invoke handler start,eventType={0} handler={1}", eventType, item.GetType());

                    var task = (Task)typeof(IEventHandler<>)
                            .MakeGenericType(eventType)
                            .GetMethod("HandleAsync")
                            .Invoke(item, new object[] { @event });
                    try
                    {
                        //task.Start();
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Log.LogError(ex, "Invoke handler exception,eventType={0} handler={1}", eventType, item.GetType());

                        continue;
                    }

                    //if (_log.IsDebugEnabled)
                    //{
                    //    _log.DebugFormat("Invoke handler complete,eventType={0} handler={1}", eventType, item.GetType());
                    //}
                }
            }
            else
            {
                Log.LogDebug("No handler for event, eventType={0}", eventType);
            }
        }

        #endregion

        #region property

        public ILoggerFactory LoggerFactory { get; set; }

        //public IOptions<EventBus2Options> EventBus2Options { get; set; }

        #endregion

        #region IEventPublisher

        public async Task PublishAsync<T>(T @event) where T : Event
        {
            //var eventType = typeof(T);
            var eventType = @event.GetType();

            List<object> handlerList;
            if (_eventHandlers.TryGetValue(eventType, out handlerList))
            {
                if (handlerList != null && handlerList.Count > 0)
                {
                    foreach (var item in handlerList)
                    {
                        _log.LogDebug($"start handle event error,eventType:{@event.GetType()} handlerType:{item.GetType()}");

                        var task = (Task)typeof(IEventHandler<>)
                           .MakeGenericType(eventType)
                           .GetMethod("HandleAsync")
                           .Invoke(item, new object[] { @event });

                        try
                        {
                            task.Wait();
                        }
                        catch (Exception ex)
                        {
                            Log.LogError(ex, "Invoke handler exception,eventType={0} handler={1}", eventType, item.GetType());

                            continue;
                        }

                        //try
                        //{
                        //    await ((IEventHandler<T>)item).HandleAsync(@event);
                        //}
                        //catch (Exception ex)
                        //{
                        //    _log.LogError(ex, $"handle event error,msg:{ex.Message} eventType:{@event.GetType()}");
                        //}
                    }
                }
            }

        }

        #endregion

        #region IEventSubscriber

        public Task SubscribeAsync<T>(IEventHandler<T> handler) where T : Event
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            var eventType = typeof(T);

            lock (_eventHandlersLocker)
            {
                List<object> handlerList;
                _eventHandlers.TryGetValue(eventType, out handlerList);

                if (handlerList == null)
                {
                    handlerList = new List<object>();
                    _eventHandlers.Add(eventType, handlerList);
                }

                handlerList.Add(handler);
            }

            Log.LogDebug("Subscribe event hanlder,eventType={0} handler={1}", eventType, handler.GetType());

            return Task.CompletedTask;
        }

        #endregion
    }
}
