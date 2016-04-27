using System.Linq;
using NUnit.Framework;
using SQSAppender.Parsers;

namespace SQSAppender.Tests
{
    [TestFixture]
    public class SQSEventParserTests
    {
        [TearDown]
        public void TearDown()
        {
        }

    


        [Test]
        public void NothingRecognizableShouldProduceCount1()
        {
            var parser = new SQSMessageParser();

            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("A tick");

                var passes = 0;
                string prevID = null;
                foreach (var r in parsedData)
                {
                    Assert.AreEqual("unspecified", r.QueueName);
                    Assert.AreEqual("A tick", r.Message);
                    Assert.AreNotEqual(prevID, r.ID);

                    prevID = r.ID;
                    passes++;
                }

                Assert.AreEqual(1, passes);
            }
        }

        [Test]
        public void TrailingNames()
        {
            var parser = new SQSMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("A tick! QueueName: NewName GroupName: GName");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.QueueName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }

        [Test]
        public void LeadingNames()
        {
            var parser = new SQSMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("QueueName: NewName GroupName: GName A tick!");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.QueueName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }

        [Test]
        public void SurroundingingNames()
        {
            var parser = new SQSMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("QueueName: NewName A tick! GroupName: GName");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.QueueName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }

        [Test]
        public void SurroundedNames()
        {
            var parser = new SQSMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("Beginning tick! QueueName: NewName Middle tick! GroupName: GName End tick!");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.QueueName), Has.All.EqualTo("NewName"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("Beginning tick! Middle tick! End tick!"));
            }
        }

        [Test]
        public void ParenthesizedNames()
        {
            var parser = new SQSMessageParser();
            for (int i = 0; i < 2; i++)
            {
                var parsedData = parser.Parse("QueueName: (New Name) A tick! GroupName: GName");

                var data = parsedData;

                Assert.That(data.Count(), Is.EqualTo(1));
                Assert.That(data.Select(x => x.QueueName), Has.All.EqualTo("New Name"));
                Assert.That(data.Select(x => x.Message), Has.All.EqualTo("A tick!"));
            }
        }
    }
}