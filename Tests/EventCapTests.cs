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

        [Test]
        public void RequestMoreThanOneSecondInBetweenAlwaysAccept()
        {
            var e = new EventCap();

            var t = DateTime.Parse("2012-09-21 14:00");
            AddSomeRequests(t, e);

            Assert.IsTrue(e.Request(DateTime.Parse("2012-09-21 14:02")));
        }


        [Test]
        public void Max10ShouldDenyOn11thAndSubsequentButAcceptAfterOneSecond()
        {
            var e = new EventCap(10);

            var t = DateTime.Parse("2012-09-21 14:00");
            AddSomeRequests(t, e);

            Assert.IsFalse(e.Request(t.AddMilliseconds(900)));
            Assert.IsFalse(e.Request(t.AddMilliseconds(910)));
            Assert.IsFalse(e.Request(t.AddMilliseconds(920)));

            Assert.IsFalse(e.Request(t.AddMilliseconds(1000)));
            Assert.IsFalse(e.Request(t.AddMilliseconds(1030)));

            Assert.IsTrue(e.Request(t.AddMilliseconds(2030)));
        }

        [Test]
        public void Max10ShouldDenyOn11th_SameTime()
        {
            var e = new EventCap(10);

            var t = DateTime.Parse("2012-09-21 14:00");
            for (int i = 0; i < 10; i++)
                e.Request(t);

            Assert.IsFalse(e.Request(t));
        }

        private static void AddSomeRequests(DateTime t, EventCap e)
        {
            for (int i = 0; i < 10; i++)
                e.Request(t.AddMilliseconds(i*10));
        }
    }
}