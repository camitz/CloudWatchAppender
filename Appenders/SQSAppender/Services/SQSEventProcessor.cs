using System;
using System.Collections.Generic;
using System.Linq;
using AWSAppender.Core.Services;
using log4net.Core;
using log4net.Util;
using SQSAppender.Model;
using SQSAppender.Parsers;
using PatternParser = AWSAppender.Core.Services.PatternParser;

namespace SQSAppender.Services
{

    public class SQSEventProcessor : IEventProcessor<SQSDatum>
    {
        private bool _dirtyParsedProperties = true;
        private string _parsedQueueName;
        private int? _parsedDelaySeconds;
        private string _parsedMessage;
        private DateTime? _dateTimeOffset;
        private readonly string _queueName;
        private readonly string _delaySeconds;
        private readonly string _message;
        private string _parsedId;

        public SQSEventProcessor(string queueName, string message, string delaySeconds)
        {
            _queueName = queueName;
            _message = message;
            _delaySeconds = delaySeconds;
        }


        public IEnumerable<SQSDatum> ProcessEvent(LoggingEvent loggingEvent, string renderedString)
        {
            //move to base class
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
            eventMessageParser.DefaultDelaySeconds= _parsedDelaySeconds;

            return eventMessageParser.Parse(renderedString);
        }

        public IEventMessageParser<SQSDatum> EventMessageParser { get; set; }

        private void ParseProperties(PatternParser patternParser)
        {
            _parsedQueueName = string.IsNullOrEmpty(_queueName)
                ? null
                : patternParser.Parse(_queueName);

            _parsedDelaySeconds = string.IsNullOrEmpty(_delaySeconds)
                ? (int?)null
                : Convert.ToInt32(patternParser.Parse(_delaySeconds));

            _parsedMessage = string.IsNullOrEmpty(_message)
                ? null
                : patternParser.Parse(_message);
        }

        private readonly static Type _declaringType = typeof(SQSEventProcessor);
    }
}