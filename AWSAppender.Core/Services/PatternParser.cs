using System;
using System.Collections.Generic;
using AWSAppender.Core.Layout;
using log4net.Core;

namespace AWSAppender.Core.Services
{
    public class PatternParser
    {
        private readonly LoggingEvent _loggingEvent;
        private IDictionary<string, Type> _converters = new Dictionary<string, Type>();

        public PatternParser(LoggingEvent loggingEvent)
        {
            _loggingEvent = loggingEvent;
        }

        public string Parse(string pattern)
        {
            var l = new PatternLayout(pattern, _loggingEvent);
            foreach (var converter in _converters)
            {
                l.AddConverter(converter.Key, converter.Value);
            }
            return l.Parse();
        }


        public void AddConverter(string messageAsName, Type type)
        {
            _converters.Add(messageAsName, type);
        }
    }
}