using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using Newtonsoft.Json;

using EnigmaNet.Bus;
using EnigmaNet.Utils;

namespace EnigmaNet.RabbitMQBus
{
    public class RabbitMQDelayMessageBus : IDelayMessagePublisher, IDelayMessageSubscriber, IDisposable
    {
        #region private

        class LoggerSubCategories
        {
            public const string Init = "Init";
            public const string SaveMessageToLocal = "SaveMessageToLocal";
            public const string LocalMessageSendHandler = "LocalMessageSendHandler";
        }

        const int ErrorWaitTime = 1000 * 2;
        const int EmptyWaitMilliSeconds = 1000 * 2;
        const int FailTTL = 1000 * 10;

        ConcurrentDictionary<Type, ConcurrentBag<Type>> _handlerToMessages = new ConcurrentDictionary<Type, ConcurrentBag<Type>>();
        ConcurrentDictionary<Type, object> _handlers = new ConcurrentDictionary<Type, object>();
        ConcurrentDictionary<Type, bool> _handlerQueuesBuilded = new ConcurrentDictionary<Type, bool>();
        ConcurrentDictionary<Type, bool> _messageExchangesBuilded = new ConcurrentDictionary<Type, bool>();
        ConcurrentBag<int> _buildTimeQueues = new ConcurrentBag<int>();
        bool _coreExchangeBuild = false;

        RabbitMQDelayMessageBusOptions _optionValue;
        ILogger _logger;

        string CoreExchangeName
        {
            get
            {

                if (!string.IsNullOrEmpty(_optionValue.CoreExchangeName))
                {
                    return _optionValue.CoreExchangeName;
                }

                return "core_ex";
            }
        }

        IConnection _connection;
        object _connectionLocker = new object();

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

        string GetExchangeNameForMessage(Type messageType)
        {
            return messageType.FullName;
        }

        string GetHandlerQueueName(Type handlerType)
        {
            return handlerType.FullName;
        }

        string GetFailQueueName(Type handlerType)
        {
            return $"{handlerType.FullName}_fail";
        }

        string GetHeaderValueForMessage(Type messageType)
        {
            return messageType.FullName;
        }

        string GetDelayQueueName(int delaySeconds)
        {
            return $"dm_ts_{delaySeconds}";
        }

        void HandleMessage(IModel channel, object handler)
        {
            var handlerType = handler.GetType();
            var queueName = GetHandlerQueueName(handlerType);
            _handlerToMessages.TryGetValue(handlerType, out ConcurrentBag<Type> supportMessages);

            while (true)
            {
                var message = channel.BasicGet(queueName, false);
                if (message == null)
                {
                    Thread.CurrentThread.Join(EmptyWaitMilliSeconds);
                    continue;
                }

                _logger.LogDebug($"receive a message");

                bool success;
                try
                {
                    var messageStr = Encoding.UTF8.GetString(message.Body.ToArray());

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug($"message content:{messageStr}");
                    }

                    var delayMessage = (DelayMessage)JsonConvert.DeserializeObject(messageStr, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                    var messageType = delayMessage.GetType();

                    if (!supportMessages.Contains(messageType))
                    {
                        //不支持该事件的处理
                        _logger.LogInformation($"not support that message,handlerType:{handlerType} messageType:{messageType}");
                    }
                    else
                    {
                        _logger.LogInformation($"start to handle message,handlerType:{handlerType} messageType:{messageType}");

                        var task = (Task)typeof(IDelayMessageHandler<>)
                            .MakeGenericType(messageType)
                            .GetMethod("HandleAsync")
                            .Invoke(handler, new object[] { delayMessage });

                        task.Wait();

                        _logger.LogInformation($"handle message complete,handlerType:{handlerType} messageType:{messageType}");
                    }

                    success = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"handle message error");
                    success = false;
                }

                if (success)
                {
                    channel.BasicAck(message.DeliveryTag, false);
                    _logger.LogDebug($"message ack");
                }
                else
                {
                    channel.BasicNack(message.DeliveryTag, false, false);
                    _logger.LogDebug("message nack");
                }
            }
        }

