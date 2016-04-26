using System;
using System.IO;
using CloudWatchAppender.Services;
using log4net.Core;
using log4net.Layout.Pattern;

namespace CloudWatchAppender.PatternConverter
{
    [Obsolete]
    internal sealed class InstanceIDPatternConverter : PatternLayoutConverter
    {
        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            bool error;
            var s = InstanceMetaDataReader.Instance.GetMetaData(MetaDataKeys.instanceid, out error);

            if (error)
                loggingEvent.Properties["IsqsAppender.MetaData." + MetaDataKeys.instanceid + ".Error"] = "error";

            if (string.IsNullOrEmpty(s))
                writer.Write("NoInstanceID");

            writer.Write(s);

        }
    }
}