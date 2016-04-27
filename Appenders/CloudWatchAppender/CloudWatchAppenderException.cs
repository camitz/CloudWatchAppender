using System;

namespace CloudWatchAppender.Appenders
{
    internal class CloudWatchAppenderException : Exception
    {
        public CloudWatchAppenderException(string msg, Exception innerException)
            : base(msg, innerException)
        {

        }

        public CloudWatchAppenderException(string msg):base(msg)
        {
        }
    }
}