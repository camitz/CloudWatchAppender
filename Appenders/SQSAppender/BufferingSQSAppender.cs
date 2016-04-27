//using Amazon.CloudWatchLogs;
//using Amazon.CloudWatchLogs.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSAppender.Core.Layout;
using AWSAppender.Core.Services;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Util;
using SQSAppender.Model;
using SQSAppender.Parsers;
using SQSAppender.Services;

namespace SQSAppender
{
    public class BufferingSQSAppender : BufferingAWSAppenderBase<SQSDatum>, ISQSAppender
    {
        private EventRateLimiter _eventRateLimiter = new EventRateLimiter();
        private SQSClientWrapper _client;
        private static readonly Type _declaringType = typeof(BufferingSQSAppender);
        private string _timestamp;

        private string _queueName;
        private string _message;

        private IEventProcessor<SQSDatum> _eventProcessor;

        private bool _configOverrides = true;

        private AmazonSQSConfig _clientConfig;

        public override IEventProcessor<SQSDatum> EventProcessor
        {
            get { return _eventProcessor; }
            set { _eventProcessor = value; }
        }

        protected override ClientConfig ClientConfig
        {
            get { return _clientConfig ?? (_clientConfig = new AmazonSQSConfig()); }
        }

        protected override void ResetClient()
        {
            _client = null;
        }

        public string QueueName
        {
            set
            {
                _queueName = value;
                _eventProcessor = null;
            }
            get { return _queueName; }
        }



        public string Message
        {
            set
            {
                _message = value;
                _eventProcessor = null;
            }
            get { return _message; }
        }


        public new bool ConfigOverrides
        {
            set
            {
                _configOverrides = value;
                _eventProcessor = null;
            }
        }

        public int RateLimit
        {
            set { _eventRateLimiter = new EventRateLimiter(value); }
        }

        public BufferingSQSAppender()
        {
            if (Assembly.GetEntryAssembly() != null)
                _queueName = Assembly.GetEntryAssembly().GetName().Name;
            else
                _queueName = "unspecified";

            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(SQSDatum), new SQSDatumRenderer());


        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            EventMessageParser = EventMessageParser ?? new SQSMessageParser(_configOverrides);

            _client = new SQSClientWrapper(EndPoint, AccessKey, Secret, ClientConfig);

            _eventProcessor = new SQSEventProcessor(_queueName, _message)
                              {
                                  EventMessageParser = EventMessageParser
                              };


            if (Layout == null)
                Layout = new PatternLayout("%message");

        }


        protected override void Append(LoggingEvent loggingEvent)
        {
            if (!_eventRateLimiter.Request(loggingEvent.TimeStamp))
            {
                LogLog.Debug(_declaringType, "Appending denied due to event limiter saturated.");
            }
            else
            {
                base.Append(loggingEvent);
            }

        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            var rs = events.SelectMany(e => _eventProcessor.ProcessEvent(e, RenderLoggingEvent(e)).Select(r => r));

            var requests = Assemble(rs);

            foreach (var putMetricDataRequest in requests)
                _client.AddSendMessageRequest(putMetricDataRequest);
        }

        private static IEnumerable<SendMessageBatchRequestWrapper> Assemble(IEnumerable<SQSDatum> data)
        {
            if (data.Any(x => x.Message.Length > 256*1024))
                throw new MessageTooLargeException();

            var requests = new List<SendMessageBatchRequestWrapper>();
            foreach (var grouping in data.GroupBy(r => r.QueueName))
            {
                var skip = 0;

                while (grouping.Skip(skip).Any())
                {
                    var size = 0;

                    var taken = grouping
                        .Skip(skip)
                        .TakeWhile(x => (size += x.Message.Length) < 256 * 1024)
                        .Take(10);

                    requests.Add(new SendMessageBatchRequestWrapper
                                 {
                                     QueueName = grouping.Key,
                                     Entries =
                                         taken
                                         .Select(
                                             sqsDatum => new SendMessageBatchRequestEntry
                                                  {
                                                      MessageBody = sqsDatum.Message,
                                                      Id = sqsDatum.ID
                                                  }
                                         ).ToList()
                                 });

                    skip += taken.Count();
                }
            }

            return requests;
        }
    }

    internal class MessageTooLargeException : Exception
    {
    }
}




