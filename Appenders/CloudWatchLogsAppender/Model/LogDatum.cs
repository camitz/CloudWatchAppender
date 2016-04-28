using System;
using System.IO;

namespace CloudWatchLogsAppender.Model
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

        public override string ToString()
        {
            var s = new StringWriter();
            new LogDatumRenderer().RenderObject(null, this, s);

            return s.ToString();
        }
    }
}