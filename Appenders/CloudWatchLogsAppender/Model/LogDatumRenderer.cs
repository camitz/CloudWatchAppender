using System;
using System.Globalization;
using System.IO;
using log4net.ObjectRenderer;

namespace CloudWatchLogsAppender.Model
{
    public class LogDatumRenderer : IObjectRenderer
    {
        public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
        {
            if (obj is LogDatum)
                RenderAppenderLogDatum((LogDatum)obj, writer);
        }

        private void RenderAppenderLogDatum(LogDatum logDatum, TextWriter writer)
        {
            if (!String.IsNullOrEmpty(logDatum.Message))
                writer.Write(logDatum.Message + " ");

            if (!String.IsNullOrEmpty(logDatum.GroupName))
                writer.Write("Groupname: {0}, ", logDatum.GroupName);

            if (!String.IsNullOrEmpty(logDatum.GroupName))
                writer.Write("Streamname: {0}, ", logDatum.StreamName);

            if (logDatum.Timestamp != default(DateTime))
                writer.Write("Timestamp: {0}, ", logDatum.Timestamp.Value.ToString(CultureInfo.CurrentCulture));
        }
    }
}