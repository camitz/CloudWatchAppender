using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Runtime.Internal.Util;
using AWSAppender.CloudWatch.Parsers;
using NUnit.Framework;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class MetricDatumEventParserTests_Json
    {


        [TearDown]
        public void TearDown()
        {
        }

     

        [Test]
        public void SingleValueAndUnit1()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! {Foo: 3.5 Kilobytes/Second, Value: 3.0 Kilobytes/Second}");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(StandardUnit.KilobytesSecond, r.MetricData[0].Unit);
                Assert.AreEqual(3.0, r.MetricData[0].Value);
                //Assert.AreEqual("A tick!", r.MetricData[1].Value);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void SingleValueAndUnit2()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! {Bar: { Foo: 4.0, Value: 3.0, Unit: \"Kilobytes/Second\", \"Type\": \"Timing\"}} {Value: { Other: 4.0, Value: 3.0, Unit: \"Kilobytes/Second\", \"Type\": \"Timing\"}}");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(StandardUnit.KilobytesSecond, r.MetricData[0].Unit);
                Assert.AreEqual(3.0, r.MetricData[0].Value);
                //Assert.AreEqual("A tick!", r.MetricData[1].Value);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void SingleValueAndUnit_Zero()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! {Value: \"0.0 Kilobytes/Second\"}");

            Assert.That(parsedData, Has.Exactly(1).Not.Null);
            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(StandardUnit.KilobytesSecond, r.MetricData[0].Unit);
                Assert.AreEqual(0.0, r.MetricData[0].Value);
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
                "A tick! {SampleCount: 3000, Minimum: { value:1.3, unit:\"Gigabits/Second\"},  Maximum: \"127.9 Gigabits/Second\",  Sum: {value: \"15000.5\", \"unit\": \"Gigabits/Second\"}}");

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
        public void Statistics2()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse(
                "A tick! {SampleCount: {value:3000}, Minimum: { value:1.3, unit:\"Gigabits/Second\"}, Maximum: {value:\"127.9\",unit: \"Gigabits/Second\"}, Sum: {value: \"15000.5\", \"unit\": \"Gigabits/Second\"}}");

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
        public void Statistics3()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse(
                "A tick! {SampleCount: {value:3000}, Minimum: 1.3 Gigabits/Second, Maximum: \"127.9 Gigabits/Second\", Sum: {value: \"15000.5\", \"unit\": \"Gigabits/Second\"}}");

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
        public void NothingRecognizableShouldProduceCount1()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick {}");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual("CloudWatchAppender", r.Namespace);
                Assert.AreEqual(1, r.MetricData.Count);
                Assert.AreEqual(0, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("CloudWatchAppender", r.MetricData[0].MetricName);
                Assert.AreEqual(StandardUnit.Count, r.MetricData[0].Unit);
                Assert.AreEqual(0, r.MetricData[0].Value);

                passes++;
            }
            Assert.AreEqual(1, passes);
        }

        [Test]
        public void MetricName()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! {MetricName: NewName}");

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
            var parsedData = parser.Parse("A tick! {Name: NewName, NameSpace: NewNameSpace}");

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
        public void ParenthesizedNameSpace()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! NameSpace: \"New Name Space\"");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual("New Name Space", r.Namespace);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void Timestamp_Override()
        {
            var parser = new MetricDatumEventMessageParser()
                         {
                             DefaultTimestamp = DateTimeOffset.Parse("2012-09-06 12:55:55")
                         };
            var parsedData = parser.Parse("A tick! Timestamp: \"2012-09-06 17:55:55\"");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(DateTime.Parse("2012-09-06 10:55:55"), r.MetricData[0].Timestamp);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }

        [Test]
        public void Timestamp()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! {Timestamp: 2012-09-06 17:55:55}");

            var passes = 0;
            foreach (var r in parsedData)
            {
                Assert.AreEqual(DateTime.Parse("2012-09-06 17:55:55"), r.MetricData[0].Timestamp);
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
            var parsedData = parser.Parse("A tick! Dimensions: {InstanceID: qwerty, Fruit: apple}, Value: \"4.5 Seconds\"");

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
            var parsedData = parser.Parse("A tick! Dimensions: {InstanceID: , Fruit: } Value: 4.5 Seconds");

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
        public void SingleDimension()
        {
            var parser = new MetricDatumEventMessageParser();
            var parsedData = parser.Parse("A tick! {Dimension: {InstanceID: qwerty },Value: 4.5 Seconds}");

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
        public void MultipleValuesWithAggressiveParsing([Values(true, false)] bool aggresive, [Values(true, false)] bool configOverrides)
        {
            var parser = new MetricDatumEventMessageParser
                         {
                             Aggresive = aggresive,
                             ConfigOverrides = configOverrides
                         };
            var parsedData = parser.Parse("A tick! {Metric1: \"5 seconds\", Metric2: \"4.6 kilobytes\"}").ToArray();

            if (aggresive)
            {
                Assert.AreEqual(2, parsedData.Count());

                Assert.AreEqual(0, parsedData[0].MetricData[0].Dimensions.Count);
                Assert.AreEqual(StandardUnit.Seconds, parsedData[0].MetricData[0].Unit);
                Assert.AreEqual(5d, parsedData[0].MetricData[0].Value);
                Assert.AreEqual("Metric1", parsedData[0].MetricData[0].MetricName);

                Assert.AreEqual(0, parsedData[1].MetricData[0].Dimensions.Count);
                Assert.AreEqual(StandardUnit.Kilobytes, parsedData[1].MetricData[0].Unit);
                Assert.AreEqual(4.6d, parsedData[1].MetricData[0].Value);
                Assert.AreEqual("Metric2", parsedData[1].MetricData[0].MetricName);
            }
            else
            {
                Assert.AreEqual(1, parsedData.Count());

                Assert.AreEqual(0, parsedData[0].MetricData[0].Dimensions.Count);
                Assert.AreEqual(StandardUnit.Count, parsedData[0].MetricData[0].Unit);
                Assert.AreEqual(0, parsedData[0].MetricData[0].Value);
                Assert.AreEqual("CloudWatchAppender", parsedData[0].MetricData[0].MetricName);
            }
        }
    }
}