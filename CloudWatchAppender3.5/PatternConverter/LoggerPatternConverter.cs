using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using log4net.Core;

[assembly: InternalsVisibleTo("CloudWatchAppender.Tests")]

namespace CloudWatchAppender.PatternConverter
{
    internal class LoggerPatternConverter : NamedPatternConverter
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
                return;
            }

            var elements = text
                .Trim()
                .Trim(new[] { '.' })
                .Split(new[] { '.' });

            if (m_precision > 0)
            {
                writer.Write(
                    string.Join("/",
                                elements
                                    .Reverse()
                                    .Take(m_precision)
                                    .Reverse().ToArray()
                        )
                    );
                return;
            }

            writer.Write(
                string.Join("/",
                            elements
                                .Take(-m_precision).ToArray()
                    )
                );
        }
    }
}