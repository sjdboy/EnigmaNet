using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.IO;
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
        const int ErrorWaitTime = 1000 * 2;
        const int EmptyWaitMilliSeconds = 1000 * 2;
        const int FailTTL = 1000 * 10;

        ILogger _logger;
        RabbitMQEventBusOptions _optionValue;

        ConcurrentDictionary<Type, ConcurrentBag<Type>> _handlerToEvents = new ConcurrentDictionary<Type, ConcurrentBag<Type>>();
        ConcurrentDictionary<Type, object> _handlers = new ConcurrentDictionary<Type, object>();
        ConcurrentDictionary<Type, bool> _buildedQueues = new ConcurrentDictionary<Type, bool>();
        ConcurrentDictionary<Type, bool> _buildedExchanges = new ConcurrentDictionary<Type, bool>();
        IConnection _connection;
        object _connectionLocker = new object();

        #endregion

        #region methods

        ConnectionFactory CreateMQConnectionFactory()
        {
            return new ConnectionFactory()
            {
                UserName = _optionValue.UserName,
                Password = _optionValue.Password,
                Port = _optionValue.Port,
                HostName = _optionValue.Host,
                VirtualHost = _optionValue.VirtualHost,
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

        string GetExchangeName(Type eventType)
        {
            return eventType.FullName;
        }

        string GetQueueName(Type handlerType)
        {
            return handlerType.FullName;
        }

        string GetFailQueueName(Type handlerType)
        {
            return $"{handlerType.FullName}_fail";
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
            var logger = _logger;
            _handlerToEvents.TryGetValue(handlerType, out ConcurrentBag<Type> supportEvents);

            while (true)
            {
                if (_stop)
                {
                    return;
                }

                var message = channel.BasicGet(queueName, false);
                if (message == null)
                {
                    Thread.CurrentThread.Join(EmptyWaitMilliSeconds);
                    continue;
                }

                var messageStr = Encoding.UTF8.GetString(message.Body.ToArray());
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug($"receive a message,queueName:{queueName} content:{messageStr}");
                }

                bool success;
                try
                {
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

            //handler init
            {
                var queueName = GetQueueName(handlerType);

                _logger.LogInformation($"handler init, handlerType:{handlerType}");

                _handlerToEvents.TryGetValue(handlerType, out ConcurrentBag<Type> eventTypes);

                while (true)
                {
                    try
                    {
                        _logger.LogInformation($"handler init,beging create rabbitmq object, handlerType:{handlerType}");

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
                                _logger.LogDebug($"handler init,create queue finish,queueName:{queueName}, handlerType:{handlerType}");

                                channel.QueueDeclare(failQueueName, true, false, false, failQueueParameters);
                                _logger.LogDebug($"handler init,create fail queue finish,failQueueName:{failQueueName}, handlerType:{handlerType}");

                                _buildedQueues.TryAdd(handlerType, true);
                            }

                            foreach (var eventType in eventTypes)
                            {
                                //create exchange(for event)
                                var exchangeName = GetExchangeName(eventType);
                                if (!_buildedExchanges.ContainsKey(eventType))
                                {
                                    channel.ExchangeDeclare(exchangeName, ExchangeTypeFanout, true, false);
                                    _logger.LogDebug($"handler init,create exchange finish,exchangeName:{exchangeName}");

                                    _buildedExchanges.TryAdd(eventType, true);
                                }

                                //subscribe
                                channel.QueueBind(queueName, exchangeName, string.Empty);
                                _logger.LogDebug($"handler init,bind queue finish,queueName:{queueName} exchangeName:{exchangeName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"handler init,create rabbitmq object error, handlerType:{handlerType}");
                        Thread.CurrentThread.Join(ErrorWaitTime);

                        continue;
                    }

                    _logger.LogInformation($"handler init,create rabbitmq object completed, handlerType:{handlerType}");
                    break;
                }
            }

            //handle message
            while (true)
            {
                if (_stop)
                {
                    return;
                }

                try
                {
                    using (var channel = GetConnection().CreateModel())
                    {
                        HandleMessage(channel, handler);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"handle error, handlerType:{handlerType}");
                    Thread.CurrentThread.Join(ErrorWaitTime);
                }
            }
        }

        #endregion

        #endregion

        #region publish

        public RabbitMQEventBus(ILogger<RabbitMQEventBus> logger,
            IOptions<RabbitMQEventBusOptions> options
            )
        {
            _logger = logger;
            _optionValue = options.Value;
        }

        public Task PublishAsync<T>(T @event) where T : Event
        {
            var logger = _logger;

            var eventType = @event.GetType();

            var exchangeName = GetExchangeName(eventType);

            var messageString = JsonConvert.SerializeObject(@event, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, });

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug($"Publish event,prev send, exchangeName:{exchangeName} messageString:{messageString}");
            }

            CreateExchangeIfNot(eventType);

            using (var channel = GetConnection().CreateModel())
            {
                var properties = CreateProperties(channel);

                channel.BasicPublish(exchangeName, string.Empty, properties, Encoding.UTF8.GetBytes(messageString));
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug($"PublishAsync,send completed");
            }

            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T>(IEventHandler<T> handler) where T : Event
        {
            var eventType = typeof(T);
            var handlerType = handler.GetType();

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Subscribe,eventType:{eventType} handlerType:{handlerType}");
            }

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
            _logger.LogInformation("Init");

            //start bus handler
            foreach (var item in _handlers)
            {
                var handler = item.Value;
                var thread = new Thread(new ParameterizedThreadStart(StartHandler));
                thread.IsBackground = true;
                thread.Start(handler);

                _logger.LogInformation($"Init,StartHandler,handler:{item.GetType()}");
            }
        }

        bool _stop;

        public void StopEventHandlers()
        {
            _logger.LogInformation("StopEventHandlers,set stop flag to true.");

            _stop = true;
        }

        #endregion
    }
}
