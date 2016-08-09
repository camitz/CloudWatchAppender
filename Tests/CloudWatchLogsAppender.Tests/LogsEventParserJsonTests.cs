using System;
using System.Linq;
using AWSAppender.CloudWatchLogs.Parsers;
using NUnit.Framework;

namespace CloudWatchLogsAppender.Tests
{
    [TestFixture]
    public class LogsEventParserJsonTests
    {
        [TearDown]
        public void TearDown()
        {
        }




        [Test]
        public void NothingRecognizableShouldProduceCount1()
        {
            var parser = new LogsEventMessageParser();

            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("{message: \"A tick!\", }");

                var passes = 0;
                foreach (var r in parsedData)
                {
                    Assert.AreEqual("unspecified", r.StreamName);
                    Assert.AreEqual("A tick!", r.Message);
                    Assert.AreEqual(null, r.Timestamp);
                    Assert.AreEqual("unspecified", r.GroupName);

                    passes++;
                }
                Assert.AreEqual(1, passes);
            }
        }

        [Test]
        public void StructuredNames()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("{Message: \"A tick!\",StreamName: \"NewName\", GroupName: \"GName\"}");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }

        [Test]
        public void TrailingNames()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("A tick! {StreamName: \"NewName\", GroupName: \"GName\"}");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }

        [Test]
        public void TrailingNames_JsonRest()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("{Data: \"A tick!\"} {StreamName: \"NewName\", GroupName: \"GName\"}");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("{Data: \"A tick!\"}"));
            }
        }

        [Test]
        public void TrailingNames_JsonRest2()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("{\"Data\": \"A tick!\"} StreamName: \"NewName\", GroupName: \"GName\"");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("{\"Data\": \"A tick!\"}"));
            }
        }

        [Test]
        public void LeadingNames()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("{StreamName: \"NewName\" ,GroupName: \"GName\" }A tick!");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }

        [Test]
        public void SurroundingingNames()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("{StreamName: \"NewName\"} A tick! {GroupName: \"GName\"}");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }

        [Test]
        public void SurroundedNames()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("Beginning tick! {StreamName: \"NewName\"} Middle tick! GroupName: \"GName\" End tick!");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("Beginning tick! Middle tick! End tick!"));
            }
        }

        [Test]
        public void ParenthesizedNames()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("{StreamName: \"New Name\"} A tick! {GroupName: \"GName\"");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("New Name"));
                Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick! {"));
            }
        }

        [Test]
        public void QuotedNames()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("{StreamName: \"New Name\"} A tick! {GroupName: GName}");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.StreamName), Has.All.EqualTo("New Name"));
                Assert.That(data.Select(x => x.GroupName), Has.All.EqualTo("GName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }



        [Test]
#if APPVEYOR
        [Ignore("Appveyor fails these")]
#endif
        public void Timestamp()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("A tick! {Timestamp: \"2012-09-06 17:55:55 +02:00\"}");
                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.Timestamp), Has.All.EqualTo(DateTime.Parse("2012-09-06 17:55:55")));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }

            parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("A tick! Timestamp: 2012-09-06 15:55:55");
                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.Timestamp), Has.All.EqualTo(DateTime.Parse("2012-09-06 15:55:55")));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }

        [Test]
        public void Timestamp2()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("{Timestamp: \"2012-09-06 17:55:55 +02:00\"} A tick! ");
                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.Timestamp), Has.All.EqualTo(DateTime.Parse("2012-09-06 17:55:55")));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }

        }

        [Test]
        public void Timestamp3()
        {
            var parser = new LogsEventMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("{\"Timestamp\": 2012-09-06 15:55:55} A tick! ");
                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.Timestamp), Has.All.EqualTo(DateTime.Parse("2012-09-06 15:55:55")));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }


        [Test]
#if APPVEYOR
        [Ignore("Appveryor fails this")]
#endif
        public void Timestamp_Override()
        {
            var parser = new LogsEventMessageParser
                         {
                             DefaultTimestamp = DateTime.Parse("2012-09-06 12:55:55")
                         };

            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("A tick! {Timestamp: \"2012-09-06 17:55:55\"}");
                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.Timestamp), Has.All.EqualTo(DateTime.Parse("2012-09-06 12:55:55")));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }

    }
}