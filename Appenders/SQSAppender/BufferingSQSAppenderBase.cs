//to core?

//using Amazon.CloudWatch;
using System;
using System.Net;
using Amazon;
using Amazon.Runtime;
using AWSAppender.Core.Services;
using log4net.Appender;
using SQSAppender.Parsers;
using SQSAppender.Services;
using SQSAppender.TypeConverters;

namespace SQSAppender
{
    public abstract class BufferingSQSAppenderBase<T> : BufferingAppenderSkeleton, IAWSAppender
    {
        protected BufferingSQSAppenderBase()
        {
            log4net.Util.TypeConverters.ConverterRegistry.AddConverter(typeof(RegionEndpoint), typeof(RegionConverter));
            //log4net.Util.TypeConverters.ConverterRegistry.AddConverter(typeof(StandardUnit), typeof(StandardUnitConverter));                         //cloudwatch specific

        }

        public string AccessKey
        {
            set
            {
                _accessKey = value;
                ResetClient();
            }
            get { return _accessKey; }
        }

        protected abstract void ResetClient();

        public string Secret
        {
            set
            {
                _secret = value;
                ResetClient();
            }
            get { return _secret; }
        }

        public string EndPoint
        {
            set
            {
                _endPoint = value;
                ResetClient();
            }
            get { return _endPoint; }
        }

        public IEventMessageParser<T> EventMessageParser
        {
            set
            {
                _eventMessageParser = value;
                ResetClient();
            }
            get { return _eventMessageParser; }
        }

        public bool ConfigOverrides
        {
            set
            {
                _configOverrides = value;
                EventProcessor = null;
            }
            get { return _configOverrides; }
        }


        private string _instanceMetaDataReaderClass;
        private string _accessKey;
        private string _secret;
        private string _endPoint;
        private IEventMessageParser<T> _eventMessageParser;
        private bool _configOverrides;
        private string _timestamp;
        private EventRateLimiter _eventRateLimiter;

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

        public string Timestamp
        {
            set
            {
                _timestamp = value;
                ResetClient();
            }
            get { return _timestamp; }
        }

        public EventRateLimiter EventRateLimiter
        {
            get { return _eventRateLimiter ?? (_eventRateLimiter = new EventRateLimiter()); }
            set { _eventRateLimiter = value; }
        }

        public abstract IEventProcessor<T> EventProcessor { get; set; }

        #region ClientConfig

        protected abstract ClientConfig ClientConfig { get; }

        public string ProxyHost { get { return ClientConfig.ProxyHost; } set { ClientConfig.ProxyHost = value; } }
        public int ProxyPort { get { return ClientConfig.ProxyPort; } set { ClientConfig.ProxyPort = value; } }
        public int MaxIdleTime { get { return ClientConfig.MaxIdleTime; } set { ClientConfig.MaxIdleTime = value; } }
        public int ConnectionLimit { get { return ClientConfig.ConnectionLimit; } set { ClientConfig.ConnectionLimit = value; } }
        public bool UseNagleAlgorithm { get { return ClientConfig.UseNagleAlgorithm; } set { ClientConfig.UseNagleAlgorithm = value; } }
        public TimeSpan? ReadWriteTimeout { get { return ClientConfig.ReadWriteTimeout; } set { ClientConfig.ReadWriteTimeout = value; } }
        //public abstract string ServiceVersion { get; }
        public SigningAlgorithm SignatureMethod { set { ClientConfig.SignatureMethod = value; } }

        public string SignatureVersion { get { return ClientConfig.SignatureVersion; } set { ClientConfig.SignatureVersion = value; } }
        public string UserAgent { get { return ClientConfig.UserAgent; } }
        public RegionEndpoint RegionEndpoint { set { ClientConfig.RegionEndpoint = value; } }
        public string ServiceURL { get { return ClientConfig.ServiceURL; } set { ClientConfig.ServiceURL = value; } }
        public bool UseHttp { get { return ClientConfig.UseHttp; } set { ClientConfig.UseHttp = value; } }
        public string AuthenticationRegion { get { return ClientConfig.AuthenticationRegion; } set { ClientConfig.AuthenticationRegion = value; } }
        public string AuthenticationServiceName { get { return ClientConfig.AuthenticationServiceName; } set { ClientConfig.AuthenticationServiceName = value; } }
        public int MaxErrorRetry { get { return ClientConfig.MaxErrorRetry; } set { ClientConfig.MaxErrorRetry = value; } }
        public bool LogResponse { get { return ClientConfig.LogResponse; } set { ClientConfig.LogResponse = value; } }
        public bool ReadEntireResponse { get { return ClientConfig.ReadEntireResponse; } set { ClientConfig.ReadEntireResponse = value; } }
        public int AWSBufferSize { get { return ClientConfig.BufferSize; } set { ClientConfig.BufferSize = value; } }
        public long ProgressUpdateInterval { get { return ClientConfig.ProgressUpdateInterval; } set { ClientConfig.ProgressUpdateInterval = value; } }
        public bool LogMetrics { get { return ClientConfig.LogMetrics; } set { ClientConfig.LogMetrics = value; } }
        public ICredentials ProxyCredentials { get { return ClientConfig.ProxyCredentials; } set { ClientConfig.ProxyCredentials = value; } }
        public TimeSpan? Timeout { get { return ClientConfig.Timeout; } set { ClientConfig.Timeout = value; } }


        #endregion
    }
}