using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using EnigmaNetCore.Bus;
using EnigmaNetCore.Utils;

namespace EnigmaNetCore.RabbitMQBus
{
    public sealed class RabbitMQEventBus1 : IEventPublisher, IEventSubscriber
    {
        #region private

        #region class

        [Serializable]
        class EventDataModel
        {
            public string HandlerTypeString { get; set; }
            public Event Event { get; set; }
        }

        class XDeath
        {
            public long Count { get; set; }
            public string Exchange { get; set; }
            public string Queue { get; set; }
            public string Reason { get; set; }
            public List<string> RoutingKeys { get; set; }
            public AmqpTimestamp Time { get; set; }

            public static List<XDeath> GetDeathData(IDictionary<string, object> headers)
            {
                if (headers == null)
                {
                    return null;
                }
                if (!headers.ContainsKey("x-death"))
                {
                    return null;
                }

                var datas = headers["x-death"] as System.Collections.IEnumerable;

                var list = new List<XDeath>();

                if (datas != null)
                {
                    foreach (IDictionary<string, object> data in datas)
                    {
                        var item = new XDeath
                        {
                            Count = (long)data["count"],
                            Exchange = Encoding.UTF8.GetString((byte[])data["exchange"]),
                            Queue = Encoding.UTF8.GetString((byte[])data["queue"]),
                            Reason = Encoding.UTF8.GetString((byte[])data["reason"]),
                            RoutingKeys = new List<string>(),
                            Time = (AmqpTimestamp)data["time"]
                        };

                        var routingKeys = data["routing-keys"] as System.Collections.IEnumerable;

                        foreach (byte[] key in routingKeys)
                        {
                            string keyValue;
                            if (key?.Length > 0)
                            {
                                keyValue = Encoding.UTF8.GetString(key);
                            }
                            else
                            {
                                keyValue = string.Empty;
                            }

                            item.RoutingKeys.Add(keyValue);
                        }

                        list.Add(item);
                    }
                }

                return list;
            }
        }

        #endregion

        #region fields

        ILogger _logger;
        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = LoggerFactory.CreateLogger<RabbitMQEventBus1>();
                }
                return _logger;
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

        RabbitMQEventBus1Options Setting
        {
            get
            {
                return EventBusOptions.Value;
            }
        }

        #endregion

        ConnectionFactory CreateMQConnectionFactory()
        {
            return new ConnectionFactory()
            {
                UserName = Setting.UserName,
                Password = Setting.Password,
                Port = Setting.Port,
                HostName = Setting.Host,
                VirtualHost = Setting.VirtualHost,
                AutomaticRecoveryEnabled = true,
            };
        }

        void HandlerEvent(Event @event, string handlerTypeString)
        {
            var eventType = @event.GetType();

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug("HandlerEvent, Begin handle event,eventType={0}", eventType);
            }

            List<object> handlerList;
            _eventHandlers.TryGetValue(eventType, out handlerList);

            var handler = handlerList?.Where(m => m.GetType().ToString() == handlerTypeString).FirstOrDefault();

            if (handler != null)
            {
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug("HandlerEvent,Begin handler,eventType={0} handler={1}", eventType, handler.GetType());
                }

                var task = (Task)typeof(IEventHandler<>)
                        .MakeGenericType(eventType)
                        .GetMethod("HandleAsync")
                        .Invoke(handler, new object[] { @event });
                //try
                //{
                task.Wait();
                //}
                //catch (Exception ex)
                //{
                //    if (Logger.IsEnabled(LogLevel.Error))
                //    {
                //        Logger.LogError(ex, string.Format("Invoke handler exception,eventType={0} handler={1}", eventType, handler.GetType()));
                //    }
                //}

