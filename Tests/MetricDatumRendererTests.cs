using System;
using System.IO;
using Amazon.CloudWatch.Model;
using MbUnit.Framework;
using log4net;
using log4net.Config;
using log4net.Repository;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class MetricDatumRendererTests
    {

        [Test]
        public void AmazonMetricDatum()
        {
            var t = new StringWriter();

            new MetricDatumRenderer().RenderObject(null, new Amazon.CloudWatch.Model.MetricDatum()
                                        .WithMetricName("TheMetricName")
                                        .WithDimensions(new[]{new Dimension
                                                                   {
                                                                       Name = "dim1", Value = "v1"
                                                                   },
                                                                new Dimension
                                                                    {
                                                                        Name = "dim2", Value = "v2"
                                                                    } })
                                         .WithUnit("Seconds")
                                         .WithValue(5.1)
                                         .WithTimestamp(DateTime.Parse("2012-09-11 11:11")),
                                     t);

            Assert.Contains(t.ToString(), "MetricName: TheMetricName");
            Assert.Contains(t.ToString(), "Unit: Seconds");
            Assert.Contains(t.ToString(), "Value: 5.1");
            Assert.Contains(t.ToString(), "Timestamp: 2012-09-11 11:11");
            Assert.Contains(t.ToString(), "Dimensions: dim1: v1, dim2: v2");
        }

        [Test]
        public void AppenderMetricDatum()
        {
            var t = new StringWriter();

            new MetricDatumRenderer().RenderObject(null, new MetricDatum()
                                        .WithMetricName("TheMetricName")
                                        .WithDimensions(new[]{new Dimension
                                                                   {
                                                                       Name = "dim1", Value = "v1"
                                                                   },
                                                                new Dimension
                                                                    {
                                                                        Name = "dim2", Value = "v2"
                                                                    } })
                                         .WithUnit("Seconds")
                                         .WithValue(5.1)
                                         .WithTimestamp(DateTime.Parse("2012-09-11 11:11")),
                                     t);

            Assert.Contains(t.ToString(), "MetricName: TheMetricName");
            Assert.Contains(t.ToString(), "Unit: Seconds");
            Assert.Contains(t.ToString(), "Value: 5.1");
            Assert.Contains(t.ToString(), "Timestamp: 2012-09-11 11:11");
            Assert.Contains(t.ToString(), "Dimensions: dim1: v1, dim2: v2");
        }

        [Test]
        public void AmazonMetricDatum_WithStatistics()
        {
            var t = new StringWriter();

            new MetricDatumRenderer().RenderObject(null, new Amazon.CloudWatch.Model.MetricDatum()
                                        .WithStatisticValues(new StatisticSet
                                                                 {
                                                                     Maximum = 100.1,
                                                                     Minimum = 2.1,
                                                                     Sum = 250.1,
                                                                     SampleCount = 4
                                                                 }),
                                     t);

            Assert.Contains(t.ToString(), "Maximum: 100.1");
            Assert.Contains(t.ToString(), "Minimum: 2.1");
            Assert.Contains(t.ToString(), "Sum: 250.1");
            Assert.Contains(t.ToString(), "SampleCount: ");
        }

        [Test]
        public void CustomMetricDatum_WithStatistics()
        {
            var t = new StringWriter();

            new MetricDatumRenderer().RenderObject(null, new MetricDatum()
                                        .WithStatisticValues(new StatisticSet
                                                                 {
                                                                     Maximum = 100.1,
                                                                     Minimum = 2.1,
                                                                     Sum = 250.1,
                                                                     SampleCount = 4
                                                                 }),
                                     t);

            Assert.Contains(t.ToString(), "Maximum: 100.1");
            Assert.Contains(t.ToString(), "Minimum: 2.1");
            Assert.Contains(t.ToString(), "Sum: 250.1");
            Assert.Contains(t.ToString(), "SampleCount: ");
        }
    }
}