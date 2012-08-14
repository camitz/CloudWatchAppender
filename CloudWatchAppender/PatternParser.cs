using log4net.Core;

namespace CloudWatchAppender
{
    class PatternParser
    {
        private readonly LoggingEvent _loggingEvent;

        public PatternParser(LoggingEvent loggingEvent)
        {
            _loggingEvent = loggingEvent;
        }

        public string Parse(string pattern)
        {
            var l = new CloudWathPatternLayout(pattern, _loggingEvent);
            return l.Parse();
        }


    }
}