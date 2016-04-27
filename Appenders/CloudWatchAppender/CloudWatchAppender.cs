using System;
using System.Collections.Generic;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Runtime;
using AWSAppender.Core.Layout;
using CloudWatchAppender.Model;
using CloudWatchAppender.Parsers;
using CloudWatchAppender.Services;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Util;

namespace CloudWatchAppender
{

    public class CloudWatchAppender : CloudWatchAppenderBase<PutMetricDataRequest>, ICloudWatchAppender
    {
        private CloudWatchClientWrapper _client;
        private readonly static Type _declaringType = typeof(CloudWatchAppender);
        private StandardUnit _standardUnit;
        private string _value;
        private string _metricName;
        private string _ns;
        private readonly Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();


        protected override void ResetClient()
        {
            _client = null;
        }

        private AmazonCloudWatchConfig _clientConfig;
        private IEventProcessor<PutMetricDataRequest> _eventProcessor1;

        protected override ClientConfig ClientConfig
        {
            get { return _clientConfig ?? (_clientConfig = new AmazonCloudWatchConfig()); }
        }

        public override IEventProcessor<PutMetricDataRequest> EventProcessor
        {
            get { return _eventProcessor1; }
            set { _eventProcessor1 = value; }
        }


        public string Unit
        {
            set
            {
                _standardUnit = value;
                EventProcessor = null;
            }
        }

        public StandardUnit StandardUnit
        {
            set
            {
                _standardUnit = value;
                EventProcessor = null;
            }
        }

        public string Value
        {
            set
            {
                _value = value;
                EventProcessor = null;
            }
        }

        public string MetricName
        {
            set
            {
                _metricName = value;
                EventProcessor = null;
            }
        }

        public string Namespace
        {
            get { return _ns; }
            set
            {
                _ns = value;
                EventProcessor = null;
            }
        }


        public Dimension Dimension
        {
            set
            {
                _dimensions[value.Name] = value;
                EventProcessor = null;
            }
        }




        public CloudWatchAppender()
        {
            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(Amazon.CloudWatch.Model.MetricDatum), new MetricDatumRenderer());
        }

        public override void ActivateOptions()
        {
            EventMessageParser = EventMessageParser ?? new MetricDatumEventMessageParser(ConfigOverrides);
            
            _client = new CloudWatchClientWrapper(EndPoint, AccessKey, Secret, _clientConfig);

            MetricDatumEventProcessor = new MetricDatumEventProcessor(ConfigOverrides, _standardUnit, _ns, _metricName, Timestamp, _value, _dimensions);

            if (Layout == null)
                Layout = new PatternLayout("%message");

            base.ActivateOptions();
        }

        public MetricDatumEventProcessor MetricDatumEventProcessor
        {
            get { return EventProcessor as MetricDatumEventProcessor; }
            set { EventProcessor = value; }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {

            LogLog.Debug(_declaringType, "Appending");

            if (!EventRateLimiter.Request(loggingEvent.TimeStamp))
            {
                LogLog.Debug(_declaringType, "Appending denied due to event limiter saturated.");
                return;
            }

            var metricDataRequests = MetricDatumEventProcessor.ProcessEvent(loggingEvent, RenderLoggingEvent(loggingEvent));

            foreach (var putMetricDataRequest in metricDataRequests)
                _client.QueuePutMetricData(putMetricDataRequest);
        }

    }

}