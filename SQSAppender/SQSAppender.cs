using System;
using System.Linq;
using System.Reflection;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Runtime;
using CloudWatchAppender.Layout;
using CloudWatchAppender.Model;
using CloudWatchAppender.Parsers;
using CloudWatchAppender.Services;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Util;

namespace CloudWatchAppender
{
    public class SQSAppender : CloudWatchAppenderBase<SQSDatum>, ISQSAppender
    {
        private SQSClientWrapper _client;
        private static readonly Type _declaringType = typeof (SQSAppender);

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

        public string StreamName { get; set; }

        public string Message { get; set; }

        public new string Timestamp { get; set; }

        private IEventProcessor<SQSDatum> _eventProcessor;

        public SQSAppender()
        {
            if (Assembly.GetEntryAssembly() != null)
                QueueName = Assembly.GetEntryAssembly().GetName().Name;
            else
                QueueName = "unspecified";

            StreamName = "unspecified";

            var hierarchy = ((Hierarchy) log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof (SQSDatum), new SQSDatumRenderer());


        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            EventMessageParser = EventMessageParser ?? new SQSMessageParser(ConfigOverrides);

            _client = new SQSClientWrapper(EndPoint, AccessKey, Secret, ClientConfig);

            _eventProcessor = new LogEventProcessor(QueueName, StreamName, Timestamp, Message)
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

            _client.AddSendMessageRequest(new SendMessageBatchRequestWrapper
                                          {
                                              QueueName = sqsDatum.QueueName,
                                              Entries = new[]
                                                        {
                                                            new SendMessageBatchRequestEntry
                                                            {
                                                                MessageBody = sqsDatum.Message
                                                            }
                                                        }.ToList()
                                          }
                );



        }
    }
}