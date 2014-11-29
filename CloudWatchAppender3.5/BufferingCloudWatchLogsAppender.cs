using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.CloudWatch;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Runtime;
using CloudWatchAppender.Appenders;
using CloudWatchAppender.Layout;
using CloudWatchAppender.Model;
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
        private static readonly Type _declaringType = typeof (BufferingCloudWatchLogsAppender);
        private StandardUnit _standardUnit;
        private string _accessKey;
        private string _secret;
        private string _endPoint;
        private string _value;
        private string _metricName;
        private string _ns;
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

        public string AccessKey
        {
            set
            {
                _accessKey = value;
                _client = null;
            }
        }


        protected override void ResetClient()
        {
            _client = null;
        }

        public string Secret
        {
            set
            {
                _secret = value;
                _client = null;
            }
        }

        public string EndPoint
        {
            set
            {
                _endPoint = value;
                _client = null;
            }
        }


        public string GroupName
        {
            set
            {
                _groupName = value;
                _eventProcessor = null;
            }
        }

        public string StreamName
        {
            set
            {
                _streamName = value;
                _eventProcessor = null;
            }
        }

        public string Message
        {
            set
            {
                _message = value;
                _eventProcessor = null;
            }
        }

        public string Timestamp
        {
            set
            {
                _timestamp = value;
                _eventProcessor = null;
            }
        }

        public bool ConfigOverrides
        {
            set
            {
                _configOverrides = value;
                _eventProcessor = null;
            }
        }


        private string _instanceMetaDataReaderClass;

        public string InstanceMetaDataReaderClass
        {
            get { return _instanceMetaDataReaderClass; }
            set
            {
                _instanceMetaDataReaderClass = value;
                InstanceMetaDataReader.Instance =
                    Activator.CreateInstance(Type.GetType(value)) as IInstanceMetaDataReader;
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

            var hierarchy = ((Hierarchy) log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof (LogDatum), new LogDatumRenderer());


        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            try
            {
                _client = new CloudWatchLogsClientWrapper(_endPoint, _accessKey, _secret, _clientConfig);
            }
            catch (CloudWatchAppenderException)
            {
            }

            _eventProcessor = new LogEventProcessor(_configOverrides, _groupName, _streamName, _timestamp, _message);

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
                                         .OrderBy(x=>x.Timestamp)
                                         .Select(
                                             x => new InputLogEvent {Message = x.Message, Timestamp = x.Timestamp.Value})
                                         .ToList()
                                 });
                }
            }

            return requests;
        }
    }
}

            
            

