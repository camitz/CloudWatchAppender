using System.Linq;
using log4net.Core;
using log4net.Util;

namespace AWSAppender.Core.Services
{
    public abstract class EventProcessorBase
    {
        private bool _dirtyParsedProperties = true;

        protected string PreProcess(LoggingEvent loggingEvent, string renderedString)
        {
            var patternParser = new PatternParser(loggingEvent);

            if (renderedString.Contains("%"))
                renderedString = patternParser.Parse(renderedString);

            LogLog.Debug(GetType(), string.Format("RenderedString: {0}", renderedString));

            if (_dirtyParsedProperties)
            {
                ParseProperties(patternParser);

                if (
                    !loggingEvent.Properties.GetKeys()
                        .Any(key => key.StartsWith("IsqsAppender.MetaData.") && key.EndsWith(".Error")))
                    _dirtyParsedProperties = false;
            }
            return renderedString;
        }

        protected abstract void ParseProperties(PatternParser patternParser);
    }
}