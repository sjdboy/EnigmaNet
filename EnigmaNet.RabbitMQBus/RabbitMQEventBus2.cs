using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using Newtonsoft.Json;

using EnigmaNetCore.Bus;
using EnigmaNetCore.Utils;
using System.Collections.Concurrent;

namespace EnigmaNetCore.RabbitMQBus
{
    public sealed class RabbitMQEventBus2 : IEventPublisher, IEventSubscriber, IDisposable
    {
        #region private

        #region fields

        Dictionary<string, object> _handlers = new Dictionary<string, object>();
        Dictionary<string, List<string>> _handlersEventCodes = new Dictionary<string, List<string>>();
        List<string> _subscribedPublicBusEventCodes = new List<string>();

        bool _startInit;

        IConnection _publicBusConnection;
        bool _publicBusInited;
        object _publicBusLocker = new object();

        IConnection _privateBusConnection;
        bool _privateBusInited;
        object _privateBusLocker = new object();

        ILogger _logger;
        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = LoggerFactory.CreateLogger<RabbitMQEventBus2>();
                }
                return _logger;
            }
        }

        const int RetryTTLValue = 1000 * 3;
        const int FailTTLValue = 1000 * 60 * 60 * 24;

        const int MaxTryTimes = 5;

        #endregion

        #region methods

        #region rabbit

        ConnectionFactory CreateMQConnectionFactory(RabbitMQEventBus2Options.HostConfigModel config)
        {
            return new ConnectionFactory()
            {
                UserName = config.UserName,
                Password = config.Password,
                Port = config.Port,
                HostName = config.Host,
                VirtualHost = config.VirtualHost,
                AutomaticRecoveryEnabled = true,
            };
        }

        IBasicProperties CreateProperties(IModel channel)
        {
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.DeliveryMode = 2;
            properties.Timestamp = new AmqpTimestamp(DateTimeUtils.ToUnixTime2(DateTime.Now));
            properties.ContentType = "application/json";
            properties.ContentEncoding = "utf-8";

            return properties;
        }

        #endregion

        #region message serialize

        byte[] SerializeEvent(Event @event)
        {
            var jsonString = JsonConvert.SerializeObject(
               @event,
               new JsonSerializerSettings
               {
                   TypeNameHandling = TypeNameHandling.All
               });

            return Encoding.UTF8.GetBytes(jsonString);
        }

        Event DeserializeEvent(byte[] messageBody)
        {
            var jsonString = Encoding.UTF8.GetString(messageBody);

            return (Event)JsonConvert.DeserializeObject(
                jsonString,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                });
        }

        #endregion

        #region queue names

        string GetRetryQueueName(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }
            return $"{queueName}_retry";
        }

        string GetFailQueueName(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }
            return $"{queueName}_fail";
        }

        #endregion

        #region message handler

        void SendMessageToBus(IModel channel, string exchangeName, Event @event)
        {
            var eventCode = @event.GetType().ToString();

            var messageBody = SerializeEvent(@event);

            var properties = CreateProperties(channel);

            channel.BasicPublish(exchangeName, eventCode, properties, messageBody);
        }

        void SendMessageToBusQueue(IModel channel, string queueName, byte[] messageBody)
        {
            var properties = CreateProperties(channel);

            channel.BasicPublish(string.Empty, queueName, properties, messageBody);
        }

        void DoPublicBusMessageHandlerTask(
            IModel publicBusChannel, string publicBusQueueName,
            IModel privateBusChannel, string privateBusExchangeName)
        {
            var publicBusFailQueueName = GetFailQueueName(publicBusQueueName);

            while (true)
            {
                var message = publicBusChannel.BasicGet(publicBusQueueName, false);
                if (message == null)
                {
                    Thread.CurrentThread.Join(Options.Value.EmptyWaitMilliSeconds);
                    continue;
                }

                //验证处理次数
                var tryCount = Utils.MessageUtils.GetDeathCount(message, publicBusQueueName);
                if (tryCount.HasValue && tryCount.Value > MaxTryTimes)
                {
                    Logger.LogInformation($"{nameof(DoPublicBusMessageHandlerTask)}, tryCount overflow,start remove and save to fail queue'{publicBusFailQueueName}'");

                    //转存到失败队列
                    SendMessageToBusQueue(publicBusChannel, publicBusFailQueueName, message.Body);

                    Logger.LogInformation($"{nameof(DoPublicBusMessageHandlerTask)}, save to fail queue complete");

                    //标记为已消费
                    publicBusChannel.BasicAck(message.DeliveryTag, false);

                    Logger.LogInformation($"{nameof(DoPublicBusMessageHandlerTask)}, remove complete");

                    continue;
                }

                bool success;
                try
                {
                    var @event = DeserializeEvent(message.Body);

                    SendMessageToBus(privateBusChannel, privateBusExchangeName, @event);

                    success = true;
                }
                catch (Exception ex1)
                {
                    Logger.LogError($"handler public bus message error", ex1);
                    success = false;
                }

                if (success)
                {
                    publicBusChannel.BasicAck(message.DeliveryTag, false);
                }
                else
                {
                    publicBusChannel.BasicNack(message.DeliveryTag, false, false);
                }
            }
        }

        void PublicBusMessageHandlerTask()
        {
            var publicBusQueueName = Options.Value.PublicBusQueueName;
            var privateBusExchange = Options.Value.PrivateBusExchangeName;

            while (true)
            {
                try
                {
                    InitPublicBusIfNot();
                    InitPrivateBusIfNot();

                    using (var publicBusChannel = _publicBusConnection.CreateModel())
                    {
                        using (var privateBusChannel = _privateBusConnection.CreateModel())
                        {
                            DoPublicBusMessageHandlerTask(
                                publicBusChannel, publicBusQueueName,
                                privateBusChannel, privateBusExchange);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"{nameof(PublicBusMessageHandlerTask)} error", ex);
                    Thread.CurrentThread.Join(Options.Value.ErrorWaitMilliSeconds);
                }
            }
        }

        void DoPrivateBusMessageHandlerTask(IModel channel, string queueName, object handler)
        {
            var failQueueName = GetFailQueueName(queueName);
            while (true)
            {
                var message = channel.BasicGet(queueName, false);
                if (message == null)
                {
                    Thread.CurrentThread.Join(Options.Value.EmptyWaitMilliSeconds);
                    continue;
                }

                //验证处理次数
                var tryCount = Utils.MessageUtils.GetDeathCount(message, queueName);
                if (tryCount.HasValue && tryCount.Value > MaxTryTimes)
                {
                    Logger.LogInformation($"{nameof(DoPrivateBusMessageHandlerTask)}, tryCount overflow,start remove and save to fail queue'{failQueueName}'");

                    //转存到失败队列
                    SendMessageToBusQueue(channel, failQueueName, message.Body);

                    Logger.LogInformation($"{nameof(DoPrivateBusMessageHandlerTask)}, save to fail queue complete");

                    //标记为已消费
                    channel.BasicAck(message.DeliveryTag, false);

                    Logger.LogInformation($"{nameof(DoPrivateBusMessageHandlerTask)}, remove complete");

                    continue;
                }

                bool success;
                try
                {
                    var @event = DeserializeEvent(message.Body);

                    var task = (Task)typeof(IEventHandler<>)
                        .MakeGenericType(handler.GetType())
                        .GetMethod("HandleAsync")
                        .Invoke(handler, new object[] { @event });

                    task.Wait();

                    success = true;
                }
                catch (Exception ex1)
                {
                    Logger.LogError($"handler private bus message error", ex1);
                    success = false;
                }

                if (success)
                {
                    channel.BasicAck(message.DeliveryTag, false);
                }
                else
                {
                    channel.BasicNack(message.DeliveryTag, false, false);
                }
            }
        }

        void PrivateBusMessageHandlerTask(object handlerCodeObject)
        {
            var handlerCode = (string)handlerCodeObject;
            var handler = _handlers[handlerCode];
            var queueName = handlerCode;

            while (true)
            {
                try
                {
                    InitPrivateBusIfNot();

                    using (var channel = _privateBusConnection.CreateModel())
                    {
                        DoPrivateBusMessageHandlerTask(channel, queueName, handler);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"{nameof(PrivateBusMessageHandlerTask)} error", ex);
                    Thread.CurrentThread.Join(Options.Value.ErrorWaitMilliSeconds);
                }
            }
        }

        #endregion

        #region init bus

        void InitPublicBusIfNot()
        {
            if (_publicBusInited)
            {
                return;
            }

            lock (_publicBusLocker)
            {
                if (_publicBusInited)
                {
                    return;
                }

                var queueName = Options.Value.PublicBusQueueName;
                var retryQueueName = GetRetryQueueName(queueName);
                var failQueueName = GetFailQueueName(queueName);
                var exchangeName = Options.Value.PublicBusExchangeName;

                var connection = CreateMQConnectionFactory(Options.Value.PublicHost).CreateConnection();

                using (var channel = _publicBusConnection.CreateModel())
                {
                    //declare:queue,retryQueue,failQueue
                    {
                        var parameters = new Dictionary<string, object>();
                        parameters.Add(Utils.QueueArguments.DeadLetterExchange, string.Empty);
                        parameters.Add(Utils.QueueArguments.DeadLetterRoutingKey, retryQueueName);
                        channel.QueueDeclare(queueName, true, false, false, parameters);
                    }

                    {
                        var parameters = new Dictionary<string, object>();
                        parameters.Add(Utils.QueueArguments.MessageTTL, RetryTTLValue);
                        parameters.Add(Utils.QueueArguments.DeadLetterExchange, string.Empty);
                        parameters.Add(Utils.QueueArguments.DeadLetterRoutingKey, queueName);
                        channel.QueueDeclare(retryQueueName, true, false, false, parameters);
                    }

                    {
                        var parameters = new Dictionary<string, object>();
                        parameters.Add(Utils.QueueArguments.MessageTTL, FailTTLValue);
                        parameters.Add(Utils.QueueArguments.DeadLetterExchange, string.Empty);
                        parameters.Add(Utils.QueueArguments.DeadLetterRoutingKey, queueName);
                        channel.QueueDeclare(failQueueName, true, false, false, parameters);
                    }

                    //绑定订阅事件的消息
                    foreach (var eventCode in _subscribedPublicBusEventCodes)
                    {
                        channel.QueueBind(queueName, exchangeName, eventCode);
                    }
                }

                _publicBusConnection = connection;
                _publicBusInited = true;
            }
        }

        void InitPrivateBusIfNot()
        {
            if (_privateBusInited)
            {
                return;
            }

            lock (_privateBusLocker)
            {
                if (_privateBusInited)
                {
                    return;
                }

                var connection = CreateMQConnectionFactory(Options.Value.PrivateHost).CreateConnection();
                using (var channel = connection.CreateModel())
                {
                    foreach (var handler in _handlers)
                    {
                        var handlerCode = handler.Key;

                        var queueName = handlerCode;
                        var retryQueueName = GetRetryQueueName(queueName);
                        var failQueueName = GetFailQueueName(queueName);
                        var exchangeName = Options.Value.PrivateBusExchangeName;

                        //declare:queue,retryQueue,failQueue
                        {
                            var parameters = new Dictionary<string, object>();
                            parameters.Add(Utils.QueueArguments.DeadLetterExchange, string.Empty);
                            parameters.Add(Utils.QueueArguments.DeadLetterRoutingKey, retryQueueName);
                            channel.QueueDeclare(queueName, true, false, false, parameters);
                        }

                        {
                            var parameters = new Dictionary<string, object>();
                            parameters.Add(Utils.QueueArguments.MessageTTL, RetryTTLValue);
                            parameters.Add(Utils.QueueArguments.DeadLetterExchange, string.Empty);
                            parameters.Add(Utils.QueueArguments.DeadLetterRoutingKey, queueName);
                            channel.QueueDeclare(retryQueueName, true, false, false, parameters);
                        }

                        {
                            var parameters = new Dictionary<string, object>();
                            parameters.Add(Utils.QueueArguments.MessageTTL, FailTTLValue);
                            parameters.Add(Utils.QueueArguments.DeadLetterExchange, string.Empty);
                            parameters.Add(Utils.QueueArguments.DeadLetterRoutingKey, queueName);
                            channel.QueueDeclare(failQueueName, true, false, false, parameters);
                        }

                        //bind event
                        foreach (var eventCode in _handlersEventCodes[handlerCode])
                        {
                            channel.QueueBind(queueName, exchangeName, eventCode, null);
                        }
                    }
                }

                _privateBusConnection = connection;
                _privateBusInited = true;
            }
        }

        #endregion

        #endregion

        #endregion

        public IOptions<RabbitMQEventBus2Options> Options { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public Task PublishAsync<T>(T @event) where T : Event
        {
            var eventCode = @event.GetType().ToString();

            if (!string.IsNullOrEmpty(Options.Value.PublicEventPrefix) &&
                eventCode.StartsWith(Options.Value.PublicEventPrefix))
            {
                InitPublicBusIfNot();

                var exchange = Options.Value.PublicBusExchangeName;
                using (var channel = _publicBusConnection.CreateModel())
                {
                    SendMessageToBus(channel, exchange, @event);
                }
            }
            else
            {
                InitPrivateBusIfNot();

                var exchange = Options.Value.PrivateBusExchangeName;
                using (var channel = _publicBusConnection.CreateModel())
                {
                    SendMessageToBus(channel, exchange, @event);
                }
            }

            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T>(IEventHandler<T> handler) where T : Event
        {
            if (_startInit)
            {
                throw new InvalidOperationException("start init,no support any subscribe");
            }

            var eventCode = typeof(T).ToString();
            var handlerCode = handler.GetType().ToString();

            if (eventCode.StartsWith(Options.Value.PublicEventPrefix))
            {
                lock (_subscribedPublicBusEventCodes)
                {
                    if (!_subscribedPublicBusEventCodes.Contains(eventCode))
                    {
                        _subscribedPublicBusEventCodes.Add(eventCode);
                    }
                }
            }

            lock (_handlers)
            {
                if (!_handlers.ContainsKey(handlerCode))
                {
                    _handlers.Add(handlerCode, handler);
                }

                //if (!_eventAndHandlerCodes.ContainsKey(eventCode))
                //{
                //    _eventAndHandlerCodes.Add(eventCode, new List<string> { handlerCode });
                //}
                //else
                //{
                //    if (!_eventAndHandlerCodes[eventCode].Contains(handlerCode))
                //    {
                //        _eventAndHandlerCodes[eventCode].Add(handlerCode);
                //    }
                //}

                if (!_handlersEventCodes.ContainsKey(handlerCode))
                {
                    _handlersEventCodes.Add(handlerCode, new List<string> { eventCode });
                }
                else
                {
                    if (!_handlersEventCodes[handlerCode].Contains(eventCode))
                    {
                        _handlersEventCodes[handlerCode].Add(eventCode);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_privateBusConnection != null)
            {
                _privateBusConnection.Dispose();
            }

            if (_publicBusConnection != null)
            {
                _publicBusConnection.Dispose();
            }
        }

        public void Init()
        {
            _startInit = true;

            //创建公有总线消息监听
            {
                var thread = new Thread(new ThreadStart(PublicBusMessageHandlerTask));
                thread.IsBackground = true;
                thread.Start();
            }

            //创建私有总线消息监听
            foreach (var handler in _handlers)
            {
                var handlerCode = handler.Key;
                var thread = new Thread(new ParameterizedThreadStart(PrivateBusMessageHandlerTask));
                thread.IsBackground = true;
                thread.Start(handlerCode);
            }

            //尝试初始总线
            try
            {
                InitPrivateBusIfNot();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{nameof(Init)},InitPrivateBusIfNot error", ex);
            }

            try
            {
                InitPublicBusIfNot();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{nameof(Init)},InitPublicBusIfNot error", ex);
            }
        }
    }
}
