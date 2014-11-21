using System;
using System.Linq;
using Amazon.CloudWatch;
using CloudWatchAppender.Services;
using NUnit.Framework;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class LogsEventParserTests
    {
        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void SingleValueAndUnit()
        {
            var parser = new MetricDatumEventMessageParser("A tick! Value: 3.0 Kilobytes/Second");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser.GetParsedData())
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
            var parser = new MetricDatumEventMessageParser("A tick! Value: 3.0 Kilobytes/Second")
                         {
                             DefaultValue = 4.0,
                             DefaultUnit = "Megabytes/Second"
                         };
            parser.Parse();

            var passes = 0;
            foreach (var r in parser.GetParsedData())
            {
                Assert.AreEqual(StandardUnit.MegabytesSecond, r.MetricData[0].Unit);
                Assert.AreEqual(4.0, r.MetricData[0].Value);
                passes++;
            }

            Assert.AreEqual(1, passes);
        }


        [Test]
        public void NothingRecognizableShouldProduceCount1()
        {
            var parser = new LogsEventMessageParser("A tick");
            parser.Parse();

            var passes = 0;
            foreach (var r in parser.GetParsedData())
            {
                Assert.AreEqual("unspecified", r.StreamName);
                Assert.AreEqual("A tick", r.Message);
                Assert.AreEqual(null, r.Timestamp);
                Assert.AreEqual("unspecified", r.GroupName);

                passes++;
            }
            Assert.AreEqual(1, passes);
        }

        [Test]
        public void TrailingNames()
        {
            var parser = new LogsEventMessageParser("A tick! StreamName: NewName GroupName: GName");
            parser.Parse();

            var data = parser.GetParsedData();

            Assert.That(data.Count(), Is.EqualTo(1));
            Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("NewName"));
            Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
            Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
        }

        [Test]
        public void LeadingNames()
        {
            var parser = new LogsEventMessageParser("StreamName: NewName GroupName: GName A tick!");
            parser.Parse();

            var data = parser.GetParsedData();

            Assert.That(data.Count(), Is.EqualTo(1));
            Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("NewName"));
            Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
            Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
        }
        
        [Test]
        public void SurroundingingNames()
        {
            var parser = new LogsEventMessageParser("StreamName: NewName A tick! GroupName: GName");
            parser.Parse();

            var data = parser.GetParsedData();

            Assert.That(data.Count(), Is.EqualTo(1));
            Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("NewName"));
            Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
            Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
        }

        [Test]
        public void ParenthesizedNames()
        {
            var parser = new LogsEventMessageParser("StreamName: (New Name) A tick! GroupName: GName");
            parser.Parse();

            var data = parser.GetParsedData();

            Assert.That(data.Count(), Is.EqualTo(1));
            Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("New Name"));
            Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
            Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
        }


        [Test]
        public void Timestamp()
        {
            var parser = new LogsEventMessageParser("A tick! Timestamp: 2012-09-06 17:55:55 +02:00");
            parser.Parse();
            var data = parser.GetParsedData();

            Assert.That(data.Count(), Is.EqualTo(1));
            Assert.That(data.Select(x => x.Timestamp), Has.All.EqualTo(DateTime.Parse("2012-09-06 17:55:55")));
            Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));


            parser = new LogsEventMessageParser("A tick! Timestamp: 2012-09-06 15:55:55");
            parser.Parse();
            data = parser.GetParsedData();

            Assert.That(data.Count(), Is.EqualTo(1));
            Assert.That(data.Select(x => x.Timestamp), Has.All.EqualTo(DateTime.Parse("2012-09-06 15:55:55")));
            Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));

        }
        [Test]
        public void Timestamp_Override()
        {
            var parser = new LogsEventMessageParser("A tick! Timestamp: 2012-09-06 17:55:55 +02:00")
                         {
                             DefaultTimestamp = DateTime.Parse("2012-09-06 12:55:55 +02:00")
                         };

            parser.Parse();
            var data = parser.GetParsedData();

            Assert.That(data.Count(), Is.EqualTo(1));
            Assert.That(data.Select(x => x.Timestamp), Has.All.EqualTo(DateTime.Parse("2012-09-06 12:55:55")));
            Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
        }
    }
}