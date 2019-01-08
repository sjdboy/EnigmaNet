using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using EnigmaNet.Bus;
using EnigmaNet.Utils;
using EnigmaNet.QCloud.CMQ;
using EnigmaNet.QCloud.CMQ.Models;

namespace EnigmaNet.CMQBus
{
    public class CMQEventBus : IEventPublisher, IEventSubscriber
    {
        #region private

        #region fields

        const int TopicMessageMaxSize = 1024 * 1024;
        const int QueueMessageMaxSize = 1024 * 1024;
        const int QueueMessageVisibilityTimeout = 30;
        const string ShaIdPrefix = "sha-";

        const int ErrorWaitTimes = 1000;
        const int EmptyWaitTimes = 1000;
        const int PollingWaitSeconds = 3;
        const int CmqIdLength = 64;

        ConcurrentDictionary<Type, bool> _buildedQueues = new ConcurrentDictionary<Type, bool>();
        ConcurrentDictionary<Type, bool> _buildedTopics = new ConcurrentDictionary<Type, bool>();
        ConcurrentDictionary<string, bool> _buildedSubscribes = new ConcurrentDictionary<string, bool>();
        ConcurrentDictionary<Type, ConcurrentBag<Type>> _handlerToEvents = new ConcurrentDictionary<Type, ConcurrentBag<Type>>();
        ConcurrentDictionary<Type, object> _handlers = new ConcurrentDictionary<Type, object>();

        System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex("^[a-zA-Z][a-zA-z0-9-]{0,63}$");
        CMQClient _cmqClient;
        ILogger _logger;
        ILogger _initLogger;

        string PublishEventNamePrefix { get { return Options.Value.PublishEventNamePrefix; } }

        #endregion

        #region methods

        CMQClient GetCmqClient()
        {
            if (_cmqClient == null)
            {
                _cmqClient = new CMQClient(
                    Options.Value.RegionHost,
                    Options.Value.SecrectId,
                    Options.Value.SecrectKey,
                    Options.Value.IsHttps,
                    Options.Value.SignatureMethod,
                    LoggerFactory);
            }
            return _cmqClient;
        }

        ILogger GetLogger()
        {
            if (_logger == null)
            {
                _logger = LoggerFactory.CreateLogger<CMQEventBus>();
            }
            return _logger;
        }

        ILogger GetInitLogger()
        {
            if (_initLogger == null)
            {
                _initLogger = LoggerFactory.CreateLogger("EnigmaNet.CMQBus.CMQEventBus_Init");
            }
            return _initLogger;
        }

        string GetCMQObjectId(string originalId)
        {
            //队列名称是一个不超过 64 个字符的字符串，必须以字母为首字符，剩余部分可以包含字母、数字和横划线(-)。

            originalId = originalId.ToLower();

            string cmqId;
            if (originalId.Length > CmqIdLength)
            {
                //名称加前缀保证是字母开头
                cmqId = ShaIdPrefix + SecurityUtils.Sha1(originalId).ToLower();
            }
            else
            {
                cmqId = originalId;
            }

            cmqId = cmqId.Replace(".", "-").Replace("_", "-");

            if (!_regex.IsMatch(cmqId))
            {
                throw new ArgumentException($"cmqId error,cmqId:{cmqId}");
            }

            return cmqId;
        }

        string GetQueueId(Type handlerType)
        {
            if (string.IsNullOrEmpty(Options.Value.InstanceId))
            {
                throw new InvalidOperationException("InstanceId is empty");
            }

            var typeName = handlerType.FullName;

            return GetCMQObjectId($"{Options.Value.InstanceId}-{typeName}");
        }

        string GetTopicId(Type eventType)
        {
            //var eventType = typeof(TEvent);

            if (string.IsNullOrEmpty(Options.Value.InstanceId))
            {
                throw new InvalidOperationException("InstanceId is empty");
            }

            var eventTypeName = eventType.FullName;

            if (!string.IsNullOrEmpty(PublishEventNamePrefix) && eventTypeName.StartsWith(PublishEventNamePrefix))
            {
                //公共事件不用加前缀
                return GetCMQObjectId(eventTypeName);
            }
            else
            {
                //私有事件加前缀以区分
                return GetCMQObjectId($"{Options.Value.InstanceId}-{eventTypeName}");
            }
        }

