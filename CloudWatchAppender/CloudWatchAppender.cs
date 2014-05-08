using System;
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
    public class CloudWatchAppender : AppenderSkeleton
    {
        public string AccessKey { get; set; }
        public string Secret { get; set; }
        public string EndPoint { get; set; }

        public string Unit
        {
            set { _standardUnit = value; }
        }

        public StandardUnit StandardUnit
        {
            get { return _standardUnit; }
            set { _standardUnit = value; }
        }
        
        public string Value { get; set; }
        public string MetricName { get; set; }
        public string Namespace { get; set; }
        public string Timestamp { get; set; }

        public Dimension Dimension { set { AddDimension(value); } }

        private bool _configOverrides = true;
        public bool ConfigOverrides
        {
            get { return _configOverrides; }
            set { _configOverrides = value; }
        }

        private void AddDimension(Dimension value)
        {
            _eventProcessor.Dimensions[value.Name] = value;
        }

        public int RateLimit
        {
            set { _eventRateLimiter = new EventRateLimiter(value); }
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


        public CloudWatchAppender()
        {
            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(Amazon.CloudWatch.Model.MetricDatum), new MetricDatumRenderer());
            try
            {
                _client = new ClientWrapper(EndPoint, AccessKey, Secret);
            }
            catch (CloudWatchAppenderException)
            {
            }

            _eventProcessor = new EventProcessor(ConfigOverrides, StandardUnit, Namespace, MetricName, Timestamp, Value);

            if (Layout == null)
                Layout = new PatternLayout("%message");

        }


        public static bool HasPendingRequests
        {
            get { return ClientWrapper.HasPendingRequests; }
        }

        public ClientWrapper Client
        {
            set { _client = value; }
            get { return _client; }
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
                _client = new ClientWrapper(EndPoint, AccessKey, Secret);

            if (_eventProcessor == null)
                _eventProcessor = new EventProcessor(ConfigOverrides, StandardUnit, Namespace, MetricName, Timestamp, Value);

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

        private EventRateLimiter _eventRateLimiter = new EventRateLimiter();
        private ClientWrapper _client;
        private EventProcessor _eventProcessor;
        private readonly static Type _declaringType = typeof(CloudWatchAppender);
        private StandardUnit _standardUnit;
    }
}