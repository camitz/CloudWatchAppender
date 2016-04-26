using System;
using System.Globalization;
using System.IO;
using log4net.ObjectRenderer;

namespace CloudWatchAppender.Model
{
    public class SQSDatumRenderer : IObjectRenderer
    {
        public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
        {
            if (obj is SQSDatum)
                RenderAppenderLogDatum((SQSDatum)obj, writer);
        }

        private void RenderAppenderLogDatum(SQSDatum sqsDatum, TextWriter writer)
        {
            if (!String.IsNullOrEmpty(sqsDatum.Message))
                writer.Write(sqsDatum.Message + " ");

            if (!String.IsNullOrEmpty(sqsDatum.QueueName))
                writer.Write("Streamname: {0}, ", sqsDatum.QueueName);

        }
    }
}