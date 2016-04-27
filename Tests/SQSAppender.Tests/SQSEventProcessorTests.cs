using NUnit.Framework;
using SQSAppender.Services;

namespace SQSAppender.Tests
{
    [TestFixture]
    public class SQSEventProcessorTests
    {
        [TearDown]
        public void TearDown()
        {
        }

    
        [Test]
        public void NullInits()
        {
            var processor = new SQSEventProcessor("queueue","messageeee");

            //todo: mock?
        }
    }
}