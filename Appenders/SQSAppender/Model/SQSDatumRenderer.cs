using System;
using System.IO;
using log4net.ObjectRenderer;

namespace SQSAppender.Model
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
                writer.Write("Queuename: {0}, ", sqsDatum.QueueName);

            if (sqsDatum.DelaySeconds.HasValue)
                writer.Write("DelaySeconds: {0}, ", sqsDatum.DelaySeconds);

            if (!String.IsNullOrEmpty(sqsDatum.ID))
                writer.Write("ID: {0}", sqsDatum.ID);

        }
    }
}