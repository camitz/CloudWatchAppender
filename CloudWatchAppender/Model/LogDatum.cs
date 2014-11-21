using System;

namespace CloudWatchAppender.Model
{
    public class LogDatum
    {
        public LogDatum(string message)
        {
            Message = message;
        }

        public LogDatum()
        {
        }

        public string Message { get; set; }
        public string StreamName { get; set; }
        public string GroupName { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}