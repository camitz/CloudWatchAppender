using System;
using System.Linq;
using CloudWatchLogsAppender.Parsers;
using NUnit.Framework;

namespace CloudWatchLogsAppender.Tests
{
    [TestFixture]
    public class DummyEventParserTests
    {
        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void SingleValueAndUnit()
        {
            var parser = new DummyLogsEventMessageParser();
            parser.DefaultStreamName = "stream!!";
            parser.DefaultGroupName = "group!!";
            parser.DefaultTimestamp = DateTime.Parse("2016-03-01");

            var parsedData = parser.Parse("A tick! Message: meddelande hej hallå Timestamp: 2012-09-06 17:55:55 +02:00 StreamName: NewName GroupName: GName");
            Assert.That(parsedData.Count(), Is.EqualTo(1));
            Assert.That(parsedData.Select(x => x.Message), Has.All.EqualTo("A tick! Message: meddelande hej hallå Timestamp: 2012-09-06 17:55:55 +02:00 StreamName: NewName GroupName: GName"));
            Assert.That(parsedData.Select(x => x.StreamName), Has.All.EqualTo("stream!!"));
            Assert.That(parsedData.Select(x => x.GroupName), Has.All.EqualTo("group!!"));
            Assert.That(parsedData.Select(x => x.Timestamp), Has.All.EqualTo(DateTime.Parse("2016-03-01")));
        }

    }
}