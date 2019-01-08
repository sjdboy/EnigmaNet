using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using Newtonsoft.Json;

using EnigmaNet.Bus;
using EnigmaNet.Utils;

namespace EnigmaNet.RabbitMQBus
{
    public sealed class RabbitMQEventBus : IEventPublisher, IEventSubscriber, IDisposable
    {
        #region private

        #region fields

        const string ExchangeTypeFanout = "fanout";
        const int RabbitMQCreateMaxTryTimes = 10;
        const int ErrorWaitTime = 1000 * 2;
        const int EmptyWaitMilliSeconds = 1000 * 2;
        const int FailTTL = 1000 * 10;

        ConcurrentDictionary<Type, ConcurrentBag<Type>> _handlerToEvents = new ConcurrentDictionary<Type, ConcurrentBag<Type>>();
        ConcurrentDictionary<Type, object> _handlers = new ConcurrentDictionary<Type, object>();
        ConcurrentDictionary<Type, bool> _buildedQueues = new ConcurrentDictionary<Type, bool>();
        ConcurrentDictionary<Type, bool> _buildedExchanges = new ConcurrentDictionary<Type, bool>();
        IConnection _connection;
        object _connectionLocker = new object();

        string PublishEventNamePrefix { get { return Options.Value.PublishEventNamePrefix; } }
        string InstanceId { get { return Options.Value.InstanceId; } }

        #endregion

        #region methods

        ConnectionFactory CreateMQConnectionFactory()
        {
            return new ConnectionFactory()
            {
                UserName = Options.Value.UserName,
                Password = Options.Value.Password,
                Port = Options.Value.Port,
                HostName = Options.Value.Host,
                VirtualHost = Options.Value.VirtualHost,
                AutomaticRecoveryEnabled = true,
            };
        }

        IConnection GetConnection()
        {
            if (_connection != null)
            {
                return _connection;
            }

            lock (_connectionLocker)
            {
                if (_connection == null)
                {
                    _connection = CreateMQConnectionFactory().CreateConnection();
                }
            }

            return _connection;
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

        ILogger GetInitLogger()
        {
            return LoggerFactory.CreateLogger("EnigmaNet.RabbitMQBus.RabbitMQEventBus_Init");
        }

        ILogger GetLogger()
        {
            return LoggerFactory.CreateLogger<RabbitMQEventBus>();
        }

        string GetExchangeName(Type eventType)
        {
            if (string.IsNullOrEmpty(InstanceId))
            {
                throw new InvalidOperationException("InstanceId is empty");
            }

            var eventTypeName = eventType.FullName;

            if (!string.IsNullOrEmpty(PublishEventNamePrefix) && eventTypeName.StartsWith(PublishEventNamePrefix))
            {
                return eventTypeName;
            }
            else
            {
                return $"{InstanceId}_{eventTypeName}";
            }
        }

        string GetQueueName(Type handlerType)
        {
            if (string.IsNullOrEmpty(InstanceId))
            {
                throw new InvalidOperationException("InstanceId is empty");
            }

            return $"{InstanceId}_{handlerType.FullName}";
        }

        string GetFailQueueName(Type handlerType)
        {
            if (string.IsNullOrEmpty(InstanceId))
            {
                throw new InvalidOperationException("InstanceId is empty");
            }

            return $"{InstanceId}_{handlerType.FullName}_fail";
        }

        void CreateExchangeIfNot(Type eventType)
        {
            if (_buildedExchanges.ContainsKey(eventType))
            {
                return;
            }

            var exchangeName = GetExchangeName(eventType);

            using (var channel = GetConnection().CreateModel())
            {
                channel.ExchangeDeclare(exchangeName, ExchangeTypeFanout, true, false);

                //create ok
                _buildedExchanges.TryAdd(eventType, true);
            }
        }

        void HandleMessage(IModel channel, object handler)
        {
            var handlerType = handler.GetType();
            var queueName = GetQueueName(handlerType);
            var logger = GetLogger();
            _handlerToEvents.TryGetValue(handlerType, out ConcurrentBag<Type> supportEvents);

            while (true)
            {
                var message = channel.BasicGet(queueName, false);
                if (message == null)
                {
                    Thread.CurrentThread.Join(EmptyWaitMilliSeconds);
                    continue;
                }

                logger.LogDebug($"receive a message");

                bool success;
                try
                {
                    var messageStr = Encoding.UTF8.GetString(message.Body);

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug($"message content:{messageStr}");
                    }

                    var @event = (Event)JsonConvert.DeserializeObject(messageStr, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                    var eventType = @event.GetType();

                    if (!supportEvents.Contains(eventType))
                    {
                        //不支持该事件的处理
                        logger.LogInformation($"not support that event,handlerType:{handlerType} eventType:{eventType}");
                    }
                    else
                    {
                        logger.LogInformation($"start to handle event,handlerType:{handlerType} eventType:{eventType}");

                        var task = (Task)typeof(IEventHandler<>)
                            .MakeGenericType(eventType)
                            .GetMethod("HandleAsync")
                            .Invoke(handler, new object[] { @event });

                        task.Wait();

                        logger.LogInformation($"handle event complete,handlerType:{handlerType} eventType:{eventType}");
                    }

                    success = true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"handle message error");
                    success = false;
                }

                if (success)
                {
                    channel.BasicAck(message.DeliveryTag, false);
                    logger.LogDebug($"message ack");
                }
                else
                {
                    channel.BasicNack(message.DeliveryTag, false, false);
                    logger.LogDebug("message nack");
                }
            }
        }

