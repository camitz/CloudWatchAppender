using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Runtime;
using AWSAppender.Core.Layout;
using AWSAppender.Core.Services;
using CloudWatchAppender.Model;
using CloudWatchAppender.Parsers;
using CloudWatchAppender.Services;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Util;

namespace CloudWatchAppender
{
    public class BufferingCloudWatchLogsAppender : BufferingCloudWatchAppenderBase<LogDatum>,
        ICloudWatchLogsAppender
    {
        private EventRateLimiter _eventRateLimiter = new EventRateLimiter();
        private CloudWatchLogsClientWrapper _client;
        private static readonly Type _declaringType = typeof(BufferingCloudWatchLogsAppender);
        private string _timestamp;

        private string _groupName;
        private string _streamName;
        private string _message;

        private IEventProcessor<LogDatum> _eventProcessor;

        private bool _configOverrides = true;

        private AmazonCloudWatchLogsConfig _clientConfig;

        public override IEventProcessor<LogDatum> EventProcessor
        {
            get { return _eventProcessor; }
            set { _eventProcessor = value; }
        }

        protected override ClientConfig ClientConfig
        {
            get { return _clientConfig ?? (_clientConfig = new AmazonCloudWatchLogsConfig()); }
        }

        protected override void ResetClient()
        {
            _client = null;
        }

        public string GroupName
        {
            set
            {
                _groupName = value;
                _eventProcessor = null;
            }
            get { return _groupName; }
        }

        public string StreamName
        {
            set
            {
                _streamName = value;
                _eventProcessor = null;
            }
            get { return _streamName; }
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

        public new string Timestamp
        {
            set
            {
                _timestamp = value;
                _eventProcessor = null;
            }
            get { return _timestamp; }
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

        public BufferingCloudWatchLogsAppender()
        {
            if (Assembly.GetEntryAssembly() != null)
                _groupName = Assembly.GetEntryAssembly().GetName().Name;
            else
                _groupName = "unspecified";

            _streamName = "unspecified";

            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(LogDatum), new LogDatumRenderer());


        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            EventMessageParser = EventMessageParser ?? new LogsEventMessageParser(_configOverrides);

            _client = new CloudWatchLogsClientWrapper(EndPoint, AccessKey, Secret, ClientConfig);

            _eventProcessor = new LogEventProcessor(_groupName, _streamName, _timestamp, _message)
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
                _client.AddLogRequest(putMetricDataRequest);
        }

        private static IEnumerable<PutLogEventsRequest> Assemble(IEnumerable<LogDatum> rs)
        {
            var requests = new List<PutLogEventsRequest>();
            foreach (var grouping0 in rs.GroupBy(r => r.GroupName))
            {

                foreach (var grouping1 in grouping0.GroupBy(x => x.StreamName))
                {

                    requests.Add(new PutLogEventsRequest
                                 {
                                     LogGroupName = grouping0.Key,
                                     LogStreamName = grouping1.Key,
                                     LogEvents =
                                         grouping1
                                         .OrderBy(x => x.Timestamp)
                                         .Select(
                                             x => new InputLogEvent { Message = x.Message, Timestamp = x.Timestamp.Value })
                                         .ToList()
                                 });
                }
            }

            return requests;
        }
    }
}




