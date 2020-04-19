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

        #region class

        class LoggerSubCategories
        {
            public const string Init = "Init";
            public const string SaveMessageToLocal = "SaveMessageToLocal";
            public const string LocalMessageSendHandler = "LocalMessageSendHandler";
        }

        #endregion

        #region fields

        const string ExchangeTypeFanout = "fanout";
        const int ErrorWaitTime = 1000 * 2;
        const int EmptyWaitMilliSeconds = 1000 * 2;
        const int FailTTL = 1000 * 10;

        const int LocalMessageOnceHandlerWaitMilliSencods = 1000 * 60;

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
            return GetLogger(LoggerSubCategories.Init);
        }

        ILogger GetLogger()
        {
            return LoggerFactory.CreateLogger<RabbitMQEventBus>();
        }

        ILogger GetLogger(string subCategory)
        {
            return LoggerFactory.CreateLogger($"EnigmaNet.RabbitMQBus.RabbitMQEventBus_{subCategory}");
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
                    var messageStr = Encoding.UTF8.GetString(message.Body.ToArray());

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

            //handler init
            {
                var queueName = GetQueueName(handlerType);

                var logger = GetInitLogger();

                logger.LogInformation($"handler init, handlerType:{handlerType}");

                _handlerToEvents.TryGetValue(handlerType, out ConcurrentBag<Type> eventTypes);

                while (true)
                {
                    try
                    {
                        logger.LogInformation($"handler init,beging create rabbitmq object, handlerType:{handlerType}");

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
                                logger.LogDebug($"handler init,create queue finish,queueName:{queueName}, handlerType:{handlerType}");

                                channel.QueueDeclare(failQueueName, true, false, false, failQueueParameters);
                                logger.LogDebug($"handler init,create fail queue finish,failQueueName:{failQueueName}, handlerType:{handlerType}");

                                _buildedQueues.TryAdd(handlerType, true);
                            }

                            foreach (var eventType in eventTypes)
                            {
                                //create exchange(for event)
                                var exchangeName = GetExchangeName(eventType);
                                if (!_buildedExchanges.ContainsKey(eventType))
                                {
                                    channel.ExchangeDeclare(exchangeName, ExchangeTypeFanout, true, false);
                                    logger.LogDebug($"handler init,create exchange finish,exchangeName:{exchangeName}");

                                    _buildedExchanges.TryAdd(eventType, true);
                                }

                                //subscribe
                                channel.QueueBind(queueName, exchangeName, string.Empty);
                                logger.LogDebug($"handler init,bind queue finish,queueName:{queueName} exchangeName:{exchangeName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"handler init,create rabbitmq object error, handlerType:{handlerType}");
                        Thread.CurrentThread.Join(ErrorWaitTime);

                        continue;
                    }

                    logger.LogInformation($"handler init,create rabbitmq object completed, handlerType:{handlerType}");
                    break;
                }
            }

            //handle message
            while (true)
            {
                var logger = GetLogger();
                try
                {
                    using (var channel = GetConnection().CreateModel())
                    {
                        HandleMessage(channel, handler);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"handle error, handlerType:{handlerType}");
                    Thread.CurrentThread.Join(ErrorWaitTime);
                }
            }
        }

        string GetFailMessageFolderPath()
        {
            string folderPath;
            if (Path.IsPathRooted(Options.Value.FailMessageStoreFolder))
            {
                folderPath = Options.Value.FailMessageStoreFolder;
            }
            else
            {
                folderPath = Path.Combine(System.AppContext.BaseDirectory, Options.Value.FailMessageStoreFolder);
            }

            return folderPath;
        }

        void SaveMessageToLocal(string eventId, string messageString)
        {
            var folderPath = GetFailMessageFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, $"{eventId}.json");

            File.WriteAllText(filePath, messageString, Encoding.UTF8);

            var logger = GetLogger(LoggerSubCategories.SaveMessageToLocal);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation($"SaveMessageToLocal,filePath:{filePath} messageString:{messageString}");
            }
        }

        void LocalMessageSendHandlerOnceHandler()
        {
            var folderPath = GetFailMessageFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var files = new DirectoryInfo(folderPath).GetFiles()?.OrderBy(m => m.CreationTime).ToList();

            var logger = GetLogger(LoggerSubCategories.LocalMessageSendHandler);

            if (!(files?.Count > 0))
            {
                logger.LogInformation("get local messages, empty");
                return;
            }

            logger.LogInformation($"get local messages, count:{files.Count}");

            foreach (var file in files)
            {
                var messageString = File.ReadAllText(file.FullName, Encoding.UTF8);

                var @event = (Event)JsonConvert.DeserializeObject(messageString, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                try
                {
                    var eventType = @event.GetType();

                    CreateExchangeIfNot(eventType);

                    using (var channel = GetConnection().CreateModel())
                    {
                        var properties = CreateProperties(channel);
                        var exchangeName = GetExchangeName(eventType);
                        channel.BasicPublish(exchangeName, string.Empty, properties, Encoding.UTF8.GetBytes(messageString));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"send message error,skip,eventId:{@event.EventId}");
                    Thread.CurrentThread.Join(ErrorWaitTime);
                    continue;
                }

                logger.LogInformation($"send local message complete,eventId:{@event.EventId}");

                File.Delete(file.FullName);

                logger.LogInformation($"delete local message file complete,eventId:{@event.EventId}");
            }
        }

        void LocalMessageSendHandler()
        {
            var logger = GetLogger(LoggerSubCategories.LocalMessageSendHandler);

            while (true)
            {
                Thread.CurrentThread.Join(LocalMessageOnceHandlerWaitMilliSencods);

                try
                {
                    LocalMessageSendHandlerOnceHandler();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "once handler error");
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

            var exchangeName = GetExchangeName(eventType);

            var messageString = JsonConvert.SerializeObject(@event, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, });

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug($"Publish event,prev send, exchangeName:{exchangeName} messageString:{messageString}");
            }

            try
            {
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
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(Options.Value.FailMessageStoreFolder))
                {
                    throw;
                }

                logger.LogError(ex, $"send message error,save message to local,messageId:{@event.EventId}");

                SaveMessageToLocal(@event.EventId, messageString);
            }

            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T>(IEventHandler<T> handler) where T : Event
        {
            var logger = GetInitLogger();

            var eventType = typeof(T);
            var handlerType = handler.GetType();

            logger.LogDebug($"Subscribe,eventType:{eventType} handlerType:{handlerType}");

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
            //start bus handler
            foreach (var item in _handlers)
            {
                var handler = item.Value;
                var thread = new Thread(new ParameterizedThreadStart(StartHandler));
                thread.IsBackground = true;
                thread.Start(handler);
            }

            if (!string.IsNullOrEmpty(Options.Value.FailMessageStoreFolder))
            {
                //start LocalMessageSendHandler
                {
                    var thread = new Thread(new ThreadStart(LocalMessageSendHandler));
                    thread.IsBackground = true;
                    thread.Start();
                }
            }
        }

        #endregion
    }
}
