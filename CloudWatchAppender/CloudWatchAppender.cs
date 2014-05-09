using System;
using System.Collections.Generic;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
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
    public interface ICloudWatchAppender
    {
        string AccessKey { set; }
        string Secret { set; }
        string EndPoint { set; }
        string Unit { set; }
        StandardUnit StandardUnit { set; }
        string Value { set; }
        string MetricName { set; }
        string Namespace { get; set; }
        string Timestamp { set; }
        Dimension Dimension { set; }
        bool ConfigOverrides { set; }
        string InstanceMetaDataReaderClass { get; set; }
    }

    public class CloudWatchAppender : AppenderSkeleton, ICloudWatchAppender
    {
        private EventRateLimiter _eventRateLimiter = new EventRateLimiter();
        private ClientWrapper _client;
        private EventProcessor _eventProcessor;
        private readonly static Type _declaringType = typeof(CloudWatchAppender);
        private StandardUnit _standardUnit;
        private string _accessKey;
        private string _secret;
        private string _endPoint;
        private string _value;
        private string _metricName;
        private string _ns;
        private string _timestamp;
        private bool _configOverrides = true;
        private readonly Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();

        public string AccessKey
        {
            set { 
                _accessKey = value;
                _client = null;
            }
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

        public Dimension Dimension
        {
            set
            {
                _dimensions[value.Name] = value;
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

        public CloudWatchAppender()
        {
            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(Amazon.CloudWatch.Model.MetricDatum), new MetricDatumRenderer());
            try
            {
                _client = new ClientWrapper(_endPoint, _accessKey, _secret);
            }
            catch (CloudWatchAppenderException)
            {
            }

            _eventProcessor = new EventProcessor(_configOverrides, _standardUnit, _ns, _metricName, _timestamp, _value, _dimensions);

            if (Layout == null)
                Layout = new PatternLayout("%message");

        }


        public static bool HasPendingRequests
        {
            get { return ClientWrapper.HasPendingRequests; }
        }

        public static void WaitForPendingRequests(TimeSpan timeout)
        {
            ClientWrapper.WaitForPendingRequests(timeout);
        }

        public static void WaitForPendingRequests()
        {
            ClientWrapper.WaitForPendingRequests();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (_client == null)
                _client = new ClientWrapper(_endPoint,_accessKey,_secret);

            if (_eventProcessor == null)
                _eventProcessor = new EventProcessor(_configOverrides, _standardUnit, _ns, _metricName, _timestamp, _value, _dimensions);

            if (Layout == null)
                Layout = new PatternLayout("%message");

            LogLog.Debug(_declaringType, "Appending");

            if (!_eventRateLimiter.Request(loggingEvent.TimeStamp))
            {
                LogLog.Debug(_declaringType, "Appending denied due to event limiter saturated.");
                return;
            }

            var metricDataRequests =_eventProcessor.ProcessEvent(loggingEvent, RenderLoggingEvent(loggingEvent));

            foreach (var putMetricDataRequest in metricDataRequests)
                _client.SendItOff(putMetricDataRequest);
        }

    }
}