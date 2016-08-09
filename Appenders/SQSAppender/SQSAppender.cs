using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSAppender.Core;
using AWSAppender.Core.Layout;
using AWSAppender.Core.Services;
using AWSAppender.SQS.Model;
using AWSAppender.SQS.Parsers;
using AWSAppender.SQS.Services;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Util;

namespace AWSAppender.SQS
{
    public class SQSAppender : AWSAppenderBase<SQSDatum>, ISQSAppender
    {
        private SQSClientWrapper _client;
        private static readonly Type _declaringType = typeof(SQSAppender);

        private AmazonSQSConfig _clientConfig;

        protected override ClientConfig ClientConfig
        {
            get { return _clientConfig ?? (_clientConfig = new AmazonSQSConfig()); }
        }

        public override IEventProcessor<SQSDatum> EventProcessor
        {
            get { return _eventProcessor; }
            set { _eventProcessor = value; }
        }

        protected override void ResetClient()
        {
            _client = null;
        }

        public string QueueName { get; set; }
        public string DelaySeconds { get; set; }

        public string Message { get; set; }

        private IEventProcessor<SQSDatum> _eventProcessor;
        private readonly string _fallbackQueueName;
        private Regex _queueNameRegex;

        public SQSAppender()
        {
            _queueNameRegex = new Regex(@"^[a-zA-Z0-9_-]{1,80}$");

            _fallbackQueueName = "unspecified";
            if (Assembly.GetEntryAssembly() != null)
                _fallbackQueueName = Assembly.GetEntryAssembly().GetName().Name.Replace(".", "_");

            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(SQSDatum), new SQSDatumRenderer());


        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            EventMessageParser = EventMessageParser ?? new SQSMessageParser(ConfigOverrides);

            _client = new SQSClientWrapper(EndPoint, AccessKey, Secret, ClientConfig);

            _eventProcessor = new SQSEventProcessor(QueueName, Message, DelaySeconds)
                              {
                                  EventMessageParser = EventMessageParser
                              };

            if (Layout == null)
                Layout = new PatternLayout("%message");

        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            LogLog.Debug(_declaringType, "Appending");


            if (!EventRateLimiter.Request(loggingEvent.TimeStamp))
            {
                LogLog.Debug(_declaringType, "Appending denied due to event limiter saturated.");
                return;
            }

            var sqsDatum = _eventProcessor.ProcessEvent(loggingEvent, RenderLoggingEvent(loggingEvent)).Single();

            if (System.Text.UTF8Encoding.UTF8.GetByteCount(sqsDatum.Message) > 256 * 1024)
                throw new MessageTooLargeException(sqsDatum.Message);

            if (sqsDatum.QueueName != null && !_queueNameRegex.IsMatch(sqsDatum.QueueName))
                throw new MessageTooLargeException(sqsDatum.Message);

            var sendMessageBatchRequestEntry = new SendMessageBatchRequestEntry
                                               {
                                                   MessageBody = sqsDatum.Message,
                                                   Id = sqsDatum.ID,
                                               };

            if (sqsDatum.DelaySeconds.HasValue)
                sendMessageBatchRequestEntry.DelaySeconds = sqsDatum.DelaySeconds.Value;

            _client.AddSendMessageRequest(new SendMessageBatchRequestWrapper
                                          {
                                              QueueName = sqsDatum.QueueName ?? _fallbackQueueName,
                                              Entries = new[]
                                                        {
                                                            sendMessageBatchRequestEntry
                                                        }.ToList()
                                          }
                );


        }
    }
}