using System.Collections;
using log4net.Core;
using log4net.Layout;

namespace CloudWatchAppender
{
    public class CloudWathPatternLayout : PatternLayout
    {
        private readonly string _pattern;
        private readonly LoggingEvent _loggingEvent;
        private static Hashtable s_rulesRegistry;

        static CloudWathPatternLayout()
        {
            s_rulesRegistry = new Hashtable(1)
                    {
                        {"instanceid", typeof (InstanceIDPatternConverter)},
                        {"c", typeof (LoggerPatternConverter)},
                        {"logger", typeof (LoggerPatternConverter)},
                    };
        }

        public CloudWathPatternLayout(string pattern, LoggingEvent loggingEvent)
            : base(pattern)
        {
            _loggingEvent = loggingEvent;
        }

        public CloudWathPatternLayout(string pattern):base(pattern)
        {
            
        }

        protected override log4net.Util.PatternParser CreatePatternParser(string pattern)
        {
            var parser = base.CreatePatternParser(pattern);

            foreach (DictionaryEntry dictionaryEntry in s_rulesRegistry)
                parser.PatternConverters[dictionaryEntry.Key] = dictionaryEntry.Value;

            return parser;
        }

        internal string Parse()
        {
            ActivateOptions();
            return Format(_loggingEvent);
        }
    }
}