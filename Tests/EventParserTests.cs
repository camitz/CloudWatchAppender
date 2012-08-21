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
    
    }
}