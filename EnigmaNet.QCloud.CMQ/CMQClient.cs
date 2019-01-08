using EnigmaNet.QCloud.CMQ.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.QCloud.CMQ
{
    public class CMQClient
    {
        Utils.CMQRequester _requester;

        public CMQClient(string regionHost, string secrectId, string secrectKey, bool isHttps, string signatureMethod, ILoggerFactory loggerFactory)
        {
            _requester = new Utils.CMQRequester()
            {
                RegionHost = regionHost,
                IsHttps = isHttps,
                SecrectId = secrectId,
                SecrectKey = secrectKey,
                SignatureMethod = string.IsNullOrEmpty(signatureMethod) ? Utils.SignatureMethods.HmacSHA1 : signatureMethod,
                LoggerFactory = loggerFactory,
            };
        }

        public async Task<CreateQueueResultModel> CreateQueueAsync(
            string queueName,
            int? maxMsgHeapNum = null,
            int? pollingWaitSeconds = null,
            int? visibilityTimeout = null,
            int? maxMsgSize = null,
            int? msgRetentionSeconds = null,
            int? rewindSeconds = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }
            var parameters = new Dictionary<string, string>();

            parameters.Add("queueName", queueName);
            if (maxMsgHeapNum.HasValue)
            {
                parameters.Add("maxMsgHeapNum", maxMsgHeapNum.Value.ToString());
            }
            if (pollingWaitSeconds.HasValue)
            {
                parameters.Add("pollingWaitSeconds", pollingWaitSeconds.Value.ToString());
            }
            if (visibilityTimeout.HasValue)
            {
                parameters.Add("visibilityTimeout", visibilityTimeout.Value.ToString());
            }
            if (maxMsgSize.HasValue)
            {
                parameters.Add("maxMsgSize", maxMsgSize.Value.ToString());
            }
            if (msgRetentionSeconds.HasValue)
            {
                parameters.Add("msgRetentionSeconds", msgRetentionSeconds.Value.ToString());
            }
            if (rewindSeconds.HasValue)
            {
                parameters.Add("rewindSeconds", rewindSeconds.Value.ToString());
            }

            return await _requester.GetAsync<CreateQueueResultModel>(
                Utils.Actions.CreateQueue, parameters);
        }

        public async Task<SendMessageResultModel> SendMessageAsync(
            string queueName, string msgBody, int? delaySeconds = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }
            if (string.IsNullOrWhiteSpace(msgBody))
            {
                throw new ArgumentNullException(nameof(msgBody));
            }
            var parameters = new Dictionary<string, string>();

            parameters.Add("queueName", queueName);
            parameters.Add("msgBody", msgBody);
            if (delaySeconds.HasValue)
            {
                parameters.Add("delaySeconds", delaySeconds.Value.ToString());
            }

            return await _requester.GetAsync<SendMessageResultModel>(
                Utils.Actions.SendMessage, parameters);
        }

        public async Task<ReceiveMessageResultModel> ReceiveMessageAsync(
            string queueName, int? pollingWaitSeconds = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }
            var parameters = new Dictionary<string, string>();

            parameters.Add("queueName", queueName);
            if (pollingWaitSeconds.HasValue)
            {
                parameters.Add("pollingWaitSeconds", pollingWaitSeconds.Value.ToString());
            }

            return await _requester.GetAsync<ReceiveMessageResultModel>(
               Utils.Actions.ReceiveMessage, parameters);
        }

        public async Task<DeleteMessageResultModel> DeleteMessageAsync(string queueName, string receiptHandle)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }
            if (string.IsNullOrWhiteSpace(receiptHandle))
            {
                throw new ArgumentNullException(nameof(receiptHandle));
            }

            var parameters = new Dictionary<string, string>();

            parameters.Add("queueName", queueName);
            parameters.Add("receiptHandle", receiptHandle);

            return await _requester.GetAsync<DeleteMessageResultModel>(
                Utils.Actions.DeleteMessage, parameters);
        }

        public async Task<CreateTopicResultModel> CreateTopicAsync(string topicName,
            int? maxMsgSize = null, TopicFilterType? filterType = null)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentNullException(nameof(topicName));
            }

            var parameters = new Dictionary<string, string>();

            parameters.Add("topicName", topicName);
            if (maxMsgSize.HasValue)
            {
                parameters.Add("maxMsgSize", maxMsgSize.Value.ToString());
            }
            if (filterType.HasValue)
            {
                parameters.Add("filterType", filterType.Value == TopicFilterType.BindingKey ? "2" : "1");
            }

            return await _requester.GetAsync<CreateTopicResultModel>(
                Utils.Actions.CreateTopic, parameters);
        }

        public async Task<SubscribeResultModel> SubscribeAsync(
            string topicName, string subscriptionName,
            SubscribeProtocol protocol, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentNullException(nameof(topicName));
            }
            if (string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentNullException(nameof(subscriptionName));
            }
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
            var parameters = new Dictionary<string, string>();

            parameters.Add("topicName", topicName);
            parameters.Add("subscriptionName", subscriptionName);
            parameters.Add("protocol", protocol == SubscribeProtocol.Http ? "http" : "queue");
            parameters.Add("endpoint", endpoint);

            return await _requester.GetAsync<SubscribeResultModel>(
                Utils.Actions.Subscribe, parameters);
        }

        public async Task<PublishMessageResultModel> PublishMessageAsync(string topicName, string msgBody)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentNullException(nameof(topicName));
            }
            if (string.IsNullOrWhiteSpace(msgBody))
            {
                throw new ArgumentNullException(nameof(msgBody));
            }
            var parameters = new Dictionary<string, string>();

            parameters.Add("topicName", topicName);
            parameters.Add("msgBody", msgBody);

            return await _requester.GetAsync<PublishMessageResultModel>(
                Utils.Actions.PublishMessage, parameters);
        }

    }
}
