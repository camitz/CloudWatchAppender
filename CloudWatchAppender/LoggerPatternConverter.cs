using System.IO;
using System.Linq;
using log4net.Core;

namespace CloudWatchAppender
{
    internal sealed class LoggerPatternConverter : NamedPatternConverter
    {
        protected override string GetFullyQualifiedName(LoggingEvent loggingEvent)
        {
            return loggingEvent.LoggerName;
        }

        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            var text = GetFullyQualifiedName(loggingEvent);

            if (m_precision == 0 || text == null || text.Length < 2)
            {
                writer.Write(text);
            }
                var strings = text.Split(new[] { '.' });
            if (m_precision > 0)
            {
                writer.Write(
                    string.Join("/",
                        strings
                            .Reverse()
                            .Take(m_precision)
                            .Reverse()
                        )
                    );
            }
            else
            {
                writer.Write(
                   string.Join("/",
                       strings
                           .Take(strings.Count() - m_precision)
                       )
                   );
            }
        }
    }
}