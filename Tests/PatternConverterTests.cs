using System;
using NUnit.Framework;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class PatternConverterTests
    {

        [TearDown]
        public void TearDown()
        {
            GlobalContext.Properties.Remove("prop1");
            ThreadContext.Properties.Remove("prop1");
        }

        [Test]
        public void TestStackTracePattern()
        {
            StringAppender stringAppender = new StringAppender();
            stringAppender.Layout = new PatternLayout("%stacktrace{2}");

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);

            ILog log1 = LogManager.GetLogger(rep.Name, "TestStackTracePattern");

            log1.Info("TestMessage");
#if !MONO
            Assert.AreEqual("System.RuntimeMethodHandle.InvokeMethod > CloudWatchAppender.Tests.PatternConverterTests.TestStackTracePattern", stringAppender.GetString(), "stack trace value set");
#else
            Assert.AreEqual("MonoMethod.InternalInvoke > PatternConverterTests.TestStackTracePattern", stringAppender.GetString(), "stack trace value set");
#endif
            stringAppender.Reset();
        }

        [Test]
        public void NamedPatternConverterWithoutPrecisionShouldReturnFullName()
        {
            StringAppender stringAppender = new StringAppender();
            log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout();
            layout.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));
            layout.ConversionPattern = "%message-as-name";
            layout.ActivateOptions();
            stringAppender.Layout = layout;
            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);
            ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

            log1.Info("NoDots");
            Assert.AreEqual("NoDots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("One.Dot");
            Assert.AreEqual("One.Dot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("Tw.o.Dots");
            Assert.AreEqual("Tw.o.Dots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("TrailingDot.");
            Assert.AreEqual("TrailingDot.", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".LeadingDot");
            Assert.AreEqual(".LeadingDot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            // empty string and other evil combinations as tests for of-by-one mistakes in index calculations
            log1.Info(string.Empty);
            Assert.AreEqual(string.Empty, stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".");
            Assert.AreEqual(".", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("x");
            Assert.AreEqual("x", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();
        }

        [Test]
        public void NamedPatternConverterWithPrecision1ShouldStripLeadingStuffIfPresent()
        {
            StringAppender stringAppender = new StringAppender();
            log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout();
            layout.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));
            layout.ConversionPattern = "%message-as-name{1}";
            layout.ActivateOptions();
            stringAppender.Layout = layout;
            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);
            ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

            log1.Info("NoDots");
            Assert.AreEqual("NoDots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("One.Dot");
            Assert.AreEqual("Dot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("Tw.o.Dots");
            Assert.AreEqual("Dots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            //This behaviour differs from the log4net source.
            log1.Info("TrailingDot.");
            Assert.AreEqual("TrailingDot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".LeadingDot");
            Assert.AreEqual("LeadingDot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            // empty string and other evil combinations as tests for of-by-one mistakes in index calculations
            log1.Info(string.Empty);
            Assert.AreEqual(string.Empty, stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("x");
            Assert.AreEqual("x", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".");
            Assert.AreEqual(".", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();
        }

        [Test]
        public void NamedPatternConverterWithPrecision2ShouldStripLessLeadingStuffIfPresent()
        {
            StringAppender stringAppender = new StringAppender();
            log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout();
            layout.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));
            layout.ConversionPattern = "%message-as-name{2}";
            layout.ActivateOptions();
            stringAppender.Layout = layout;
            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);
            ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

            log1.Info("NoDots");
            Assert.AreEqual("NoDots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("One.Dot");
            Assert.AreEqual("One/Dot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("Tw.o.Dots");
            Assert.AreEqual("o/Dots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("TrailingDot.");
            Assert.AreEqual("TrailingDot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".LeadingDot");
            Assert.AreEqual("LeadingDot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            // empty string and other evil combinations as tests for of-by-one mistakes in index calculations
            log1.Info(string.Empty);
            Assert.AreEqual(string.Empty, stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("x");
            Assert.AreEqual("x", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".");
            Assert.AreEqual(".", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();
        }

        [Test]
        public void NamedPatternConverterWithPrecisionMinus2ShouldStripLastTwoElements()
        {
            StringAppender stringAppender = new StringAppender();
            log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout();
            layout.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));
            layout.ConversionPattern = "%message-as-name{-2}";
            layout.ActivateOptions();
            stringAppender.Layout = layout;
            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);
            ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

            log1.Info("Tw.o.Dots");
            Assert.AreEqual("Tw/o", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("NoDots");
            Assert.AreEqual("NoDots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("One.Dot");
            Assert.AreEqual("One/Dot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("TrailingDot.");
            Assert.AreEqual("TrailingDot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".LeadingDot");
            Assert.AreEqual("LeadingDot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            // empty string and other evil combinations as tests for of-by-one mistakes in index calculations
            log1.Info(string.Empty);
            Assert.AreEqual(string.Empty, stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("x");
            Assert.AreEqual("x", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info(".");
            Assert.AreEqual(".", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();
        }

    }

    internal class MessageAsNamePatternConverter : LoggerPatternConverter
    {
        protected override string GetFullyQualifiedName(LoggingEvent loggingEvent)
        {
            return loggingEvent.MessageObject.ToString();
        }
    }
}
