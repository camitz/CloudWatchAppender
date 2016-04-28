using System;
using System.Collections.Generic;
using AWSAppender.Core.Services;
using log4net.Core;
using SNSAppender.Model;
using SNSAppender.Parsers;

namespace SNSAppender.Services
{

    public class SNSEventProcessor : EventProcessorBase, IEventProcessor<SNSDatum>
    {
        private string _parsedTopic;
        private int? _parsedDelaySeconds;
        private string _parsedMessage;
        private DateTime? _dateTimeOffset;
        private readonly string _topic;
        private readonly string _message;
        private string _parsedId;

        public SNSEventProcessor(string topic, string message)
        {
            _topic = topic;
            _message = message;
        }


        public IEnumerable<SNSDatum> ProcessEvent(LoggingEvent loggingEvent, string renderedString)
        {
            renderedString = PreProcess(loggingEvent, renderedString);

            var eventMessageParser = EventMessageParser as ISNSEventMessageParser;

            eventMessageParser.DefaultTopic = _parsedTopic;
            eventMessageParser.DefaultMessage = _parsedMessage;
            //eventMessageParser.DefaultDelaySeconds= _parsedDelaySeconds;

            return eventMessageParser.Parse(renderedString);
        }

        public IEventMessageParser<SNSDatum> EventMessageParser { get; set; }

        protected override void ParseProperties(PatternParser patternParser)
        {
            _parsedTopic = string.IsNullOrEmpty(_topic)
                ? null
                : patternParser.Parse(_topic);

            //_parsedDelaySeconds = string.IsNullOrEmpty(_delaySeconds)
            //    ? (int?)null
            //    : Convert.ToInt32(patternParser.Parse(_delaySeconds));

            _parsedMessage = string.IsNullOrEmpty(_message)
                ? null
                : patternParser.Parse(_message);
        }
    }
}