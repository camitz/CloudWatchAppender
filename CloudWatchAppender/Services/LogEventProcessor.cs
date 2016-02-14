using System;
using System.Collections.Generic;
using CloudWatchAppender.Model;
using CloudWatchAppender.Parsers;
using log4net.Core;
using log4net.Util;

namespace CloudWatchAppender.Services
{
    public class LogEventProcessor : IEventProcessor<LogDatum>
    {
        private bool _hasParsedProperties;
        private string _parsedStreamName;
        private string _parsedGroupName;
        private string _parsedMessage;
        private DateTime? _dateTimeOffset;
        private LogsEventMessageParser _logsEventMessageParser;
        private readonly bool _configOverrides;
        private readonly string _groupName;
        private readonly string _streamName;
        private readonly string _timestamp;
        private readonly string _message;

        public LogEventProcessor(bool configOverrides, string groupName, string streamName, string timestamp, string message)
        {
            _configOverrides = configOverrides;
            _groupName = groupName;
            _streamName = streamName;
            _timestamp = timestamp;
            _message = message;
        }


        public IEnumerable<LogDatum> ProcessEvent(LoggingEvent loggingEvent, string renderedString)
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

            _logsEventMessageParser = new LogsEventMessageParser(useOverrides: _configOverrides)
                                  {
                                      DefaultStreamName = _parsedStreamName,
                                      DefaultGroupName = _parsedGroupName,
                                      DefaultMessage = _parsedMessage,
                                      DefaultTimestamp = _dateTimeOffset??loggingEvent.TimeStamp
                                  };

            return _logsEventMessageParser.Parse(renderedString);
        }

        private void ParseProperties(PatternParser patternParser)
        {
            _parsedStreamName = string.IsNullOrEmpty(_streamName)
                ? null
                : patternParser.Parse(_streamName);

            _parsedGroupName = string.IsNullOrEmpty(_groupName)
                ? null
                : patternParser.Parse(_groupName);

            _parsedMessage = string.IsNullOrEmpty(_message)
                ? null
                : patternParser.Parse(_message);

            _dateTimeOffset = string.IsNullOrEmpty(_timestamp)
                ? null
                : (DateTime?)DateTime.Parse(patternParser.Parse(_timestamp));
        }

        private readonly static Type _declaringType = typeof(LogEventProcessor);
    }
}