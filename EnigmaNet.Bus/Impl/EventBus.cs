using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnigmaNet.Bus.Impl
{
    /// <summary>
    /// 事件总线（订阅和发布事件）
    /// </summary>
    /// <remarks>
    /// 发布事件为异步处理方式
    /// </remarks>
    public sealed class EventBus : IEventPublisher, IEventSubscriber
    {
        #region private

        ILogger _logger;

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

        int _threadNumber = 1;
        int _emptyWaitSecond = 1;
        bool _stop;

        ManualResetEvent autoResetEvent = new ManualResetEvent(false);

        /// <summary>
        /// 是否初始化
        /// </summary>
        bool _isInit = false;
        /// <summary>
        /// 初始化的操作锁
        /// </summary>
        object _initLocker = new object();

        /// <summary>
        /// 事件存储队列
        /// </summary>
        ConcurrentQueue<Event> _eventQueue = new ConcurrentQueue<Event>();

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="event"></param>
        void HandleEvent(Event @event)
        {
            var eventType = @event.GetType();

            _logger.LogDebug("Begin handle event,eventType={0}", eventType);

            List<object> handlerList;
            _eventHandlers.TryGetValue(eventType, out handlerList);
            if (handlerList != null && handlerList.Count > 0)
            {
                foreach (var item in handlerList)
                {
                    _logger.LogDebug("Invoke handler start,eventType={0} handler={1}", eventType, item.GetType());

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
                        _logger.LogError(ex, "Invoke handler exception,eventType={0} handler={1}", eventType, item.GetType());
                    }
                }
            }
            else
            {
                _logger.LogDebug("No handler for event, eventType={0}", eventType);
            }
        }

        /// <summary>
        /// 事件队列处理器
        /// </summary>
        void EventQueueHandler()
        {
            while (true)
            {
                if (_stop)
                {
                    return;
                }

                Event @event;
                if (!_eventQueue.TryDequeue(out @event))
                {
                    _logger.LogTrace("EventQueueHandler,empty waiting");

                    var getsignal = autoResetEvent.WaitOne(1000 * _emptyWaitSecond);
                    if (getsignal)
                    {
                        _logger.LogTrace("EventQueueHandler,get new event signal");
                    }

                    continue;
                }

                try
                {
                    HandleEvent(@event);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EventQueueHandler exception");
                }
            }
        }

        #endregion

        public EventBus(ILogger<EventBus> logger, IOptions<EventBusOptions> options)
        {
            _logger = logger;

            if (options != null)
            {
                if (options.Value.ThreadNumber > 0)
                {
                    _threadNumber = options.Value.ThreadNumber;
                }

                if (options.Value.EmptyWaitSecond > 0)
                {
                    _emptyWaitSecond = options.Value.EmptyWaitSecond;
                }
            }
        }

        #region IEventPublisher

        public Task PublishAsync<T>(T @event) where T : Event
        {
            var eventType = @event.GetType();

            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventQueue.Enqueue(@event);

                autoResetEvent.Set();

                _logger.LogDebug("Publish complete,eventType={0}", eventType);

                InitEventQueueHandlerIfNot();
            }
            else
            {
                _logger.LogDebug("Publish no do,eventType={0}", eventType);
            }

            return Task.CompletedTask;
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

            _logger.LogInformation("Subscribe event hanlder,eventType={0} handler={1}", eventType, handler.GetType());

            return Task.CompletedTask;
        }

        #endregion

        /// <summary>
        /// 初始化事件队列处理器
        /// </summary>
        public void InitEventQueueHandlerIfNot()
        {
            if (_isInit)
            {
                return;
            }

            lock (_initLocker)
            {
                if (_isInit)
                {
                    return;
                }

                _logger.LogInformation("InitEventQueueHandler start");

                //启动处理线程
                for (var i = 0; i < _threadNumber; i++)
                {
                    var t = new Thread(new ThreadStart(EventQueueHandler));
                    t.IsBackground = false;
                    t.Start();
                }

                _isInit = true;

                _logger.LogInformation("InitEventQueueHandler complete,params={0}", _threadNumber);
            }
        }

        public void StopQueueHandler()
        {
            _stop = true;
            _logger.LogInformation("StopQueueHandler,set stop true");
        }
    }
}