        void StartHandler(object handler)
        {
            var handlerType = handler.GetType();

            //init
            {
                var queueName = GetHandlerQueueName(handlerType);

                _logger.LogInformation($"handler init, handlerType:{handlerType}");

                _handlerToMessages.TryGetValue(handlerType, out ConcurrentBag<Type> messageTypes);

                //crate queue
                while (true)
                {
                    try
                    {
                        _logger.LogInformation($"handler init,beging create rabbitmq object, handlerType:{handlerType},queueName:{queueName}");

                        using (var channel = GetConnection().CreateModel())
                        {
                            if (!_handlerQueuesBuilded.ContainsKey(handlerType))
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

                                _handlerQueuesBuilded.TryAdd(handlerType, true);
                            }

                            CreateCoreExchangeIfNot(channel);

                            foreach (var messageType in messageTypes)
                            {
                                var messageExchangeName = GetExchangeNameForMessage(messageType);

                                if (!_messageExchangesBuilded.ContainsKey(messageType))
                                {
                                    channel.ExchangeDeclare(messageExchangeName, Utils.ExchangeTypes.Fanout, true, false);

                                    var bindArguments = new Dictionary<string, object>();
                                    bindArguments.Add(Utils.BindArguments.XMatch, Utils.BindArguments.XMathAny);
                                    bindArguments.Add(Utils.BindArguments.MessageType, GetHeaderValueForMessage(messageType));
                                    channel.ExchangeBind(messageExchangeName, CoreExchangeName, string.Empty, bindArguments);

                                    _logger.LogDebug($"handler init,create message exchange finish,exchange:{messageExchangeName},bind exchange:{CoreExchangeName}");

                                    _messageExchangesBuilded.TryAdd(messageType, true);
                                }

                                //subscribe
                                channel.QueueBind(queueName, messageExchangeName, string.Empty);

                                _logger.LogDebug($"handler init,bind queue to message exchange finish,queueName:{queueName} messageExchangeName:{messageExchangeName}");
                            }

                            _logger.LogDebug($"handler init,bind queue finish,queueName:{queueName}");
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

        void CreateCoreExchangeIfNot(IModel channel)
        {
            if (_coreExchangeBuild)
            {
                return;
            }

            var coreExchangeName = CoreExchangeName;

            if (string.IsNullOrEmpty(coreExchangeName))
            {
                throw new ArgumentNullException(nameof(coreExchangeName));
            }

            channel.ExchangeDeclare(coreExchangeName, Utils.ExchangeTypes.Headers, true, false);

            _coreExchangeBuild = true;

            _logger.LogDebug($"handler init,create core exchange finish,exchange:{coreExchangeName}");
        }

        void CreateDelayQueueIfNot(int delaySeconds, IModel channel)
        {
            if (_buildTimeQueues.Contains(delaySeconds))
            {
                return;
            }

            CreateCoreExchangeIfNot(channel);

            var queueName = GetDelayQueueName(delaySeconds);

            var arguments = new Dictionary<string, object>();
            arguments.Add(Utils.QueueArguments.DeadLetterExchange, CoreExchangeName);
            arguments.Add(Utils.QueueArguments.DeadLetterRoutingKey, string.Empty);
            arguments.Add(Utils.QueueArguments.MessageTTL, 1000 * delaySeconds);

            channel.QueueDeclare(queueName, true, false, false, arguments);

            _logger.LogDebug($"CreateDelayQueue,create delay queue finish,queue:{queueName}");
            _buildTimeQueues.Add(delaySeconds);
        }

        IBasicProperties CreateProperties(IModel channel, Type messageType)
        {
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.DeliveryMode = 2;
            properties.Timestamp = new AmqpTimestamp(DateTimeUtils.ToUnixTime2(DateTime.Now));
            properties.ContentType = "application/json";
            properties.ContentEncoding = "utf-8";
            properties.Headers = new Dictionary<string, object>();
            properties.Headers.Add(Utils.BindArguments.MessageType, GetHeaderValueForMessage(messageType));

            return properties;
        }

        #endregion

        public RabbitMQDelayMessageBus(ILogger<RabbitMQDelayMessageBus> logger,
            IOptions<RabbitMQDelayMessageBusOptions> options)
        {

        }

        public Task PublishAsync<T>(T message) where T : DelayMessage
        {
            if (message.DelaySeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(message.DelaySeconds));
            }

            var delaySeconds = message.DelaySeconds;

            var messageType = message.GetType();

            var queueName = GetDelayQueueName(delaySeconds);

            var messageString = JsonConvert.SerializeObject(message, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, });

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Publish message,prev send, queueName:{queueName} messageString:{messageString}");
            }

            using (var channel = GetConnection().CreateModel())
            {
                CreateDelayQueueIfNot(delaySeconds, channel);

                channel.BasicPublish(string.Empty, queueName, CreateProperties(channel, messageType), Encoding.UTF8.GetBytes(messageString));
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"PublishAsync,send completed");
            }

            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T>(IDelayMessageHandler<T> handler) where T : DelayMessage
        {
            var messageType = typeof(T);
            var handlerType = handler.GetType();

            _logger.LogDebug($"Subscribe,messageType:{messageType} handlerType:{handlerType}");

            if (!_handlerToMessages.ContainsKey(handlerType))
            {
                _handlerToMessages.TryAdd(handlerType, new ConcurrentBag<Type>());
            }
            _handlerToMessages.TryGetValue(handlerType, out ConcurrentBag<Type> handlersEvents);
            handlersEvents.Add(messageType);

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
            if (string.IsNullOrEmpty(CoreExchangeName))
            {
                throw new Exception($"CoreExchangeName is empty");
            }

            foreach (var item in _handlers)
            {
                var handler = item.Value;
                var thread = new Thread(new ParameterizedThreadStart(StartHandler));
                thread.IsBackground = true;
                thread.Start(handler);
            }
        }
    }
}
