using System;
using System.IO;
using System.Runtime.CompilerServices;
using log4net.Core;
using log4net.Layout.Pattern;

[assembly: InternalsVisibleTo("MetaDataTester")]

namespace CloudWatchAppender
{
    internal sealed class InstanceMetaDataPatternConverter : PatternLayoutConverter, IOptionHandler
    {
        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            if (string.IsNullOrEmpty(Option))
                throw new InvalidOperationException("The option must be set. Example: metadata{instanceid}.");

            var s = InstanceMetaDataReader.Instance.GetMetaData(Option);
            if (string.IsNullOrEmpty(s))
                writer.Write("No" + Option);

            writer.Write(s);
        }

        public void ActivateOptions()
        {
            var p = Option;
        }
    }
}