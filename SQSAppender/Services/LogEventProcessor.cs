using System;
using System.Collections.Generic;
using System.Linq;
using CloudWatchAppender.Model;
using CloudWatchAppender.Parsers;
using log4net.Core;
using log4net.Util;

namespace CloudWatchAppender.Services
{
#if NET35
    public interface IEventProcessor<T>
#else
    public interface IEventProcessor<T>
#endif
    {
        //to core
        IEnumerable<T> ProcessEvent(LoggingEvent loggingEvent, string renderedString);
        IEventMessageParser<T> EventMessageParser { get; set; }
    }

    public class LogEventProcessor : IEventProcessor<SQSDatum>
    {
        private bool _dirtyParsedProperties = true;
        private string _parsedQueueName;
        private string _parsedMessage;
        private DateTime? _dateTimeOffset;
        private readonly string _groupName;
        private readonly string _queueName;
        private readonly string _timestamp;
        private readonly string _message;

        public LogEventProcessor(string groupName, string queueName, string timestamp, string message)
        {
            _groupName = groupName;
            _queueName = queueName;
            _timestamp = timestamp;
            _message = message;
        }


        public IEnumerable<SQSDatum> ProcessEvent(LoggingEvent loggingEvent, string renderedString)
        {
            var patternParser = new PatternParser(loggingEvent);

            if (renderedString.Contains("%"))
                renderedString = patternParser.Parse(renderedString);

            LogLog.Debug(_declaringType, string.Format("RenderedString: {0}", renderedString));

            if (_dirtyParsedProperties)
            {
                ParseProperties(patternParser);

                if (!loggingEvent.Properties.GetKeys().Any(key => key.StartsWith("IsqsAppender.MetaData.") && key.EndsWith(".Error")))
                    _dirtyParsedProperties = false;
            }

            var eventMessageParser = EventMessageParser as ISQSEventMessageParser;

            eventMessageParser.DefaultQueueName = _parsedQueueName;
            eventMessageParser.DefaultMessage = _parsedMessage;
            eventMessageParser.DefaultTimestamp = _dateTimeOffset ?? loggingEvent.TimeStamp;

            return eventMessageParser.Parse(renderedString);
        }

        public IEventMessageParser<SQSDatum> EventMessageParser { get; set; }

        private void ParseProperties(PatternParser patternParser)
        {
            _parsedQueueName = string.IsNullOrEmpty(_queueName)
                ? null
                : patternParser.Parse(_queueName);

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