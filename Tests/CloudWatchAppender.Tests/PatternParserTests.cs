using System;
using System.Threading;
using AWSAppender.Core.Services;
using NUnit.Framework;
using log4net;
using log4net.Core;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class PatternParserTests
    {
        [TearDown]
        public void TearDown()
        {
            GlobalContext.Properties.Remove("prop1");
            ThreadContext.Properties.Remove("prop1");
        }

        [Test]
        [Ignore]
        public void TestStackTracePattern()
        {
            var p = new PatternParser(GetLoggingEvent());
            var s = p.Parse("%stacktrace{6}");

            Assert.AreEqual("log4net.Layout.LayoutSkeleton.Format > log4net.Layout.PatternLayout.Format > log4net.Util.PatternConverter.Format > log4net.Layout.Pattern.PatternLayoutConverter.Convert > log4net.Layout.Pattern.StackTracePatternConverter.Convert > log4net.Core.LoggingEvent.get_LocationInformation",
                s);
        }
    
        [Test]
        public void TestMessageAsNamePattern()
        {
            var loggingEvent = GetLoggingEvent();

            var p = new PatternParser(loggingEvent);
            p.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));

            var s = p.Parse("%message-as-name{-2}");
            Assert.AreEqual("Tw/o", s, "%message-as-name not registered");
        }

        private static LoggingEvent GetLoggingEvent()
        {
            LoggingEventData loggingEventData1 = new LoggingEventData();
            loggingEventData1.LoggerName = typeof (PatternParserTests).FullName;
            loggingEventData1.Level = Level.Warn;
            loggingEventData1.Message = "Tw.o.Dots";
            loggingEventData1.Domain = "ReallySimpleApp";
            loggingEventData1.LocationInfo = new LocationInfo(typeof (PatternParserTests).Name, "Main", "Class1.cs", "29");
                //Completely arbitary
            loggingEventData1.ThreadName = Thread.CurrentThread.Name;
            loggingEventData1.TimeStamp = DateTime.Today;
            loggingEventData1.ExceptionString = "Exception occured here";
            loggingEventData1.UserName = "TestUser";
            LoggingEventData loggingEventData = loggingEventData1;

            // LoggingEvents occur at distinct points in time
            LoggingEvent loggingEvent = new LoggingEvent(
                loggingEventData.LocationInfo.GetType(),
                LogManager.CreateRepository(Guid.NewGuid().ToString()),
                loggingEventData.LoggerName,
                loggingEventData.Level,
                loggingEventData.Message,
                new Exception("This is the exception"));
            return loggingEvent;
        }
    }
}