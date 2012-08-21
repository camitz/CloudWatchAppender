using System;
using System.Threading;
using MbUnit.Framework;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class PatternParserTests
    {

        const string TEST_REPOSITORY = "Test Repository";
        
        [TearDown]
        public void TearDown()
        {
            GlobalContext.Properties.Remove("prop1");
            ThreadContext.Properties.Remove("prop1");
        }

        [Test]
        public void TestStackTracePattern()
        {
            var p = new PatternParser(GetLoggingEvent());
            var s = p.Parse("%stacktrace{10}");

            Assert.AreEqual("RuntimeMethodHandle._InvokeMethodFast > PatternParserTests.TestStackTracePattern > PatternParser.Parse > PatternLayout.Parse > LayoutSkeleton.Format > PatternLayout.Format > PatternConverter.Format > PatternLayoutConverter.Convert > StackTracePatternConverter.Convert > LoggingEvent.get_LocationInformation", s);
        }
    
        [Test]
        public void TestMessageAsNamePattern()
        {
            var loggingEvent = GetLoggingEvent();

            var p = new PatternParser(loggingEvent);
            p.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));

            var s = p.Parse("%message-as-name{-2}");
            Assert.AreEqual("Tw", s, "%message-as-name not registered");
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