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
    public sealed class EventBus : IEventPublisher, IEventSubscriber
    {
        #region private

        ILogger _log;
        ILogger Log
        {
            get
            {
                if (_log == null)
                {
                    _log = LoggerFactory.CreateLogger<EventBus>();
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
        ConcurrentQueue<Event> _eventQueue = new ConcurrentQueue<Event>(); //Queue<Event> _eventQueue = new Queue<Event>();

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

        /// <summary>
        /// 事件队列处理器
        /// </summary>
        void EventQueueHandler()
        {
            while (true)
            {
                try
                {
                    #region 处理过程

                    Event @event;
                    if (_eventQueue.TryDequeue(out @event))
                    {
                        HandleEvent(@event);
                    }
                    else
                    {
                        var waitSecord = EmptyWaitSecond;
                        if (waitSecord < 1)
                        {
                            waitSecord = 1;
                        }
                        Thread.CurrentThread.Join(1000 * waitSecord);
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, "EventQueueHandler exception");
                }
            }
        }

        /// <summary>
        /// 初始化事件队列处理器
        /// </summary>
        void InitEventQueueHandler()
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

                Log.LogDebug("InitEventQueueHandler start");

                //启动处理线程
                var threadNumber = ThreadNumber;
                if (threadNumber < 1)
                {
                    threadNumber = 1;
                }
                for (var i = 0; i < threadNumber; i++)
                {
                    Thread t = new Thread(new ThreadStart(EventQueueHandler));
                    t.IsBackground = false;
                    t.Start();
                }

                _isInit = true;

                //if (_log.IsDebugEnabled)
                //{
                //    _log.DebugFormat("InitEventQueueHandler complete,params={0}", threadNumber);
                //}
            }
        }

        #endregion

        #region property

        /// <summary>
        /// 队列处理线程数
        /// </summary>
        public int ThreadNumber { get; set; }

        /// <summary>
        /// 队列空闲等待时间（秒）
        /// </summary>
        public int EmptyWaitSecond { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        #endregion

        public EventBus(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
        }

        #region IEventPublisher

        public async Task PublishAsync<T>(T @event) where T : Event
        {
            var eventType = @event.GetType();// typeof(T);

            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventQueue.Enqueue(@event);

                Log.LogDebug("Publish complete,eventType={0}", eventType);

                if (_isInit == false)
                {
                    InitEventQueueHandler();
                }
            }
            else
            {
                Log.LogDebug("Publish no do,eventType={0}", eventType);
            }

            //var eventType = typeof(T);
            //List<object> handlerList;
            //_eventHandlers.TryGetValue(eventType, out handlerList);
            //if (handlerList != null && handlerList.Count > 0)
            //{
            //    foreach (var item in handlerList)
            //    {
            //        ((IEventHandler<T>)item).Handle(@event);
            //    }
            //}
        }

        //public async Task PublishAsync<T>(T @event) where T : Event
        //{
        //    Publish(@event);
        //}

        #endregion

        #region IEventSubscriber

        public async Task SubscribeAsync<T>(IEventHandler<T> handler) where T : Event
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
        }

        #endregion


    }
}
