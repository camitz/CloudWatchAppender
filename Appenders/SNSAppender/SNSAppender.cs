using System;
using System.Linq;
using System.Reflection;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using AWSAppender.Core;
using AWSAppender.Core.Layout;
using AWSAppender.Core.Services;
using AWSAppender.SNS.Model;
using AWSAppender.SNS.Parsers;
using AWSAppender.SNS.Services;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Util;

namespace AWSAppender.SNS
{
    public class SNSAppender : AWSAppenderBase<SNSDatum>, ISNSAppender
    {
        private SNSClientWrapper _client;
        private static readonly Type _declaringType = typeof(SNSAppender);

        private AmazonSimpleNotificationServiceConfig _clientConfig;

        protected override ClientConfig ClientConfig
        {
            get { return _clientConfig ?? (_clientConfig = new AmazonSimpleNotificationServiceConfig()); }
        }

        public override IEventProcessor<SNSDatum> EventProcessor
        {
            get { return _eventProcessor; }
            set { _eventProcessor = value; }
        }

        protected override void ResetClient()
        {
            _client = null;
        }

        public string Topic { get; set; }
        //public string DelaySeconds { get; set; }

        public string Message { get; set; }

        private IEventProcessor<SNSDatum> _eventProcessor;
        private readonly string _fallbackTopic;

        public SNSAppender()
        {
            _fallbackTopic = "unspecified";
            if (Assembly.GetEntryAssembly() != null)
                _fallbackTopic = Assembly.GetEntryAssembly().GetName().Name;

            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(SNSDatum), new SNSDatumRenderer());


        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            EventMessageParser = EventMessageParser ?? new SNSMessageParser(ConfigOverrides);

            _client = new SNSClientWrapper(EndPoint, AccessKey, Secret, ClientConfig);

            _eventProcessor = new SNSEventProcessor(Topic, Message)
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

            var snsDatum = _eventProcessor.ProcessEvent(loggingEvent, RenderLoggingEvent(loggingEvent)).Single();


            _client.AddPublishRequest(new PublishRequestWrapper
                                      {
                                          Message= snsDatum.Message,
                                          Topic = snsDatum.Topic ?? _fallbackTopic
                                      });
        }
    }
}