using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Parsers;
using log4net.Core;
using log4net.Util;

namespace CloudWatchAppender.Services
{
#if NET35
    public interface IEventProcessor<T>
#else
    public interface IEventProcessor<out T>
#endif
    {
        IEnumerable<T> ProcessEvent(LoggingEvent loggingEvent, string renderedString);
    }

    public class MetricDatumEventProcessor : IEventProcessor<PutMetricDataRequest>
    {
        private Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();
        private Dictionary<string, Dimension> _parsedDimensions;
        private bool _hasParsedProperties;
        private string _parsedNamespace;
        private string _parsedMetricName;
        private DateTimeOffset? _dateTimeOffset;
        private MetricDatumEventMessageParser _metricDatumEventMessageParser;
        private readonly bool _configOverrides;
        private readonly StandardUnit _unit;
        private readonly string _namespace;
        private readonly string _metricName;
        private readonly string _timestamp;
        private readonly string _value;

        public MetricDatumEventProcessor(bool configOverrides, StandardUnit unit, string @namespace, string metricName, string timestamp, string value, Dictionary<string, Dimension> dimensions)
        {
            _configOverrides = configOverrides;
            _unit = unit;
            _namespace = @namespace;
            _metricName = metricName;
            _timestamp = timestamp;
            _value = value;
            _dimensions = dimensions;
        }



        public IEnumerable<PutMetricDataRequest> ProcessEvent(LoggingEvent loggingEvent, string renderedString)
        {
            var patternParser = new PatternParser(loggingEvent);

            if (renderedString.Contains("%"))
                renderedString = patternParser.Parse(renderedString);

            LogLog.Debug(_declaringType, string.Format("RenderedString: {0}", renderedString));

            if (!_hasParsedProperties)
            {
                ParseProperties(patternParser);
                _hasParsedProperties = true;
            }

            _metricDatumEventMessageParser = new MetricDatumEventMessageParser(renderedString, _configOverrides)
                         {
                             DefaultMetricName = _parsedMetricName,
                             DefaultNameSpace = _parsedNamespace,
                             DefaultUnit = _unit,
                             DefaultDimensions = _parsedDimensions,
                             DefaultTimestamp = _dateTimeOffset
                         };

            if (!string.IsNullOrEmpty(_value) && _configOverrides)
                _metricDatumEventMessageParser.DefaultValue = Double.Parse(_value, CultureInfo.InvariantCulture);

            _metricDatumEventMessageParser.Parse();

            return _metricDatumEventMessageParser.GetParsedData();
        }

        private void ParseProperties(PatternParser patternParser)
        {
            _parsedDimensions = !_dimensions.Any()
                ? null
                : _dimensions
                    .Select(x => new Dimension { Name = x.Key, Value = patternParser.Parse(x.Value.Value) }).
                    ToDictionary(x => x.Name, y => y);

            _parsedNamespace = string.IsNullOrEmpty(_namespace)
                ? null
                : patternParser.Parse(_namespace);

            _parsedMetricName = string.IsNullOrEmpty(_metricName)
                ? null
                : patternParser.Parse(_metricName);

            _dateTimeOffset = string.IsNullOrEmpty(_timestamp)
                ? null
                : (DateTimeOffset?)DateTimeOffset.Parse(patternParser.Parse(_timestamp));
        }

        private readonly static Type _declaringType = typeof(MetricDatumEventProcessor);
    }
}