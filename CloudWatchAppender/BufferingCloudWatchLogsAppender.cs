using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Appenders;
using CloudWatchAppender.Layout;
using CloudWatchAppender.Model;
using CloudWatchAppender.Services;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using MetricDatum = Amazon.CloudWatch.Model.MetricDatum;

namespace CloudWatchAppender
{
    public class BufferingCloudWatchLogsAppender : BufferingAppenderSkeleton, ICloudWatchAppender
    {
        private CloudWatchClientWrapper _cloudWatchClient;
        private EventProcessor _eventProcessor;
        private readonly static Type _declaringType = typeof(BufferingCloudWatchLogsAppender);
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


        public BufferingCloudWatchLogsAppender()
        {
            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(MetricDatum), new MetricDatumRenderer());

            try
            {
                _cloudWatchClient = new CloudWatchClientWrapper(_endPoint, _accessKey, _secret);
            }
            catch (CloudWatchAppenderException)
            {
            }

            _eventProcessor = new EventProcessor(_configOverrides, _standardUnit, _ns, _metricName, _timestamp, _value, _dimensions);
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            if (_cloudWatchClient == null)
                _cloudWatchClient = new CloudWatchClientWrapper(_endPoint, _accessKey, _secret);

            if (_eventProcessor == null)
                _eventProcessor = new EventProcessor(_configOverrides, _standardUnit, _ns, _metricName, _timestamp, _value, _dimensions);

            if (Layout == null)
                Layout = new PatternLayout("%message");

            var rs = events.SelectMany(e => _eventProcessor.ProcessEvent(e, RenderLoggingEvent(e)).Select(r => r));

            var requests = Assemble(rs);

            foreach (var putMetricDataRequest in requests)
                _cloudWatchClient.QueuePutMetricData(putMetricDataRequest);
        }

        internal static IEnumerable<PutMetricDataRequest> Assemble(IEnumerable<PutMetricDataRequest> rs)
        {
            var requests = new List<PutMetricDataRequest>();

            foreach (var namespaceGrouping in rs.GroupBy(r => r.Namespace))
            {
                var metricData = new List<MetricDatum>();

                foreach (var metricNameGrouping in namespaceGrouping.SelectMany(x => x.MetricData).GroupBy(x => x.MetricName))
                {
                    var units = metricNameGrouping.Select(x => x.Unit).Distinct();
                    var unit = FindLeastUnit(units);

                    foreach (var dimensionGrouping in metricNameGrouping
                        .GroupBy(x => string.Join(";", x.Dimensions
                            .OrderBy(d => d.Name)
                            .Select(d => string.Format("{0}/{1}", d.Name, d.Value)))))
                    {
                        var timestamp = dimensionGrouping.Max(x => x.Timestamp);
                        metricData.Add(new MetricDatum
                                       {
                                           MetricName = metricNameGrouping.Key,
                                           Dimensions = dimensionGrouping.First().Dimensions,
                                           Timestamp = timestamp > DateTime.MinValue ? timestamp : DateTime.UtcNow,
                                           Unit = unit,
                                           StatisticValues = Aggregate(dimensionGrouping.AsEnumerable(), unit)
                                       });
                    }
                }

                var bin = metricData.Take(20);
                var i = 0;
                do
                {
                    var putMetricDataRequest = new PutMetricDataRequest
                                               {
                                                   Namespace = namespaceGrouping.Key
                                               };

                    putMetricDataRequest.MetricData.AddRange(bin);
                    requests.Add(putMetricDataRequest);
                    bin = metricData.Skip(i += 20).Take(20);

                } while (bin.Any());
            }

            return requests;
        }

        private static StatisticSet Aggregate(IEnumerable<MetricDatum> data, StandardUnit unit)
        {
            return new StatisticSet
                   {
                       Maximum = MakeStatistic(data, unit, d => d.Maximum).Max(),
                       Minimum = MakeStatistic(data, unit, d => d.Minimum).Min(),
                       Sum = MakeStatistic(data, unit, d => d.Sum).Sum(),
                       SampleCount = data.Select(d1 => d1.StatisticValues == null ? 1 : d1.StatisticValues.SampleCount).Sum()
                   };
        }

        private static IEnumerable<double> MakeStatistic(IEnumerable<MetricDatum> data, StandardUnit unit, Func<StatisticSet, double> func)
        {
            return data.Select(d => d.StatisticValues == null
                ? UnitConverter.Convert(d.Value).From(d.Unit).To(unit)
                : UnitConverter.Convert(func(d.StatisticValues)).From(d.Unit).To(unit));
        }


        private static StandardUnit FindLeastUnit(IEnumerable<StandardUnit> units)
        {
            var unit = units.First();
            if (units.Count() > 1)
                foreach (var standardUnit in units)
                {
                    if (UnitConverter.Convert(1).From(unit).To(standardUnit) > 1)
                        unit = standardUnit;
                }
            return unit;
        }







    }
}