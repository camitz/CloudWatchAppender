using System;
using System.IO;
using log4net.ObjectRenderer;

namespace AWSAppender.SNS.Model
{
    public class SNSDatumRenderer : IObjectRenderer
    {
        public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
        {
            if (obj is SNSDatum)
                RenderAppenderLogDatum((SNSDatum)obj, writer);
        }

        private void RenderAppenderLogDatum(SNSDatum snsDatum, TextWriter writer)
        {
            if (!String.IsNullOrEmpty(snsDatum.Message))
                writer.Write(snsDatum.Message + " ");

            if (!String.IsNullOrEmpty(snsDatum.Topic))
                writer.Write("Topic: {0}", snsDatum.Topic);

            //if (snsDatum.DelaySeconds.HasValue)
            //    writer.Write("DelaySeconds: {0}, ", snsDatum.DelaySeconds);

            //if (!String.IsNullOrEmpty(snsDatum.ID))
            //    writer.Write("ID: {0}", snsDatum.ID);

        }
    }
}