using System;
using System.IO;
using AWSAppender.Core.Services;
using log4net.Core;
using log4net.Layout.Pattern;

namespace AWSAppender.Core.PatternConverter
{
    [Obsolete]
    internal sealed class InstanceIDPatternConverter : PatternLayoutConverter
    {
        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            bool error;
            var s = InstanceMetaDataReader.Instance.GetMetaData(MetaDataKeys.instanceid, out error);

            if (error)
                loggingEvent.Properties["AWSAppender.MetaData." + MetaDataKeys.instanceid + ".Error"] = "error";

            if (string.IsNullOrEmpty(s))
                writer.Write("NoInstanceID");

            writer.Write(s);

        }
    }
}