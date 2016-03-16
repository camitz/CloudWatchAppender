using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Parsers;
using NUnit.Framework;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class MetricDatumEventParserTests
    {


        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void SingleValueAndUnit()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! Value: 3.0 Kilobytes/Second");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(StandardUnit.KilobytesSecond, r.MetricData[0].Unit);
                Assert.AreEqual(3.0, r.MetricData[0].Value);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void SingleValueAndUnit_Overrides()
        {
            var parser = new MetricDatumEventMessageParser()
            {
                DefaultValue = 4.0,
                DefaultUnit = "Megabytes/Second"
            };
            var parsedData = parser.Parse("A tick! Value: 3.0 Kilobytes/Second");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(StandardUnit.MegabytesSecond, r.MetricData[0].Unit);
                Assert.AreEqual(4.0, r.MetricData[0].Value);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void Statistics()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse(
                "A tick! SampleCount: 3000, Minimum: 1.3 Gigabits/Second, Maximum: 127.9 Gigabits/Second, Sum: 15000.5 Gigabits/Second");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(StandardUnit.GigabitsSecond, r.MetricData[0].Unit);
                Assert.AreEqual(1.3, r.MetricData[0].StatisticValues.Minimum);
                Assert.AreEqual(127.9, r.MetricData[0].StatisticValues.Maximum);
                Assert.AreEqual(15000.5, r.MetricData[0].StatisticValues.Sum);
                Assert.AreEqual(3000, r.MetricData[0].StatisticValues.SampleCount);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void Statistics_Overrides()
        {
            var parser =
                new MetricDatumEventMessageParser()
                {
                    DefaultSampleCount = 4000,
                    DefaultMinimum = 1.2,
                    DefaultMaximum = 130.5,
                    DefaultSum = 16000.5
                };
            var parsedData = parser.Parse(
                "A tick! SampleCount: 3000, Minimum: 1.3 Gigabits/Second, Maximum: 127.9 Gigabits/Second, Sum: 15000.1 Gigabits/Second");


            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(StandardUnit.GigabitsSecond, r.MetricData[0].Unit);
                Assert.AreEqual(1.2, r.MetricData[0].StatisticValues.Minimum);
                Assert.AreEqual(130.5, r.MetricData[0].StatisticValues.Maximum);
                Assert.AreEqual(16000.5, r.MetricData[0].StatisticValues.Sum);
                Assert.AreEqual(4000, r.MetricData[0].StatisticValues.SampleCount);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }


        [Test]
        public void NothingRecognizableShouldProduceCount1()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual("CloudWatchAppender", r.Namespace);
                Assert.AreEqual(1, r.MetricData.Count);
                Assert.AreEqual(0, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("CloudWatchAppender", r.MetricData[0].MetricName);
                Assert.AreEqual(StandardUnit.Count, r.MetricData[0].Unit);
                Assert.AreEqual(1.0, r.MetricData[0].Value);

                passes++;
            }
            Assert.AreEqual(1, passes);
        }

        [Test]
        public void MetricName()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! MetricName: NewName");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual("NewName", r.MetricData[0].MetricName);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void MetricNameAndNameSpace_Overrides()
        {
            var parser = new MetricDatumEventMessageParser()
            {
                DefaultMetricName = "DefaultMetricName",
                DefaultNameSpace = "DefaultNameSpace"
            };
            var parsedData = parser.Parse("A tick! Name: NewName NameSpace: NewNameSpace");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual("DefaultMetricName", r.MetricData[0].MetricName);
                Assert.AreEqual("DefaultNameSpace", r.Namespace);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void NameSpace()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! NameSpace: NewNameSpace");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual("NewNameSpace", r.Namespace);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void ParenthesizedNameSpace()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! NameSpace: (New Name Space)");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual("New Name Space", r.Namespace);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        //[Ignore("Ignore until App Veyor deploy to nuget is working")]
        public void Timestamp_Override()
        {
            var parser = new MetricDatumEventMessageParser()
            {
                DefaultTimestamp = DateTimeOffset.Parse("2012-09-06 12:55:55 +02:00")
            };
            var parsedData = parser.Parse("A tick! Timestamp: 2012-09-06 17:55:55 +02:00");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(DateTime.Parse("2012-09-06 10:55:55Z").ToUniversalTime(), r.MetricData[0].Timestamp);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        [Ignore("Ignore until App Veyor deploy to nuget is working")]
        public void Timestamp()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! Timestamp: 2012-09-06 17:55:55 +02:00");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(DateTime.Parse("2012-09-06 15:55:55"), r.MetricData[0].Timestamp);
                passes++;
            }

            Assert.AreEqual(1, passes);


            parser = new MetricDatumEventMessageParser();
            parsedData = parser.Parse("A tick! Timestamp: 2012-09-06 15:55:55");

            foreach (var r in parsedData)
                Assert.AreEqual(DateTime.Parse("2012-09-06 15:55:55"), r.MetricData[0].Timestamp);
        }

        [Test]
        public void DimensionsList()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! Dimensions: (InstanceID: qwerty, Fruit: apple) Value: 4.5 Seconds");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("qwerty", r.MetricData[0].Dimensions[0].Value);
                Assert.AreEqual("Fruit", r.MetricData[0].Dimensions[1].Name);
                Assert.AreEqual("apple", r.MetricData[0].Dimensions[1].Value);

                Assert.AreEqual(StandardUnit.Seconds, r.MetricData[0].Unit);
                Assert.AreEqual(4.5, r.MetricData[0].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);

            //Not plural, should work anyway
            parser = new MetricDatumEventMessageParser();
            parsedData = parser.Parse("A tick! Dimension: (InstanceID: qwerty, Fruit: apple)");

            foreach (var r in parsedData)
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
        }

        [Test]
        public void DimensionsList_Empties()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! Dimensions: (InstanceID: , Fruit: ) Value: 4.5 Seconds");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(0, r.MetricData[0].Dimensions.Count);

                Assert.AreEqual(StandardUnit.Seconds, r.MetricData[0].Unit);
                Assert.AreEqual(4.5, r.MetricData[0].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);
        }


        [Test]
        public void DimensionsList_Overrides()
        {
            var parser = new MetricDatumEventMessageParser()
            {
                DefaultDimensions = new Dictionary<string, Dimension>
                                                         {
                                                             {"InstanceID", new Dimension{Name="InstanceID", Value = "asdfg"}},
                                                             {"Cake", new Dimension{Name="Cake", Value = "chocolate"}}
                                                         }
            };
            var parsedData = parser.Parse("A tick! Dimensions: (InstanceID: qwerty, Fruit: apple) Value: 4.5 Seconds");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(3, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("asdfg", r.MetricData[0].Dimensions[0].Value);
                Assert.AreEqual("Cake", r.MetricData[0].Dimensions[1].Name);
                Assert.AreEqual("chocolate", r.MetricData[0].Dimensions[1].Value);
                Assert.AreEqual("Fruit", r.MetricData[0].Dimensions[2].Name);
                Assert.AreEqual("apple", r.MetricData[0].Dimensions[2].Value);

                Assert.AreEqual(StandardUnit.Seconds, r.MetricData[0].Unit);
                Assert.AreEqual(4.5, r.MetricData[0].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);

            //Not plural, should work anyway
            parser = new MetricDatumEventMessageParser();
            parsedData = parser.Parse("A tick! Dimension: (InstanceID: qwerty, Fruit: apple)");

            foreach (var r in parsedData)
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
        }

        [Test]
        public void DimensionsList_Overrides_Empties()
        {
            var parser = new MetricDatumEventMessageParser()
            {
                DefaultDimensions = new Dictionary<string, Dimension>
                                                         {
                                                             {"InstanceID", new Dimension{Name="InstanceID", Value = "asdfg"}},
                                                             {"Cake", new Dimension{Name="Cake", Value = "chocolate"}},
                                                             {"Nuts", new Dimension{Name="Nuts", Value = ""}}
                                                         }
            };
            var parsedData = parser.Parse("A tick! Dimensions: (InstanceID: qwerty, Fruit: , Nuts: walnuts) Value: 4.5 Seconds");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("asdfg", r.MetricData[0].Dimensions[0].Value);
                Assert.AreEqual("Cake", r.MetricData[0].Dimensions[1].Name);
                Assert.AreEqual("chocolate", r.MetricData[0].Dimensions[1].Value);

                Assert.AreEqual(StandardUnit.Seconds, r.MetricData[0].Unit);
                Assert.AreEqual(4.5, r.MetricData[0].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);

        }

        [Test]
        public void Dimensions()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! Dimension: (InstanceID: qwerty), Dimension: Fruit: apple) Value: 4.5 Seconds");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);

                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("qwerty", r.MetricData[0].Dimensions[0].Value);
                Assert.AreEqual("Fruit", r.MetricData[0].Dimensions[1].Name);
                Assert.AreEqual("apple", r.MetricData[0].Dimensions[1].Value);

                Assert.AreEqual(StandardUnit.Seconds, r.MetricData[0].Unit);
                Assert.AreEqual(4.5, r.MetricData[0].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);

            //Not plural, should work anyway
            parser = new MetricDatumEventMessageParser();
            parsedData = parser.Parse("A tick! Dimension: (InstanceID: qwerty, Fruit: apple)");

            foreach (var r in parsedData)
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
        }

        [Test]
        public void SingleDimension()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! Dimension: InstanceID: qwerty Value: 4.5 Seconds");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(1, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("qwerty", r.MetricData[0].Dimensions[0].Value);

                Assert.AreEqual(StandardUnit.Seconds, r.MetricData[0].Unit);
                Assert.AreEqual(4.5, r.MetricData[0].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void DimensionUnfinishedParenthsTriesToParseAsDimensionSkippingUnit()
        {            //Plural, with unended parenths, should work anyway
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! Dimensions: (InstanceID: qwerty Value: 4.5 Seconds");

            foreach (var r in parsedData)
            {
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("qwerty", r.MetricData[0].Dimensions[0].Value);
                Assert.AreEqual("Value", r.MetricData[0].Dimensions[1].Name);
                Assert.AreEqual("4.5", r.MetricData[0].Dimensions[1].Value);

                Assert.AreEqual(StandardUnit.Count, r.MetricData[0].Unit);
                Assert.AreEqual(1.0, r.MetricData[0].Value);
            }
        }
    }


}