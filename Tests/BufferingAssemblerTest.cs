using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using NUnit.Framework;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class BufferingAssemblerTest
    {

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void TestSingleGrouping()
        {
            var result = BufferingAggregatingCloudWatchAppender.Assemble(new[]{new PutMetricDataRequest
                                     {
                                         MetricData = new List<MetricDatum>()
                                                      {
                                                          new MetricDatum()
                                                          {
                                                              Value = 11,
                                                              Unit = "Bytes"
                                                          },
                                                          new MetricDatum()
                                                          {
                                                              StatisticValues = new StatisticSet()
                                                                                {
                                                                                    Maximum = 7,
                                                                                    Minimum = 6,
                                                                                    SampleCount = 3,
                                                                                    Sum = 19
                                                                                },
                                                                                Unit="Kilobits"
                                                          }
                                                      }
                                     }});

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().MetricData, Has.Count.EqualTo(1));
            Assert.That(result.Single().MetricData.Single().Unit, Is.EqualTo(StandardUnit.Bytes));
            Assert.That(result.Single().MetricData.Single().StatisticValues, Is.Not.Null);

            Assert.That(result.Single().MetricData.Single().StatisticValues.SampleCount, Is.EqualTo(4));
            Assert.That(result.Single().MetricData.Single().StatisticValues.Maximum, Is.EqualTo(7 * 1024 / 8));
            Assert.That(result.Single().MetricData.Single().StatisticValues.Minimum, Is.EqualTo(11));
            Assert.That(result.Single().MetricData.Single().StatisticValues.Sum, Is.EqualTo(19 * 1024 / 8 + 11));
        }

        [Test]
        public void TestSingleGrouping2()
        {
            var result = BufferingAggregatingCloudWatchAppender.Assemble(new[]{new PutMetricDataRequest
                                     {
                                         MetricData = new List<MetricDatum>()
                                                      {
                                                          new MetricDatum()
                                                          {
                                                              Value = 11,
                                                              Unit = "Kilobytes"
                                                          },
                                                          new MetricDatum()
                                                          {
                                                              StatisticValues = new StatisticSet()
                                                                                {
                                                                                    Maximum = 7,
                                                                                    Minimum = 6,
                                                                                    SampleCount = 3,
                                                                                    Sum = 19
                                                                                },
                                                                                Unit="Bits"
                                                          }
                                                      }
                                     }});

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().MetricData, Has.Count.EqualTo(1));
            Assert.That(result.Single().MetricData.Single().Unit, Is.EqualTo(StandardUnit.Bits));
            Assert.That(result.Single().MetricData.Single().StatisticValues, Is.Not.Null);

            Assert.That(result.Single().MetricData.Single().StatisticValues.SampleCount, Is.EqualTo(4));
            Assert.That(result.Single().MetricData.Single().StatisticValues.Maximum, Is.EqualTo(11 * 1024 * 8));
            Assert.That(result.Single().MetricData.Single().StatisticValues.Minimum, Is.EqualTo(6));
            Assert.That(result.Single().MetricData.Single().StatisticValues.Sum, Is.EqualTo(11 * 1024 * 8 + 19));
        }  
        
        [Test]
        public void TestSingleGrouping_IncompatibleUnits()
        {
            var result = BufferingAggregatingCloudWatchAppender.Assemble(new[]{new PutMetricDataRequest
                                     {
                                         MetricData = new List<MetricDatum>()
                                                      {
                                                          new MetricDatum()
                                                          {
                                                              Value = 11,
                                                              Unit = "Kilobytes"
                                                          },
                                                          new MetricDatum()
                                                          {
                                                              StatisticValues = new StatisticSet()
                                                                                {
                                                                                    Maximum = 7,
                                                                                    Minimum = 6,
                                                                                    SampleCount = 3,
                                                                                    Sum = 19
                                                                                },
                                                                                Unit="Seconds"
                                                          }
                                                      }
                                     }});

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Single().MetricData, Has.Count.EqualTo(1));
            Assert.That(result.Single().MetricData.Single().Unit, Is.EqualTo(StandardUnit.Bits));
            Assert.That(result.Single().MetricData.Single().StatisticValues, Is.Not.Null);

            Assert.That(result.Single().MetricData.Single().StatisticValues.SampleCount, Is.EqualTo(4));
            Assert.That(result.Single().MetricData.Single().StatisticValues.Maximum, Is.EqualTo(11 * 1024 * 8));
            Assert.That(result.Single().MetricData.Single().StatisticValues.Minimum, Is.EqualTo(6));
            Assert.That(result.Single().MetricData.Single().StatisticValues.Sum, Is.EqualTo(11 * 1024 * 8 + 19));
        }
    }
}