                //if (_log.IsDebugEnabled)
                //{
                //    _log.DebugFormat("Invoke handler complete,eventType={0} handler={1}", eventType, item.GetType());
                //}
            }
            else
            {
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug("HandlerEvent,No handler for event, eventType={0} handlerTypeString={1}", eventType, handlerTypeString);
                }
            }
        }

        void EventHandler(object obj)
        {
            var queueName = (string)obj;

            Logger.LogInformation($"{nameof(EventHandler)} start,queueName:{queueName}");

            while (true)
            {
                try
                {
                    using (var conn = CreateMQConnectionFactory().CreateConnection())
                    {
                        using (var channel = conn.CreateModel())
                        {
                            while (true)
                            {
                                var message = channel.BasicGet(queueName, false);
                                if (message == null)
                                {
                                    Thread.CurrentThread.Join(Setting.EmptyWaitMilliSeconds);
                                    continue;
                                }

                                Logger.LogDebug($"EventHandler,receive a message,DeliveryTag:{message.DeliveryTag}");

                                EventDataModel eventData;
                                using (var memoryStream = new MemoryStream(message.Body))
                                {
                                    eventData = (EventDataModel)new BinaryFormatter().Deserialize(memoryStream);
                                }

                                if (Logger.IsEnabled(LogLevel.Debug))
                                {
                                    Logger.LogDebug($"EventHandler,receive event message is:{Newtonsoft.Json.JsonConvert.SerializeObject(eventData)}");
                                }

                                bool handlerEventSuccess;
                                try
                                {
                                    HandlerEvent(eventData.Event, eventData.HandlerTypeString);
                                    handlerEventSuccess = true;
                                }
                                catch (Exception ex)
                                {
                                    handlerEventSuccess = false;
                                    Logger.LogError(ex, $"EventHandler,HandlerEvent error");
                                }

                                if (handlerEventSuccess)
                                {
                                    if (Logger.IsEnabled(LogLevel.Debug))
                                    {
                                        Logger.LogDebug($"EventHandler,handler event message finish");
                                    }

                                    channel.BasicAck(message.DeliveryTag, false);

                                    if (Logger.IsEnabled(LogLevel.Debug))
                                    {
                                        Logger.LogDebug($"EventHandler,BasicAck finish,DeliveryTag:{message.DeliveryTag}");
                                    }
                                }
                                else
                                {
                                    channel.BasicNack(message.DeliveryTag, false, false);

                                    if (Logger.IsEnabled(LogLevel.Debug))
                                    {
                                        Logger.LogDebug($"EventHandler,BasicNack finish,DeliveryTag:{message.DeliveryTag}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"EventHandler error");
                    Thread.CurrentThread.Join(Setting.ErrorWaitMilliSeconds);
                }
            }
        }

        void CreateEventHandler(string queueName)
        {
            var thread = new Thread(new ParameterizedThreadStart(EventHandler));
            thread.IsBackground = true;
            thread.Start(queueName);
        }

        #endregion

        #region property

        public ILoggerFactory LoggerFactory { get; set; }

        public IOptions<RabbitMQEventBus1Options> EventBusOptions { get; set; }

        #endregion

        public void Init()
        {
            Logger.LogInformation($"Init,Setting:{Newtonsoft.Json.JsonConvert.SerializeObject(Setting)}");

            if (Setting.MQEnabled)
            {
                if (string.IsNullOrEmpty(EventBusOptions.Value.Host))
                {
                    throw new ArgumentNullException(nameof(EventBusOptions.Value.Host));
                }
                if (EventBusOptions.Value.Port <= 0)
                {
                    throw new ArgumentException("值必须大于0", nameof(EventBusOptions.Value.Port));
                }
                if (string.IsNullOrEmpty(EventBusOptions.Value.UserName))
                {
                    throw new ArgumentNullException(nameof(EventBusOptions.Value.UserName));
                }
                if (string.IsNullOrEmpty(EventBusOptions.Value.Password))
                {
                    throw new ArgumentNullException(nameof(EventBusOptions.Value.Password));
                }
                if (string.IsNullOrEmpty(EventBusOptions.Value.VirtualHost))
                {
                    throw new ArgumentNullException(nameof(EventBusOptions.Value.VirtualHost));
                }
                if (EventBusOptions.Value.EmptyWaitMilliSeconds < 1)
                {
                    throw new ArgumentException($"值必须大于0", nameof(EventBusOptions.Value.EmptyWaitMilliSeconds));
                }
                if (EventBusOptions.Value.ErrorWaitMilliSeconds < 1)
                {
                    throw new ArgumentException($"值必须大于0", nameof(EventBusOptions.Value.ErrorWaitMilliSeconds));
                }

                if (!(Setting.ProductQueueSettings?.Count > 0))
                {
                    throw new ArgumentNullException(nameof(Setting.ProductQueueSettings));
                }

                if (!(Setting.ConsumerQueueSettings?.Count > 0))
                {
                    throw new ArgumentNullException(nameof(Setting.ConsumerQueueSettings));
                }

                foreach (var item in Setting.ProductQueueSettings)
                {
                    if (string.IsNullOrEmpty(item.QueueName))
                    {
                        throw new ArgumentNullException(nameof(item.QueueName), $"ProductQueueSettings queueName 不能为空");
                    }
                }

                foreach (var item in Setting.ConsumerQueueSettings)
                {
                    if (string.IsNullOrEmpty(item.QueueName))
                    {
                        throw new ArgumentNullException(nameof(item.QueueName), $"ConsumerQueueSettings queueName 不能为空");
                    }
                    if (item.HandlerAmount < 1)
                    {
                        throw new ArgumentNullException(nameof(item.HandlerAmount), $"ConsumerQueueSettings QueueHandlerAmount 值必须大于0");
                    }
                }

                try
                {
                    foreach (var consumerQueueSetting in Setting.ConsumerQueueSettings)
                    {
                        for (var i = 0; i < consumerQueueSetting.HandlerAmount; i++)
                        {
                            CreateEventHandler(consumerQueueSetting.QueueName);
                            Logger.LogInformation($"CreateEventHandler,QueueName:{consumerQueueSetting.QueueName} i:{i}");
                        }
                    }

                    Logger.LogInformation("Init MQ,complete");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Init MQ,error");
                }
            }
        }

        #region IEventPublisher

        public async Task PublishAsync<T>(T @event) where T : Event
        {
            var eventType = @event.GetType();

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug($"PublishAsync,eventType:{eventType} eventData:{Newtonsoft.Json.JsonConvert.SerializeObject(@event)}");
            }

            if (Setting.MQEnabled)
            {
                List<object> handlerList;
                if (_eventHandlers.TryGetValue(eventType, out handlerList) && handlerList?.Count > 0)
                {
                    var eventTypeString = eventType.ToString();

                    string queueName;

                    queueName = Setting.ProductQueueSettings
                        .Where(m => m.ForAllEvent == false && m.EventTypeStrings.Contains(eventTypeString)).FirstOrDefault()?.QueueName;

                    if (string.IsNullOrEmpty(queueName))
                    {
                        queueName = Setting.ProductQueueSettings.Where(m => m.ForAllEvent == true).FirstOrDefault()?.QueueName;
                    }

                    if (string.IsNullOrEmpty(queueName))
                    {
                        throw new InvalidOperationException($"no queue for event({eventTypeString})");
                    }

                    var messageBodys = new List<byte[]>();
                    foreach (var handler in handlerList)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            var eventData = new EventDataModel
                            {
                                HandlerTypeString = handler.GetType().ToString(),
                                Event = @event,
                            };

                            new BinaryFormatter().Serialize(memoryStream, eventData);

                            messageBodys.Add(memoryStream.ToArray());

                            if (Logger.IsEnabled(LogLevel.Debug))
                            {
                                Logger.LogDebug($"PublishAsync,MQ,prepare a event message,{Newtonsoft.Json.JsonConvert.SerializeObject(eventData)}");
                            }
                        }
                    }

                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug($"PublishAsync(MQ),queueName is {queueName} eventTypeString:{eventTypeString}");
                    }

                    using (var conn = CreateMQConnectionFactory().CreateConnection())
                    {
                        using (var channel = conn.CreateModel())
                        {
                            foreach (var messageBody in messageBodys)
                            {
                                var properties = channel.CreateBasicProperties();
                                properties.Persistent = true;
                                properties.DeliveryMode = 2;
                                properties.Timestamp = new AmqpTimestamp(DateTimeUtils.ToUnixTime2(DateTime.Now));

                                channel.BasicPublish(string.Empty, queueName, properties, messageBody);

                                if (Logger.IsEnabled(LogLevel.Debug))
                                {
                                    Logger.LogDebug($"PublishAsync,MQ BasicPublish complete");
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug($"PublishAsync,not handler,eventType:{eventType}");
                    }
                }
            }
            else
            {
                List<object> handlerList;
                if (_eventHandlers.TryGetValue(eventType, out handlerList) && handlerList?.Count > 0)
                {
                    foreach (var handler in handlerList)
                    {
                        if (Logger.IsEnabled(LogLevel.Debug))
                        {
                            Logger.LogDebug($"PublishAsync(no mq),handler doing,eventType:{eventType} handlerType:{handler.GetType()}");
                        }

                        try
                        {
                            await ((IEventHandler<T>)handler).HandleAsync(@event);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"PublishAsync(no mq),handler event error,msg:{ex.Message} eventType:{eventType}");
                        }
                    }
                }
                else
                {
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug($"PublishAsync(no mq),not handler,eventType:{eventType}");
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

            Logger.LogDebug("Subscribe event hanlder,eventType={0} handler={1}", eventType, handler.GetType());

            return Task.CompletedTask;
        }

        #endregion

    }
}
