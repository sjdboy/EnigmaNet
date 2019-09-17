using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using EnigmaNet.Bus;
using System.Collections.Concurrent;
using System.Threading;
using RabbitMQ.Client;
using Newtonsoft.Json;
using EnigmaNet.Utils;
using System.IO;

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
        const int LocalMessageOnceHandlerWaitMilliSencods = 1000 * 60;

        ConcurrentDictionary<Type, ConcurrentBag<Type>> _handlerToMessages = new ConcurrentDictionary<Type, ConcurrentBag<Type>>();
        ConcurrentDictionary<Type, object> _handlers = new ConcurrentDictionary<Type, object>();
        ConcurrentDictionary<Type, bool> _handlerQueuesBuilded = new ConcurrentDictionary<Type, bool>();
        ConcurrentDictionary<Type, bool> _messageExchangesBuilded = new ConcurrentDictionary<Type, bool>();
        ConcurrentBag<int> _buildTimeQueues = new ConcurrentBag<int>();
        bool _coreExchangeBuild = false;

        string InstanceId { get { return Options.Value.InstanceId; } }
        string CoreExchangeName { get { return $"{InstanceId}_dm_{Options.Value.CoreExchangeName}"; } }

        IConnection _connection;
        object _connectionLocker = new object();

        ILogger GetLogger(string subCategory)
        {
            return LoggerFactory.CreateLogger($"EnigmaNet.RabbitMQBus.RabbitMQDelayMessageBus_{subCategory}");
        }

        ILogger GetInitLogger()
        {
            return GetLogger(LoggerSubCategories.Init);
        }

        ILogger GetLogger()
        {
            return LoggerFactory.CreateLogger<RabbitMQEventBus>();
        }

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

        string GetExchangeNameForMessage(Type messageType)
        {
            return $"{InstanceId}_dm_{messageType.FullName}";
        }

        string GetHandlerQueueName(Type handlerType)
        {
            return $"{InstanceId}_dm_{handlerType.FullName}";
        }

        string GetFailQueueName(Type handlerType)
        {
            return $"{InstanceId}_dm_{handlerType.FullName}_fail";
        }

        string GetHeaderValueForMessage(Type messageType)
        {
            return messageType.FullName;
        }

        string GetDelayQueueName(int delaySeconds)
        {
            return $"{InstanceId}_dm_ts_{delaySeconds}";
        }

        void HandleMessage(IModel channel, object handler)
        {
            var handlerType = handler.GetType();
            var queueName = GetHandlerQueueName(handlerType);
            _handlerToMessages.TryGetValue(handlerType, out ConcurrentBag<Type> supportMessages);

            var logger = GetLogger();

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

                    var delayMessage = (DelayMessage)JsonConvert.DeserializeObject(messageStr, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                    var messageType = delayMessage.GetType();

                    if (!supportMessages.Contains(messageType))
                    {
                        //不支持该事件的处理
                        logger.LogInformation($"not support that message,handlerType:{handlerType} messageType:{messageType}");
                    }
                    else
                    {
                        logger.LogInformation($"start to handle message,handlerType:{handlerType} messageType:{messageType}");

                        var task = (Task)typeof(IDelayMessageHandler<>)
                            .MakeGenericType(messageType)
                            .GetMethod("HandleAsync")
                            .Invoke(handler, new object[] { delayMessage });

                        task.Wait();

                        logger.LogInformation($"handle message complete,handlerType:{handlerType} messageType:{messageType}");
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

            //init
            {
                var queueName = GetHandlerQueueName(handlerType);

                var logger = GetInitLogger();

                logger.LogInformation($"handler init, handlerType:{handlerType}");

                _handlerToMessages.TryGetValue(handlerType, out ConcurrentBag<Type> messageTypes);

                //crate queue
                while (true)
                {
                    try
                    {
                        logger.LogInformation($"handler init,beging create rabbitmq object, handlerType:{handlerType},queueName:{queueName}");

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
                                logger.LogDebug($"handler init,create queue finish,queueName:{queueName}, handlerType:{handlerType}");

                                channel.QueueDeclare(failQueueName, true, false, false, failQueueParameters);
                                logger.LogDebug($"handler init,create fail queue finish,failQueueName:{failQueueName}, handlerType:{handlerType}");

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

                                    logger.LogDebug($"handler init,create message exchange finish,exchange:{messageExchangeName},bind exchange:{CoreExchangeName}");

                                    _messageExchangesBuilded.TryAdd(messageType, true);
                                }

                                //subscribe
                                channel.QueueBind(queueName, messageExchangeName, string.Empty);

                                logger.LogDebug($"handler init,bind queue to message exchange finish,queueName:{queueName} messageExchangeName:{messageExchangeName}");
                            }

                            logger.LogDebug($"handler init,bind queue finish,queueName:{queueName}");
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

            var logger = GetInitLogger();

            logger.LogDebug($"handler init,create core exchange finish,exchange:{coreExchangeName}");
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

            var logger = GetInitLogger();
            logger.LogDebug($"CreateDelayQueue,create delay queue finish,queue:{queueName}");
            _buildTimeQueues.Add(delaySeconds);
        }

        IBasicProperties CreateProperties(IModel channel,Type messageType)
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

        void SaveMessageToLocal(string messageId, string messageString)
        {
            var folderPath = GetFailMessageFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, $"{messageId}.json");

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

                var delayMessage = (DelayMessage)JsonConvert.DeserializeObject(messageString, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                var messageType = delayMessage.GetType();

                var delaySeconds = delayMessage.DelaySeconds;

                var delayQueueName = GetDelayQueueName(delaySeconds);

                try
                {
                    using (var channel = GetConnection().CreateModel())
                    {
                        CreateDelayQueueIfNot(delaySeconds, channel);

                        channel.BasicPublish(string.Empty, delayQueueName, CreateProperties(channel, messageType), Encoding.UTF8.GetBytes(messageString));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"send message error,skip,messageId:{delayMessage.MessageId}");
                    Thread.CurrentThread.Join(ErrorWaitTime);
                    continue;
                }

                logger.LogInformation($"send local message complete,messageId:{delayMessage.MessageId}");

                File.Delete(file.FullName);

                logger.LogInformation($"delete local message file complete,messageId:{delayMessage.MessageId}");
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

        public IOptions<RabbitMQDelayMessageBusOptions> Options { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public Task PublishAsync<T>(T message) where T : DelayMessage
        {
            if (message.DelaySeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(message.DelaySeconds));
            }

            var logger = GetLogger();

            var delaySeconds = message.DelaySeconds;

            var messageType = message.GetType();

            var queueName = GetDelayQueueName(delaySeconds);

            var messageString = JsonConvert.SerializeObject(message, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, });

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug($"Publish message,prev send, queueName:{queueName} messageString:{messageString}");
            }

            try
            {
                using (var channel = GetConnection().CreateModel())
                {
                    CreateDelayQueueIfNot(delaySeconds, channel);

                    channel.BasicPublish(string.Empty, queueName, CreateProperties(channel, messageType), Encoding.UTF8.GetBytes(messageString));
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

                logger.LogError(ex, $"send message error,save message to local,messageId:{message.MessageId}");

                SaveMessageToLocal(message.MessageId, messageString);
            }

            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T>(IDelayMessageHandler<T> handler) where T : DelayMessage
        {
            var logger = GetInitLogger();

            var messageType = typeof(T);
            var handlerType = handler.GetType();

            logger.LogDebug($"Subscribe,messageType:{messageType} handlerType:{handlerType}");

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

    }
}
