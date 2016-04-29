using System;
using System.Collections.Generic;
using AWSAppender.Core.Services;
using AWSAppender.SQS.Model;
using AWSAppender.SQS.Parsers;
using log4net.Core;
using PatternParser = AWSAppender.Core.Services.PatternParser;

namespace AWSAppender.SQS.Services
{

    public class SQSEventProcessor : EventProcessorBase, IEventProcessor<SQSDatum>
    {
        private string _parsedQueueName;
        private int? _parsedDelaySeconds;
        private string _parsedMessage;
        private readonly string _queueName;
        private readonly string _delaySeconds;
        private readonly string _message;

        public SQSEventProcessor(string queueName, string message, string delaySeconds)
        {
            _queueName = queueName;
            _message = message;
            _delaySeconds = delaySeconds;
        }


        public IEnumerable<SQSDatum> ProcessEvent(LoggingEvent loggingEvent, string renderedString)
        {
            renderedString = PreProcess(loggingEvent, renderedString);

            var eventMessageParser = EventMessageParser as ISQSEventMessageParser;

            eventMessageParser.DefaultQueueName = _parsedQueueName;
            eventMessageParser.DefaultMessage = _parsedMessage;
            eventMessageParser.DefaultDelaySeconds= _parsedDelaySeconds;

            return eventMessageParser.Parse(renderedString);
        }

        public IEventMessageParser<SQSDatum> EventMessageParser { get; set; }

        protected override void ParseProperties(PatternParser patternParser)
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
    }
}