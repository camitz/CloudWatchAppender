using System;
using System.Collections;
using log4net.Core;
using log4net.Util;

namespace CloudWatchAppender
{
    public class PatternLayout : log4net.Layout.PatternLayout
    {
        private readonly LoggingEvent _loggingEvent;
        private static Hashtable s_rulesRegistry;

        static PatternLayout()
        {
            s_rulesRegistry = new Hashtable(1)
                    {
                        {"instanceid", typeof (InstanceIDPatternConverter)}, //Deprecated
                        {"metadata", typeof (InstanceMetaDataPatternConverter)}, 
                        {"c", typeof (LoggerPatternConverter)},
                        {"logger", typeof (LoggerPatternConverter)},
                    };
        }

        public PatternLayout()
            : base()
        {
        }

        public PatternLayout(string pattern, LoggingEvent loggingEvent)
            : base(pattern)
        {
            _loggingEvent = loggingEvent;
        }

        public PatternLayout(string pattern)
            : base(pattern)
        {

        }

        protected override log4net.Util.PatternParser CreatePatternParser(string pattern)
        {
            var parser = base.CreatePatternParser(pattern);

            foreach (DictionaryEntry entry in s_rulesRegistry)
            {
                var converterInfo = new ConverterInfo
                                        {
                                            Name = (string)entry.Key, 
                                            Type = (Type)entry.Value
                                        };
                parser.PatternConverters[entry.Key] = converterInfo;
            }

            return parser;
        }

        internal string Parse()
        {
            ActivateOptions();
            return Format(_loggingEvent);
        }
    }
}