        void StartHandler(object handler)
        {
            var handlerType = handler.GetType();
            var queueName = GetQueueName(handlerType);

            //create rabbit object
            {
                var initLogger = GetInitLogger();

                initLogger.LogInformation($"StartHandler,handlerType:{handlerType}");

                _handlerToEvents.TryGetValue(handlerType, out ConcurrentBag<Type> eventTypes);

                var tryTimes = 0;
                while (true)
                {
                    try
                    {
                        using (var channel = GetConnection().CreateModel())
                        {
                            //create queue(for handler)
                            if (!_buildedQueues.ContainsKey(handlerType))
                            {
                                var failQueueName = GetFailQueueName(handlerType);

                                var queueParameters = new Dictionary<string, object>();
                                queueParameters.Add(Utils.QueueArguments.DeadLetterExchange, string.Empty);
                                queueParameters.Add(Utils.QueueArguments.DeadLetterRoutingKey, failQueueName);

                                var failQueueParameters = new Dictionary<string, object>();
                                failQueueParameters.Add(Utils.QueueArguments.DeadLetterExchange, string.Empty);
                                failQueueParameters.Add(Utils.QueueArguments.DeadLetterRoutingKey, queueName);
                                failQueueParameters.Add(Utils.QueueArguments.MessageTTL, FailTTL);

                                channel.QueueDeclare(queueName, true, false, false, queueParameters);
                                initLogger.LogDebug($"create queue finish,queueName:{queueName}");

                                channel.QueueDeclare(failQueueName, true, false, false, failQueueParameters);
                                initLogger.LogDebug($"create fail queue finish,failQueueName:{failQueueName}");

                                _buildedQueues.TryAdd(handlerType, true);
                            }

                            foreach (var eventType in eventTypes)
                            {
                                //create exchange(for event)
                                var exchangeName = GetExchangeName(eventType);
                                if (!_buildedExchanges.ContainsKey(eventType))
                                {
                                    channel.ExchangeDeclare(exchangeName, ExchangeTypeFanout, true, false);
                                    initLogger.LogDebug($"create exchange finish,exchangeName:{exchangeName}");

                                    _buildedExchanges.TryAdd(eventType, true);
                                }

                                //subscribe
                                channel.QueueBind(queueName, exchangeName, string.Empty);
                                initLogger.LogDebug($"bind queue finish,queueName:{queueName} exchangeName:{exchangeName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        tryTimes++;
                        initLogger.LogError(ex, $"StartHandler,create rabbitmq object error");

                        if (tryTimes > RabbitMQCreateMaxTryTimes)
                        {
                            initLogger.LogInformation($"try create rabbitmq more time,and out");
                            break;
                        }
                        else
                        {
                            Thread.CurrentThread.Join(ErrorWaitTime);
                            continue;
                        }
                    }

                    break;
                }
            }

            //handle message
            var logger = GetLogger();
            while (true)
            {
                try
                {
                    using (var channel = GetConnection().CreateModel())
                    {
                        HandleMessage(channel, handler);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"handle error");
                    Thread.CurrentThread.Join(ErrorWaitTime);
                }
            }
        }

        #endregion

        #endregion

        #region publish

        public IOptions<RabbitMQEventBusOptions> Options { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public Task PublishAsync<T>(T @event) where T : Event
        {
            var logger = GetLogger();

            var eventType = @event.GetType();

            CreateExchangeIfNot(eventType);

            var exchangeName = GetExchangeName(eventType);

            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, }));

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug($"PublishAsync,prev send, exchangeName:{exchangeName}");
            }

            using (var channel = GetConnection().CreateModel())
            {
                var properties = CreateProperties(channel);

                channel.BasicPublish(exchangeName, string.Empty, properties, messageBody);
            }

            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T>(IEventHandler<T> handler) where T : Event
        {
            var logger = GetInitLogger();

            var eventType = typeof(T);
            var handlerType = handler.GetType();

            logger.LogDebug($"SubscribeAsync,eventType:{eventType} handlerType:{handlerType}");

            if (!_handlerToEvents.ContainsKey(handlerType))
            {
                _handlerToEvents.TryAdd(handlerType, new ConcurrentBag<Type>());
            }
            _handlerToEvents.TryGetValue(handlerType, out ConcurrentBag<Type> handlersEvents);
            handlersEvents.Add(eventType);

            if (!_handlers.ContainsKey(handlerType))
            {
                _handlers.TryAdd(handlerType, handler);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
            }
        }

        public void Init()
        {
            foreach (var item in _handlers)
            {
                var handler = item.Value;
                var thread = new Thread(new ParameterizedThreadStart(StartHandler));
                thread.IsBackground = true;
                thread.Start(handler);
            }
        }

        #endregion
    }
}
