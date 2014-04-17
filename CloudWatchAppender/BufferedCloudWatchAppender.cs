using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace CloudWatchAppender
{
    public class BufferingCloudWatchAppender : BufferingAppenderSkeleton
    {
        public string AccessKey { get; set; }
        public string Secret { get; set; }
        public string EndPoint { get; set; }

        public string Unit { get; set; }
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

        private Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();

        private void AddDimension(Dimension value)
        {
            _dimensions[value.Name] = value;
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


        private static ConcurrentDictionary<int, Task> _tasks = new ConcurrentDictionary<int, Task>();

        public BufferingCloudWatchAppender()
        {
            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(Amazon.CloudWatch.Model.MetricDatum), new MetricDatumRenderer());
            _client = new ClientWrapper(EndPoint,AccessKey,Secret);
        }


        public static bool HasPendingRequests
        {
            get { return _tasks.Values.Any(t => !t.IsCompleted); }
        }

        public ClientWrapper Client
        {
            get { return _client; }
        }


        public static void WaitForPendingRequests(TimeSpan timeout)
        {
            var startedTime = DateTime.Now;
            var timeConsumed = TimeSpan.Zero;
            while (HasPendingRequests && timeConsumed < timeout)
            {
                Task.WaitAll(_tasks.Values.ToArray(), timeout - timeConsumed);
                timeConsumed = DateTime.Now - startedTime;
            }
        }

        public static void WaitForPendingRequests()
        {
            while (HasPendingRequests)
                Task.WaitAll(_tasks.Values.ToArray());
        }

       

        protected override void SendBuffer(LoggingEvent[] events)
        {
            throw new NotImplementedException();
        }
        

        private void ParseProperties(PatternParser patternParser)
        {
            if (!_parsedProperties)
            {
                _parsedDimensions = !_dimensions.Any() ? null :
                                                                  _dimensions
                                                                      .Select(x => new Dimension { Name = x.Key, Value = patternParser.Parse(x.Value.Value) }).
                                                                      ToDictionary(x => x.Name, y => y);

                _parsedUnit = String.IsNullOrEmpty(Unit)
                                  ? null
                                  : patternParser.Parse(Unit);

                _parsedNamespace = string.IsNullOrEmpty(Namespace)
                                       ? null
                                       : patternParser.Parse(Namespace);

                _defaultMetricName = string.IsNullOrEmpty(MetricName)
                                         ? null
                                         : patternParser.Parse(MetricName);

                _dateTimeOffset = string.IsNullOrEmpty(Timestamp)
                                      ? null
                                      : (DateTimeOffset?)DateTimeOffset.Parse(patternParser.Parse(Timestamp));

                _parsedProperties = true;
            }
        }

        private Dictionary<string, Dimension> _parsedDimensions;
        private bool _parsedProperties;
        private string _parsedUnit;
        private string _parsedNamespace;
        private string _defaultMetricName;
        private DateTimeOffset? _dateTimeOffset;
        private ClientWrapper _client;

    }
}