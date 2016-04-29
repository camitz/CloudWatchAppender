using System;
using AWSAppender.Core.Services;
using NUnit.Framework;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class EventRateLimiterTests
    {

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void DefaultAlwaysAccepts()
        {
            var e = new EventRateLimiter();

            var t = DateTime.Parse("2012-09-21 14:00");
            AddSomeRequests(t, e);

            Assert.IsTrue(e.Request(t.AddMilliseconds(900)));
        }


        [Test]
        public void Accepts100PerSecond()
        {
            var e = new EventRateLimiter(100);

            var t = DateTime.Parse("2012-09-21 14:00");
            var p = 0;
            for (int i = 0; i < 10000; i++)
                if (e.Request(t + TimeSpan.FromTicks(i)))
                    p++;

            Assert.AreEqual(100, p);
            
            t = DateTime.Parse("2012-09-21 14:00:01");
            p = 0;
            for (int i = 0; i < 10000; i++)
                if (e.Request(t + TimeSpan.FromTicks(i)))
                    p++;

            Assert.AreEqual(100, p);
        }

        private static void AddSomeRequests(DateTime t, EventRateLimiter e)
        {
            for (int i = 0; i < 10; i++)
                e.Request(t.AddMilliseconds(i * 10));
        }
    }
}