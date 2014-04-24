using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Layout;
using CloudWatchAppender.Model;
using CloudWatchAppender.Services;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using MetricDatum = Amazon.CloudWatch.Model.MetricDatum;

[assembly: InternalsVisibleTo("CloudWatchAppender.Tests")]


namespace CloudWatchAppender
{
    public class BufferingAggregatingCloudWatchAppender : BufferingAppenderSkeleton
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

        public BufferingAggregatingCloudWatchAppender()
        {
            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(Amazon.CloudWatch.Model.MetricDatum), new MetricDatumRenderer());

        }


        protected override void SendBuffer(LoggingEvent[] events)
        {
            if (_client == null)
                _client = new ClientWrapper(EndPoint, AccessKey, Secret);

            if (_eventProcessor == null)
                _eventProcessor = new EventProcessor(ConfigOverrides, StandardUnit, Namespace, MetricName, Timestamp, Value);

            if (Layout == null)
                Layout = new PatternLayout("%message");

            var rs = events.SelectMany(e => _eventProcessor.ProcessEvent(e, RenderLoggingEvent(e)).Select(r => r));

            var requests = Assemble(rs);

            foreach (var putMetricDataRequest in requests)
                _client.SendItOff(putMetricDataRequest);
        }

        internal static List<PutMetricDataRequest> Assemble(IEnumerable<PutMetricDataRequest> rs)
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

                        metricData.Add(new MetricDatum
                                               {
                                                   MetricName = metricNameGrouping.Key,
                                                   Dimensions = dimensionGrouping.First().Dimensions,
                                                   Timestamp = DateTime.UtcNow,
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


        private Dictionary<string, Dimension> _parsedDimensions;
        private bool _parsedProperties;
        private StandardUnit _parsedUnit;
        private string _parsedNamespace;
        private string _defaultMetricName;
        private DateTimeOffset? _dateTimeOffset;
        private ClientWrapper _client;
        private EventProcessor _eventProcessor;
        private StandardUnit _standardUnit;


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

        private readonly static Type _declaringType = typeof(BufferingAggregatingCloudWatchAppender);
    }
}