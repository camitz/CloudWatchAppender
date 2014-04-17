using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Layout;
using CloudWatchAppender.Model;
using CloudWatchAppender.Services;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace CloudWatchAppender
{
    public class BufferingAggregatingCloudWatchAppender : BufferingAppenderSkeleton
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
                _eventProcessor = new EventProcessor(ConfigOverrides, Unit, Namespace, MetricName, Timestamp, Value);

            if (Layout == null)
                Layout = new PatternLayout("%message");

            var rs = events.SelectMany(e => _eventProcessor.ProcessEvent(e, RenderLoggingEvent(e)).Select(r => r));

            var requests = Assemble(rs);

            foreach (var putMetricDataRequest in requests)
                _client.SendItOff(putMetricDataRequest);
        }

        private static List<PutMetricDataRequest> Assemble(IEnumerable<PutMetricDataRequest> rs)
        {
            var requests = new List<PutMetricDataRequest>();

            foreach (var namespaceGrouping in rs.GroupBy(r => r.Namespace))
            {
                var request = new PutMetricDataRequest
                              {
                                  Namespace = namespaceGrouping.Key
                              };

                requests.Add(request);

                foreach (var metricNameGrouping in namespaceGrouping.SelectMany(x => x.MetricData).GroupBy(x => x.MetricName))
                {
                    foreach (var dimensionGrouping in metricNameGrouping
                        .GroupBy(x => string.Join(";", x.Dimensions
                            .OrderBy(d => d.Name)
                            .Select(d => string.Format("{0}/{1}", d.Name, d.Value)))))
                    {
                        var unit = dimensionGrouping.First().Unit;

                        var t = dimensionGrouping.Where(x => x.Unit == unit);

                        if (t.Count() > dimensionGrouping.Count())
                            foreach (var source in dimensionGrouping.Except(t))
                            {
                                System.Diagnostics.Debug.WriteLine(string.Format("Dropping a datum, expected unit {0}, was {4}. ({1};{2};{3})",
                                    unit, namespaceGrouping.Key, metricNameGrouping.Key, dimensionGrouping.Key, source.Unit));
                            }

                        var t2 = t.Where(x => x.StatisticValues == null);
                        var t3 = t.Where(x => x.StatisticValues != null);

                        request.MetricData.Add(new Amazon.CloudWatch.Model.MetricDatum
                                               {
                                                   MetricName = metricNameGrouping.Key,
                                                   Dimensions = t2.First().Dimensions,
                                                   Timestamp = DateTime.UtcNow,
                                                   Unit = unit,
                                                   StatisticValues = new StatisticSet
                                                                     {
                                                                         Maximum = t2.Select(x => x.Value).Union(t3.Select(x => x.StatisticValues.Maximum)).Max(),
                                                                         Minimum = t2.Select(x => x.Value).Union(t3.Select(x => x.StatisticValues.Minimum)).Min(),
                                                                         Sum = t2.Select(x => x.Value).Union(t3.Select(x => x.StatisticValues.Sum)).Sum(),
                                                                         SampleCount = t2.Count() + (t3.Select(x => x.StatisticValues.SampleCount)).Sum()
                                                                     }
                                               });


                    }
                }
            }

            return requests;
        }


        private Dictionary<string, Dimension> _parsedDimensions;
        private bool _parsedProperties;
        private string _parsedUnit;
        private string _parsedNamespace;
        private string _defaultMetricName;
        private DateTimeOffset? _dateTimeOffset;
        private ClientWrapper _client;
        private EventProcessor _eventProcessor;


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
    }
}