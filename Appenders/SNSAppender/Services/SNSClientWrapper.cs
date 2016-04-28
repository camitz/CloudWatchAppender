using System;
using System.Collections.Concurrent;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AWSAppender.Core.Services;

namespace SQSAppender.Services
{
    public class SNSClientWrapper : ClientWrapperBase<AmazonSimpleNotificationServiceConfig, AmazonSimpleNotificationServiceClient>
    {
        private static readonly LockObject _lockObject = new LockObject();

        private readonly ConcurrentDictionary<string, string> _validatedTopics = new ConcurrentDictionary<string, string>();
        public SNSClientWrapper(string endPoint, string accessKey, string secret, ClientConfig clientConfig)
            : base(endPoint, accessKey, secret, clientConfig)
        {
        }

        internal void AddPublishRequest(PublishRequestWrapper sendMessageRequest)
        {
            AddRequest(() => SendMessages(sendMessageRequest));
        }

        private AmazonWebServiceResponse SendMessages(PublishRequestWrapper publishRequest)
        {
            if (!_validatedTopics.ContainsKey(publishRequest.Topic))
            {
                lock (_lockObject)
                {
                    if (!_validatedTopics.ContainsKey(publishRequest.Topic))
                    {
                        var response = Client.CreateTopic(publishRequest.Topic);
                        _validatedTopics.TryAdd(publishRequest.Topic, response.TopicArn);
                    }
                }
            }

            lock (_lockObject)
            {
                string topicArn;
                _validatedTopics.TryGetValue(publishRequest.Topic, out topicArn);

                var request = publishRequest.PublishRequest;
                request.TopicArn = topicArn;
                var sendMessageBatchResponse = Client.Publish(request);
                return sendMessageBatchResponse;
            }

        }
    }

    internal class PublishRequestWrapper
    {
        public string Topic { get; set; }
        public string Message { get; set; }

        public PublishRequest PublishRequest
        {
            get
            {
                return new PublishRequest
                       {
                           Message = Message
                       };
            }
        }
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