using System.Text;
using log4net.Appender;
using log4net.Core;

namespace CloudWatchAppender.Tests
{
    public class StringAppender : AppenderSkeleton
    {
        private StringBuilder m_buf = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="StringAppender" /> class.
        /// </summary>
        public StringAppender()
        {
        }

        /// <summary>
        /// Get the string logged so far
        /// </summary>
        /// <returns></returns>
        public string GetString()
        {
            return m_buf.ToString();
        }

        /// <summary>
        /// Reset the string
        /// </summary>
        public void Reset()
        {
            m_buf.Length = 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="loggingEvent">the event to log</param>
        protected override void Append(LoggingEvent loggingEvent)
        {
            m_buf.Append(RenderLoggingEvent(loggingEvent));
        }

        /// <summary>
        /// This appender requires a <see cref="Layout"/> to be set.
        /// </summary>
        /// <value><c>true</c></value>
        protected override bool RequiresLayout
        {
            get { return true; }
        }
    }
}