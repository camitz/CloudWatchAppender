using System.Collections.Generic;
using Amazon.CloudWatch.Model;
using MbUnit.Framework;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class EventParserTests
    {


        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void StringWithSingleValueAndUnit()
        {
            var parser = new EventMessageParser("A tick! Value: 3.0 Kilobytes/Second");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual("Kilobytes/Second", r.MetricData[0].Unit);
                Assert.AreEqual(3.0, r.MetricData[0].Value);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void StringWithSingleValueAndUnit_Ocerrides()
        {
            var parser = new EventMessageParser("A tick! Value: 3.0 Kilobytes/Second")
                             {
                                 DefaultValue = 4.0,
                                 DefaultUnit = "Megabytes/Second"
                             };
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual("Megabytes/Second", r.MetricData[0].Unit);
                Assert.AreEqual(4.0, r.MetricData[0].Value);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void StringWithStatistics()
        {
            var parser = new EventMessageParser("A tick! SampleCount: 3000, Minimum: 1.3 Gigabits/Second, Maximum: 127.9 Gigabits/Second, Sum: 15000 Gigabits/Second");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual("Gigabits/Second", r.MetricData[0].Unit);
                Assert.AreEqual(1.3, r.MetricData[0].StatisticValues.Minimum);
                Assert.AreEqual(127.9, r.MetricData[0].StatisticValues.Maximum);
                Assert.AreEqual(15000, r.MetricData[0].StatisticValues.Sum);
                Assert.AreEqual(3000, r.MetricData[0].StatisticValues.SampleCount);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void StringWithStatistics_Overrides()
        {
            var parser =
                new EventMessageParser(
                    "A tick! SampleCount: 3000, Minimum: 1.3 Gigabits/Second, Maximum: 127.9 Gigabits/Second, Sum: 15000 Gigabits/Second")
                    {
                        DefaultSampleCount = 4000,
                        DefaultMinimum = 1.2,
                        DefaultMaximum = 130,
                        DefaultSum = 16000
                    };
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual("Gigabits/Second", r.MetricData[0].Unit);
                Assert.AreEqual(1.2, r.MetricData[0].StatisticValues.Minimum);
                Assert.AreEqual(130, r.MetricData[0].StatisticValues.Maximum);
                Assert.AreEqual(16000, r.MetricData[0].StatisticValues.Sum);
                Assert.AreEqual(4000, r.MetricData[0].StatisticValues.SampleCount);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }


        [Test]
        public void StringWithNothingRecognizableShouldProduceCount1()
        {
            var parser = new EventMessageParser("A tick");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual("CloudWatchAppender", r.Namespace);
                Assert.AreEqual(1, r.MetricData.Count);
                Assert.AreEqual(0, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("CloudWatchAppender", r.MetricData[0].MetricName);
                Assert.AreEqual("Count", r.MetricData[0].Unit);
                Assert.AreEqual(1.0, r.MetricData[0].Value);

                passes++;
            }
            Assert.AreEqual(1, passes);
        }

        [Test]
        public void StringWithMetricName()
        {
            var parser = new EventMessageParser("A tick! Name: NewName");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual("NewName", r.MetricData[0].MetricName);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void StringWithMetricNameAndNameSpace_Overrides()
        {
            var parser = new EventMessageParser("A tick! Name: NewName NameSpace: NewNameSpace")
                             {
                                 DefaultName = "DefaultName",
                                 DefaultNameSpace = "DefaultNameSpace"
                             };
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual("DefaultName", r.MetricData[0].MetricName);
                Assert.AreEqual("DefaultNameSpace", r.Namespace);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void StringWithNameSpace()
        {
            var parser = new EventMessageParser("A tick! NameSpace: NewNameSpace");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual("NewNameSpace", r.Namespace);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void StringWithDimensionsList()
        {
            var parser = new EventMessageParser("A tick! Dimensions: (InstanceID: qwerty, Fruit: apple) Value: 4.5 Seconds");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("qwerty", r.MetricData[0].Dimensions[0].Value);
                Assert.AreEqual("Fruit", r.MetricData[0].Dimensions[1].Name);
                Assert.AreEqual("apple", r.MetricData[0].Dimensions[1].Value);

                Assert.AreEqual("Seconds", r.MetricData[0].Unit);
                Assert.AreEqual(4.5, r.MetricData[0].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);

            //Not plural, should work anyway
            parser = new EventMessageParser("A tick! Dimension: (InstanceID: qwerty, Fruit: apple)");
            parser.Parse();

            foreach (var r in parser)
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
        }

        [Test]
        public void StringWithDimensionsList_Overrides()
        {
            var parser = new EventMessageParser("A tick! Dimensions: (InstanceID: qwerty, Fruit: apple) Value: 4.5 Seconds")
                             {
                                 DefaultDimensions = new Dictionary<string, Dimension>
                                                         {
                                                             {"InstanceID", new Dimension{Name="InstanceID", Value = "asdfg"}},
                                                             {"Cake", new Dimension{Name="Cake", Value = "chocolate"}}
                                                         }
                             };
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual(3, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("asdfg", r.MetricData[0].Dimensions[0].Value);
                Assert.AreEqual("Cake", r.MetricData[0].Dimensions[1].Name);
                Assert.AreEqual("chocolate", r.MetricData[0].Dimensions[1].Value);
                Assert.AreEqual("Fruit", r.MetricData[0].Dimensions[2].Name);
                Assert.AreEqual("apple", r.MetricData[0].Dimensions[2].Value);

                Assert.AreEqual("Seconds", r.MetricData[0].Unit);
                Assert.AreEqual(4.5, r.MetricData[0].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);

            //Not plural, should work anyway
            parser = new EventMessageParser("A tick! Dimension: (InstanceID: qwerty, Fruit: apple)");
            parser.Parse();

            foreach (var r in parser)
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
        }

        [Test]
        public void StringWithDimensions()
        {
            var parser = new EventMessageParser("A tick! Dimension: (InstanceID: qwerty), Dimension: Fruit: apple) Value: 4.5 Seconds");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);

                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("qwerty", r.MetricData[0].Dimensions[0].Value);
                Assert.AreEqual("Fruit", r.MetricData[0].Dimensions[1].Name);
                Assert.AreEqual("apple", r.MetricData[0].Dimensions[1].Value);

                Assert.AreEqual("Seconds", r.MetricData[0].Unit);
                Assert.AreEqual(4.5, r.MetricData[0].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);

            //Not plural, should work anyway
            parser = new EventMessageParser("A tick! Dimension: (InstanceID: qwerty, Fruit: apple)");
            parser.Parse();

            foreach (var r in parser)
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
        }

        [Test]
        public void StringWithSingleDimension()
        {
            var parser = new EventMessageParser("A tick! Dimension: InstanceID: qwerty Value: 4.5 Seconds");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual(1, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("qwerty", r.MetricData[0].Dimensions[0].Value);

                Assert.AreEqual("Seconds", r.MetricData[0].Unit);
                Assert.AreEqual(4.5, r.MetricData[0].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void StringWithDimensionUnfinishedParenthsTriesToParseAsDimensionSkippingUnit()
        {            //Plural, with unended parenths, should work anyway
            var parser = new EventMessageParser("A tick! Dimensions: (InstanceID: qwerty Value: 4.5 Seconds");
            parser.Parse();

            foreach (var r in parser)
            {
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("qwerty", r.MetricData[0].Dimensions[0].Value);
                Assert.AreEqual("Value", r.MetricData[0].Dimensions[1].Name);
                Assert.AreEqual("4.5", r.MetricData[0].Dimensions[1].Value);

                Assert.AreEqual("Count", r.MetricData[0].Unit);
                Assert.AreEqual(1.0, r.MetricData[0].Value);
            }
        }
    }


}