        string GetSubscribeId(Type handlerType, Type eventType)
        {
            if (string.IsNullOrEmpty(Options.Value.InstanceId))
            {
                throw new InvalidOperationException("InstanceId is empty");
            }

            return GetCMQObjectId($"{Options.Value.InstanceId}-{eventType.FullName}-{handlerType.FullName}");
        }

        async Task CreateTopicIfNotExistsAsync(Type eventType)
        {
            if (_buildedTopics.ContainsKey(eventType))
            {
                return;
            }

            var topicId = GetTopicId(eventType);

            var client = GetCmqClient();

            var logger = GetInitLogger();

            var result = await client.CreateTopicAsync(topicId, TopicMessageMaxSize, TopicFilterType.Tag);

            logger.LogInformation($"create topic finish,result:{JsonConvert.SerializeObject(result)}");

            //成功或同名存在
            if (result.Code == 0)
            {
                _buildedTopics.TryAdd(eventType, true);
                logger.LogInformation($"create topic success");
            }
            //同名存在错误（控制台实测）:(4460) (10550)topic is already existed(cmq-topic[#4000])
            else if (result.Code == 4460 && result.GetModuleCode() == 10550)
            {
                _buildedTopics.TryAdd(eventType, true);
                logger.LogInformation($"create topic success2");
            }
            else if (result.Code == 4000 && result.Message == "(4460) (10550)topic is already existed")
            {
                _buildedTopics.TryAdd(eventType, true);
                logger.LogInformation($"create topic success3");
            }
            else
            {
                throw new Exception($"create cmq topic error,code:{result.Code} message:{result.Message} requestId:{result.RequestId}");
            }
        }

        async Task CreateQueueIfNotExiststAsync(Type handlerType)
        {
            if (_buildedQueues.ContainsKey(handlerType))
            {
                return;
            }

            var queueId = GetQueueId(handlerType);

            var client = GetCmqClient();

            var logger = GetInitLogger();

            var result = await client.CreateQueueAsync(queueId, null, null, QueueMessageVisibilityTimeout, QueueMessageMaxSize);

            logger.LogInformation($"create queue finish,result:{JsonConvert.SerializeObject(result)}");

            //成功或已存在
            if (result.Code == 0)
            {
                _buildedQueues.TryAdd(handlerType, true);
                logger.LogInformation($"create queue success");
            }
            //同名存在错误（控制台实测）:(4460) (10210)queue is already existed(cmq-queue[#4000])
            else if (result.Code == 4460 && result.GetModuleCode() == 10210)
            {
                _buildedQueues.TryAdd(handlerType, true);
                logger.LogInformation($"create queue success2");
            }
            else if (result.Code == 4000 && result.Message == "(4460) (10210)queue is already existed")
            {
                _buildedQueues.TryAdd(handlerType, true);
                logger.LogInformation($"create queue success3");
            }
            else
            {
                throw new Exception($"create cmq queue error,code:{result.Code} message:{result.Message} requestId:{result.RequestId}");
            }
        }

