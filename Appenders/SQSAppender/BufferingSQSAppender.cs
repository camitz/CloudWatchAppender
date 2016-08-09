using System;
using System.Collections.Generic;
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
    public class BufferingSQSAppender : BufferingAWSAppenderBase<SQSDatum>, ISQSAppender
    {
        private EventRateLimiter _eventRateLimiter = new EventRateLimiter();
        private SQSClientWrapper _client;
        private static readonly Type _declaringType = typeof(BufferingSQSAppender);

        private string _queueName;
        private string _message;

        private IEventProcessor<SQSDatum> _eventProcessor;

        private bool _configOverrides = true;

        private AmazonSQSConfig _clientConfig;
        private string _delaySeconds;
        private static string _fallbackQueueName;
        private static Regex _queueNameRegex;

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

        public string DelaySeconds
        {
            set
            {
                _delaySeconds = value;
                _eventProcessor = null;
            }
            get { return _delaySeconds; }
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

            EventMessageParser = EventMessageParser ?? new SQSMessageParser(_configOverrides);

            _client = new SQSClientWrapper(EndPoint, AccessKey, Secret, ClientConfig);

            _eventProcessor = new SQSEventProcessor(_queueName, _message, _delaySeconds)
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

        protected virtual IEnumerable<SQSDatum> ProcessEvents(LoggingEvent[] events)
        {
            return events.SelectMany(e => _eventProcessor.ProcessEvent(e, RenderLoggingEvent(e)).Select(r => r));
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            var rs = ProcessEvents(events);

            var requests = Assemble(rs);

            foreach (var putMetricDataRequest in requests)
                _client.AddSendMessageRequest(putMetricDataRequest);

            if (rs.Any(x => System.Text.UTF8Encoding.UTF8.GetByteCount(x.Message) > 256 * 1024))
                throw new MessageTooLargeException(rs.First(x => System.Text.UTF8Encoding.UTF8.GetByteCount(x.Message) > 256 * 1024).Message);

            if (rs.Any(x => x.QueueName != null && !_queueNameRegex.IsMatch(x.QueueName)))
                throw new MessageTooLargeException(rs.First(x => System.Text.UTF8Encoding.UTF8.GetByteCount(x.Message) > 256 * 1024).Message);
        }


        private static IEnumerable<SendMessageBatchRequestWrapper> Assemble(IEnumerable<SQSDatum> data)
        {

            var requests = new List<SendMessageBatchRequestWrapper>();
            foreach (var grouping in data
                .Where(x => System.Text.UTF8Encoding.UTF8.GetByteCount(x.Message) <= 256 * 1024)
                .Where(x => x.QueueName == null || _queueNameRegex.IsMatch(x.QueueName))
                .GroupBy(r => r.QueueName ?? _fallbackQueueName))
            {
                var skip = 0;

                while (grouping.Skip(skip).Any())
                {
                    var size = 0;

                    var taken = grouping
                        .Skip(skip)
                        .TakeWhile(x => (size += System.Text.UTF8Encoding.UTF8.GetByteCount(x.Message)) < 256 * 1024)
                        .Take(10);

                    requests.Add(new SendMessageBatchRequestWrapper
                                 {
                                     QueueName = grouping.Key,
                                     Entries =
                                         taken
                                         .Select(
                                             sqsDatum =>
                                             {
                                                 var t = new SendMessageBatchRequestEntry
                                                        {
                                                            MessageBody = sqsDatum.Message,
                                                            Id = sqsDatum.ID
                                                        };

                                                 if (sqsDatum.DelaySeconds.HasValue)
                                                     t.DelaySeconds = sqsDatum.DelaySeconds.Value;
                                                 return t;
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
        public MessageTooLargeException(string message)
            : base(message)
        {
        }
    }
    internal class MalformedQueueName : Exception
    {
        public MalformedQueueName(string message)
            : base("Queuname must be 1-80 alphanumeric characters, hyphen or underscore: " + message)
        {
        }
    }
}




