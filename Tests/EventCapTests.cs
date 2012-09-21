using System;
using System.Collections.Generic;
using Amazon.CloudWatch.Model;
using MbUnit.Framework;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class EventCapTests
    {


        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void DefaultAlwaysAccepts()
        {
            var e = new EventCap();

            var t = DateTime.Parse("2012-09-21 14:00");
            AddSomeRequests(t, e);

            Assert.IsTrue(e.Request(t.AddMilliseconds(900)));
        }

        private static void AddSomeRequests(DateTime t, EventCap e)
        {
            for (int i = 0; i < 10; i++)
                e.Request(t.AddMilliseconds(i*10));
        }
    }
}