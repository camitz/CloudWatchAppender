using System;

namespace SQSAppender
{
    internal class SQSAppenderException : Exception
    {
        public SQSAppenderException(string msg, Exception innerException)
            : base(msg, innerException)
        {

        }

        public SQSAppenderException(string msg):base(msg)
        {
        }
    }
}