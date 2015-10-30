using System;
using System.Linq;
using System.Reflection;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Runtime;
using CloudWatchAppender.Layout;
using CloudWatchAppender.Model;
using CloudWatchAppender.Services;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Util;

namespace CloudWatchAppender
{
    public class CloudWatchLogsAppender : CloudWatchAppenderBase<LogDatum>, ICloudWatchLogsAppender
    {
        private CloudWatchLogsClientWrapper _client;
        private readonly static Type _declaringType = typeof(CloudWatchLogsAppender);
        private string _message;
        private string _timestamp;

        private string _groupName;
        private string _streamName;

        private bool _configOverrides = true;

        private AmazonCloudWatchLogsConfig _clientConfig;

        protected override ClientConfig ClientConfig
        {
            get { return _clientConfig ?? (_clientConfig = new AmazonCloudWatchLogsConfig()); }
        }

        public override IEventProcessor<LogDatum> EventProcessor
        {
            get { return _eventProcessor; }
            set { _eventProcessor = value; }
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
            }
        }

        public string StreamName
        {
            set
            {
                _streamName = value;
            }
        }

        public string Message
        {
            set
            {
                _message = value;
            }
        }


        public new string Timestamp
        {
            set
            {
                _timestamp = value;
            }
        }

        private IEventProcessor<LogDatum> _eventProcessor;

        public CloudWatchLogsAppender()
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

            _client = new CloudWatchLogsClientWrapper(EndPoint, AccessKey, Secret, ClientConfig);

            _eventProcessor = new LogEventProcessor(_configOverrides, _groupName, _streamName, _timestamp, _message);

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

            var logDatum = _eventProcessor.ProcessEvent(loggingEvent, RenderLoggingEvent(loggingEvent)).Single();

            _client.AddLogRequest(new PutLogEventsRequest(logDatum.GroupName, logDatum.StreamName, new[] { new InputLogEvent
                                                                                                      {
                                                                                                          Timestamp = logDatum.Timestamp.Value.ToUniversalTime(),
                                                                                                          Message = logDatum.Message
                                                                                                      } }.ToList()));
        }


    }
}