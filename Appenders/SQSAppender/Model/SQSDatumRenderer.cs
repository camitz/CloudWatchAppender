using System;
using System.IO;
using log4net.ObjectRenderer;

namespace AWSAppender.SQS.Model
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
            var s = "";
            if (!String.IsNullOrEmpty(sqsDatum.Message))
                s += sqsDatum.Message + " ";

            if (!String.IsNullOrEmpty(sqsDatum.QueueName))
                s+=String.Format("Queuename: {0}, ", sqsDatum.QueueName);

            if (sqsDatum.DelaySeconds.HasValue)
                s+=String.Format("DelaySeconds: {0}, ", sqsDatum.DelaySeconds);

            if (!String.IsNullOrEmpty(sqsDatum.ID))
                s += String.Format("ID: {0}", sqsDatum.ID);

            s = s.Trim(", ".ToCharArray());

            writer.Write(s);
        }
    }
}