using System;
using System.Threading;
using MbUnit.Framework;
using log4net;
using log4net.Core;

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
        public void StringWithNothingRecognizableShouldProduceCount1()
        {
            var parser = new EventMessageParser("A tick");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual("CloudWatchAppender", r.Namespace);
                Assert.AreEqual(1,r.MetricData.Count);
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
            var parser = new EventMessageParser("A tick! Dimensions: (InstanceID: qwerty, Fruit: apple");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser)
            {
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
                Assert.AreEqual("InstanceID", r.MetricData[0].Dimensions[0].Name);
                Assert.AreEqual("qwerty", r.MetricData[0].Dimensions[0].Value);
                Assert.AreEqual("Fruit", r.MetricData[0].Dimensions[1].Name);
                Assert.AreEqual("apple", r.MetricData[0].Dimensions[1].Value);

                passes++;
            }

            Assert.AreEqual(1, passes);

            //Not plural, should work anyway
            parser = new EventMessageParser("A tick! Dimension: (InstanceID: qwerty, Fruit: apple");
            parser.Parse();

            foreach (var r in parser)
                Assert.AreEqual(2, r.MetricData[0].Dimensions.Count);
        }
    }
}