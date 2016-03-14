using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Runtime;
using CloudWatchAppender.Appenders;
using CloudWatchAppender.Layout;
using CloudWatchAppender.Model;
using CloudWatchAppender.Parsers;
using CloudWatchAppender.Services;
using log4net.Core;
using log4net.Repository.Hierarchy;
using MetricDatum = Amazon.CloudWatch.Model.MetricDatum;

[assembly: InternalsVisibleTo("CloudWatchAppender.Tests")]


namespace CloudWatchAppender
{

    public class BufferingAggregatingCloudWatchAppender : BufferingCloudWatchAppenderBase<PutMetricDataRequest>, ICloudWatchAppender
    {
        private CloudWatchClientWrapper _client;
        private StandardUnit _standardUnit;
        private string _value;
        private string _metricName;
        private string _ns;
        private bool _configOverrides = true;
        private readonly Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();

        
        private AmazonCloudWatchConfig _clientConfig;
        private IEventProcessor<PutMetricDataRequest> _eventProcessor;

        protected override void ResetClient()
        {
            _client = null;
        }

        public override IEventProcessor<PutMetricDataRequest> EventProcessor
        {
            get { return _eventProcessor; }
            set { _eventProcessor = value; }
        }

        protected override ClientConfig ClientConfig
        {
            get { return _clientConfig ?? (_clientConfig = new AmazonCloudWatchConfig()); }
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


        public Dimension Dimension
        {
            set
            {
                _dimensions[value.Name] = value;
                _eventProcessor = null;
            }
        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();
            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(MetricDatum), new MetricDatumRenderer());

            EventMessageParser = EventMessageParser ?? new MetricDatumEventMessageParser(ConfigOverrides);

            try
            {
                _client = new CloudWatchClientWrapper(EndPoint, AccessKey, Secret, _clientConfig);
            }
            catch (CloudWatchAppenderException)
            {
            }

            _eventProcessor = new MetricDatumEventProcessor(_configOverrides, _standardUnit, _ns, _metricName, Timestamp, _value, _dimensions)
                              {EventMessageParser = EventMessageParser};

            if (Layout == null)
                Layout = new PatternLayout("%message");
        }



        protected override void SendBuffer(LoggingEvent[] events)
        {
            var rs = events.SelectMany(e => _eventProcessor.ProcessEvent(e, RenderLoggingEvent(e)).Select(r => r));

            var requests = Assemble(rs);

            foreach (var putMetricDataRequest in requests)
                _client.QueuePutMetricData(putMetricDataRequest);
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
                            .Select(d => string.Format("{0}/{1}", d.Name, d.Value)).ToArray())))
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