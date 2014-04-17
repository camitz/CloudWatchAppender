using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Amazon.CloudWatch.Model;
using log4net.Core;
using log4net.Layout;

namespace CloudWatchAppender
{
    public class EventProcessor
    {
        private CloudWatchAppender _cloudWatchAppender;
        private Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();
        private Dictionary<string, Dimension> _parsedDimensions;
        private bool _hasParsedProperties;
        private string _parsedUnit;
        private string _parsedNamespace;
        private string _defaultMetricName;
        private DateTimeOffset? _dateTimeOffset;
        private EventMessageParser _eventMessageParser;
        private ILayout _layout;
        private readonly bool _configOverrides;
        private readonly string _unit;
        private readonly string _namespace;
        private readonly string _metricName;
        private readonly string _timestamp;
        private readonly string _value;

        public EventProcessor(bool configOverrides, string unit, string @namespace, string metricName, string timestamp, string value)
        {
            _configOverrides = configOverrides;
            _unit = unit;
            _namespace = @namespace;
            _metricName = metricName;
            _timestamp = timestamp;
            _value = value;
        }

        public Dictionary<string, Dimension> Dimensions
        {
            set { _dimensions = value; }
            get { return _dimensions; }
        }

        public IEnumerable<PutMetricDataRequest> GetMetricDataRequests()
        {
            return _eventMessageParser.GetMetricDataRequests();
        }

        public EventMessageParser ProcessEvent(LoggingEvent loggingEvent, string renderedString)
        {


            var patternParser = new PatternParser(loggingEvent);

            if (renderedString.Contains("%"))
                renderedString = patternParser.Parse(renderedString);

            System.Diagnostics.Debug.WriteLine(string.Format("RenderedString: {0}", renderedString));

            if (!_hasParsedProperties)
            {
                ParProperties(patternParser);
                _hasParsedProperties = true;
            }


            
            _eventMessageParser = new EventMessageParser(renderedString, _configOverrides)
                         {
                             DefaultMetricName = _defaultMetricName,
                             DefaultNameSpace = _parsedNamespace,
                             DefaultUnit = _parsedUnit,
                             DefaultDimensions = _parsedDimensions,
                             DefaultTimestamp = _dateTimeOffset
                         };

            if (!string.IsNullOrEmpty(_value) && _configOverrides)
                _eventMessageParser.DefaultValue = Double.Parse(_value, CultureInfo.InvariantCulture);

            _eventMessageParser.Parse();
            return _eventMessageParser;
        }

        private void ParProperties(PatternParser patternParser)
        {
            _parsedDimensions = !_dimensions.Any()
                ? null
                : _dimensions
                    .Select(x => new Dimension {Name = x.Key, Value = patternParser.Parse(x.Value.Value)}).
                    ToDictionary(x => x.Name, y => y);

            _parsedUnit = String.IsNullOrEmpty(_unit)
                ? null
                : patternParser.Parse(_unit);

            _parsedNamespace = string.IsNullOrEmpty(_namespace)
                ? null
                : patternParser.Parse(_namespace);

            _defaultMetricName = string.IsNullOrEmpty(_metricName)
                ? null
                : patternParser.Parse(_metricName);

            _dateTimeOffset = string.IsNullOrEmpty(_timestamp)
                ? null
                : (DateTimeOffset?) DateTimeOffset.Parse(patternParser.Parse(_timestamp));
        }
    }
}