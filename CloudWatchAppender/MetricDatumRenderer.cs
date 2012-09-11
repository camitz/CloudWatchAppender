using System;
using System.Globalization;
using System.IO;
using System.Linq;
using log4net.ObjectRenderer;

namespace CloudWatchAppender
{
    public class MetricDatumRenderer : IObjectRenderer
    {
        public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
        {
            if (obj is Amazon.CloudWatch.Model.MetricDatum)
                RenderAWSMetricDatum((Amazon.CloudWatch.Model.MetricDatum)obj, writer);
            else if (obj is MetricDatum)
                RenderAWSMetricDatum(((MetricDatum)obj).AWSDatum, writer);
        }

        private void RenderAWSMetricDatum(Amazon.CloudWatch.Model.MetricDatum metricDatum, TextWriter writer)
        {
            if (!String.IsNullOrEmpty(metricDatum.MetricName))
                writer.Write(String.Format("MetricName: {0}, ", metricDatum.MetricName));
            if (!String.IsNullOrEmpty(metricDatum.Unit))
                writer.Write(String.Format("Unit: {0}, ", metricDatum.Unit));

            writer.Write(String.Format("Value: {0}, ", metricDatum.Value.ToString(CultureInfo.InvariantCulture)));

            if (metricDatum.Dimensions.Any())
            {
                writer.Write("Dimensions: {0}, ", String.Join(", ",
                                                              metricDatum.Dimensions.Select(
                                                                  x =>
                                                                  String.Format("{0}: {1}", x.Name, x.Value))));
            }

            if (metricDatum.Timestamp != default(DateTime))
                writer.Write(String.Format("Timestamp: {0}, ",
                                           metricDatum.Timestamp.ToString(CultureInfo.CurrentCulture)));

            if (metricDatum.StatisticValues != null)
            {
                if (metricDatum.StatisticValues.Maximum > 0)
                    writer.Write(String.Format("Maximum: {0}, ", metricDatum.StatisticValues.Maximum.ToString(CultureInfo.InvariantCulture)));

                writer.Write(String.Format("Minimum: {0}, ", metricDatum.StatisticValues.Minimum.ToString(CultureInfo.InvariantCulture)));

                if (metricDatum.StatisticValues.SampleCount > 1)
                    writer.Write(String.Format("SampleCount: {0}, ", metricDatum.StatisticValues.SampleCount.ToString(CultureInfo.InvariantCulture)));

                if (metricDatum.StatisticValues.Sum > 0)
                    writer.Write(String.Format("Sum: {0}, ", metricDatum.StatisticValues.Sum.ToString(CultureInfo.InvariantCulture)));
            }
        }
    }
}