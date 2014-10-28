using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.CloudWatchLogs.Model;
using CloudWatchAppender.Appenders;
using CloudWatchAppender.Layout;
using CloudWatchAppender.Model;
using CloudWatchAppender.Services;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Util;

namespace CloudWatchAppender
{
    public class CloudWatchLogsAppender : AppenderSkeleton, ICloudWatchLogsAppender
    {
        private EventRateLimiter _eventRateLimiter = new EventRateLimiter();
        private CloudWatchLogsClientWrapper _cloudWatchClient;
        private EventProcessor _eventProcessor;
        private readonly static Type _declaringType = typeof(CloudWatchLogsAppender);
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

        private bool _configOverrides = true;

        public string AccessKey
        {
            set
            {
                _accessKey = value;
                _cloudWatchClient = null;
            }
        }

        public string Secret
        {
            set
            {
                _secret = value;
                _cloudWatchClient = null;
            }
        }

        public string EndPoint
        {
            set
            {
                _endPoint = value;
                _cloudWatchClient = null;
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

        public string Unit
        {
            set
            {
                _standardUnit = value;
                _eventProcessor = null;
            }
        }

        public StandardUnit StandardUnit
        {
            set
            {
                _standardUnit = value;
                _eventProcessor = null;
            }
        }

        public string Value
        {
            set
            {
                _value = value;
                _eventProcessor = null;
            }
        }

        public string MetricName
        {
            set
            {
                _metricName = value;
                _eventProcessor = null;
            }
        }

        public string Namespace
        {
            get { return _ns; }
            set
            {
                _ns = value;
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

        public CloudWatchLogsAppender()
        {
            if (Assembly.GetEntryAssembly() != null)
                _groupName = Assembly.GetEntryAssembly().GetName().Name;
            else
                _groupName = "unspecified";

            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(Amazon.CloudWatch.Model.MetricDatum), new MetricDatumRenderer());
            try
            {
                _cloudWatchClient = new CloudWatchLogsClientWrapper(_endPoint, _accessKey, _secret);
            }
            catch (CloudWatchAppenderException)
            {
            }

            //_eventProcessor = new EventProcessor(_configOverrides, _standardUnit, _ns, _metricName, _timestamp, _value, _dimensions);

            if (Layout == null)
                Layout = new PatternLayout("%message");

        }


        public static bool HasPendingRequests
        {
            get { return CloudWatchClientWrapper.HasPendingRequests; }
        }

        public static void WaitForPendingRequests(TimeSpan timeout)
        {
            CloudWatchClientWrapper.WaitForPendingRequests(timeout);
        }

        public static void WaitForPendingRequests()
        {
            CloudWatchClientWrapper.WaitForPendingRequests();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (_cloudWatchClient == null)
                _cloudWatchClient = new CloudWatchLogsClientWrapper(_endPoint, _accessKey, _secret);

            if (Layout == null)
                Layout = new PatternLayout("%message");

            LogLog.Debug(_declaringType, "Appending");

            if (!_eventRateLimiter.Request(loggingEvent.TimeStamp))
            {
                LogLog.Debug(_declaringType, "Appending denied due to event limiter saturated.");
                return;
            }

            var t = new InputLogEvent
                    {
                        Timestamp = loggingEvent.TimeStamp.ToUniversalTime(),
                        Message = loggingEvent.RenderedMessage
                    };

            var t2 = new PutLogEventsRequest(_groupName, "trunk", new[] { t }.ToList());
            _cloudWatchClient.QueuePutLogRequest(t2);
        }

    }
}