using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSAppender.Core.Services;

namespace SQSAppender.Services
{
    public class SQSClientWrapper : ClientWrapperBase<AmazonSQSConfig, AmazonSQSClient>
    {
        private static readonly LockObject _lockObject = new LockObject();

        private readonly ConcurrentDictionary<string, string> _validatedQueueNames = new ConcurrentDictionary<string, string>();
        public SQSClientWrapper(string endPoint, string accessKey, string secret, ClientConfig clientConfig)
            : base(endPoint, accessKey, secret, clientConfig)
        {
        }

        internal void AddSendMessageRequest(SendMessageBatchRequestWrapper sendMessageRequest)
        {
            AddRequest(() => SendMessages(sendMessageRequest));
        }

        private AmazonWebServiceResponse SendMessages(SendMessageBatchRequestWrapper sendMessageBatchRequest)
        {
            if (!_validatedQueueNames.ContainsKey(sendMessageBatchRequest.QueueName))
            {
                lock (_lockObject)
                {
                    if (!_validatedQueueNames.ContainsKey(sendMessageBatchRequest.QueueName))
                    {
                        var response = Client.CreateQueue(sendMessageBatchRequest.QueueName);
                        _validatedQueueNames.TryAdd(sendMessageBatchRequest.QueueName, response.QueueUrl);
                    }
                }
            }

            lock (_lockObject)
            {
                AmazonWebServiceResponse ret = null;

                string queueUrl;
                _validatedQueueNames.TryGetValue(sendMessageBatchRequest.QueueName, out queueUrl);

                var messageBatchRequest = sendMessageBatchRequest.BatchRequest;
                messageBatchRequest.QueueUrl = queueUrl;
                var sendMessageBatchResponse = Client.SendMessageBatch(messageBatchRequest);
                return sendMessageBatchResponse;
            }

        }
    }

    internal class SendMessageBatchRequestWrapper
    {
        public string QueueName { get; set; }

        public SendMessageBatchRequest BatchRequest
        {
            get { return new SendMessageBatchRequest {Entries = Entries}; }
        }

        public List<SendMessageBatchRequestEntry> Entries { get; set; }
    }

    internal class LockObject
    {
        private int _id;

        public LockObject()
        {
            _id = new Random().Next();
        }
    }
}