        async Task CreateSubscribeIfNotExistsAsync(Type handlerType, Type eventType)
        {
            var subscribeKey = $"{eventType.FullName}_{handlerType.FullName}";
            if (_buildedSubscribes.ContainsKey(subscribeKey))
            {
                return;
            }

            var subscriptionId = GetSubscribeId(handlerType, eventType);
            var queueId = GetQueueId(handlerType);
            var topicId = GetTopicId(eventType);

            var client = GetCmqClient();

            var logger = GetInitLogger();

            var result = await client.SubscribeAsync(topicId, subscriptionId, SubscribeProtocol.Queue, queueId);

            logger.LogInformation($"subscribe finish,result:{JsonConvert.SerializeObject(result)}");

            //成功或同名存在
            if (result.Code == 0)
            {
                _buildedSubscribes.TryAdd(subscribeKey, true);
                logger.LogInformation($"subscribe success");
            }
            //同名存在错误（控制台实测）:(4000) (10470)subscribtion is already existed(cmq-topic[#4000])
            else if (result.Code == 4490 && result.GetModuleCode() == 10470)
            {
                _buildedSubscribes.TryAdd(subscribeKey, true);
                logger.LogInformation($"subscribe success2");
            }
            else if (result.Code == 4000 && result.Message == "(4000) (10470)subscribtion is already existed")
            {
                _buildedSubscribes.TryAdd(subscribeKey, true);
                logger.LogInformation($"subscribe success3");
            }
            else
            {
                throw new Exception($"subscribe cmq error,code:{result.Code} message:{result.Message} requestId:{result.RequestId}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handlerTypeObject"></param>
        /// <remarks>
        /// 消息API参考：https://cloud.tencent.com/document/product/406/5839
        /// </remarks>
        void StartHandler(object handler)
        {
            var handlerType = handler.GetType();
            _handlerToEvents.TryGetValue(handlerType, out ConcurrentBag<Type> supportEvents);

            //create cmq object
            {
                var initLogger = GetInitLogger();
                initLogger.LogInformation($"StartHandler,handlerType:{handlerType.FullName}");
                int errorTimes = 0;
                while (true)
                {
                    try
                    {
                        //create queue
                        CreateQueueIfNotExiststAsync(handlerType).Wait();

                        //create topic and subscribe
                        _handlerToEvents.TryGetValue(handlerType, out ConcurrentBag<Type> eventTypes);
                        foreach (var eventType in eventTypes)
                        {
                            CreateTopicIfNotExistsAsync(eventType).Wait();
                            CreateSubscribeIfNotExistsAsync(handlerType, eventType).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        errorTimes++;
                        initLogger.LogError(ex, $"StartHandler,create cmq object error");

                        if (errorTimes > 5)
                        {
                            initLogger.LogInformation($"StartHandler,try create cmq object more time,and out");
                            break;
                        }
                        else
                        {
                            Thread.CurrentThread.Join(ErrorWaitTimes);
                            continue;
                        }
                    }

                    break;
                }
            }

            //return;
            //handle message
            var logger = GetLogger();
            var client = GetCmqClient();
            var queueId = GetQueueId(handlerType);
            while (true)
            {
                try
                {
                    var messageResult = client.ReceiveMessageAsync(queueId, PollingWaitSeconds).Result;
                    if (messageResult.Code != 0)
                    {
                        //no message
                        if (messageResult.Code == 7000 && messageResult.GetModuleCode() == 10200)
                        {
                            //继续
                            logger.LogInformation($"receive message empty1,code:{messageResult.Code}");
                            Thread.CurrentThread.Join(EmptyWaitTimes);
                            continue;
                        }
                        //队列中有太多不可见或者延时消息，这里建议用户稍等一会再继续消费。
                        else if (messageResult.Code == 6070 && messageResult.GetModuleCode() == 10690)
                        {
                            //继续
                            logger.LogInformation($"receive message empty2,code:{messageResult.Code}");
                            Thread.CurrentThread.Join(EmptyWaitTimes);
                            continue;
                        }
                        else
                        {
                            throw new Exception($"receive cmq message error,queueId:{queueId} result:{Newtonsoft.Json.JsonConvert.SerializeObject(messageResult)}");
                        }
                    }

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug($"receive a messsage,content:{messageResult.MsgBody}");
                    }

                    var @event = (Event)JsonConvert.DeserializeObject(messageResult.MsgBody, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, });
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
                    }

                    logger.LogInformation($"start delete message,queueId:{queueId} msgId:{messageResult.MsgId} receiptHandle:{messageResult.ReceiptHandle}");

                    var deleteResult = client.DeleteMessageAsync(queueId, messageResult.ReceiptHandle).Result;

                    logger.LogDebug($"delete message,code:{deleteResult.Code} message:{deleteResult.Message} requestId:{deleteResult.RequestId}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"handle error,handlerType:{handlerType.FullName}");
                    Thread.CurrentThread.Join(ErrorWaitTimes);
                }
            }
        }

        #endregion

        #endregion

        #region publish

        public IOptions<CMQBusOptions> Options { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public async Task PublishAsync<T>(T @event) where T : Event
        {
            var eventType = @event.GetType();

            await CreateTopicIfNotExistsAsync(eventType);

            var topicId = GetTopicId(eventType);

            var client = GetCmqClient();

            var messageBody = JsonConvert.SerializeObject(
                @event,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                });

            var result = await client.PublishMessageAsync(topicId, messageBody);

            //参考API:https://cloud.tencent.com/document/product/406/7411
            if (result.Code != 0)
            {
                //本主题没有订阅者
                if (result.Code == 6030 && result.GetModuleCode() == 10650)
                {
                    //忽略
                }
                else
                {
                    throw new Exception($"publish cmq message error,code:{result.Code} message:{result.Message} requestId:{result.RequestId}");
                }
            }
        }

        public Task SubscribeAsync<T>(IEventHandler<T> handler) where T : Event
        {
            var eventType = typeof(T);
            var handlerType = handler.GetType();

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
