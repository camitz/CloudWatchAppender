using System;
using MbUnit.Framework;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class LoggerPatternConverterTests
    {
        [Test]
        public void NamedPatternConverterWithoutPrecisionShouldReturnFullName()
        {
            StringAppender stringAppender = new StringAppender();
            PatternLayout layout = new PatternLayout();
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
            PatternLayout layout = new PatternLayout();
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

            log1.Info("TrailingDot.");
            Assert.AreEqual("TrailingDot.", stringAppender.GetString(), "%message-as-name not registered");
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
            PatternLayout layout = new PatternLayout();
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
            Assert.AreEqual("One.Dot", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("Tw.o.Dots");
            Assert.AreEqual("o.Dots", stringAppender.GetString(), "%message-as-name not registered");
            stringAppender.Reset();

            log1.Info("TrailingDot.");
            Assert.AreEqual("TrailingDot.", stringAppender.GetString(), "%message-as-name not registered");
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
    
        private class MessageAsNamePatternConverter : NamedPatternConverter
        {
            protected override string GetFullyQualifiedName(LoggingEvent loggingEvent)
            {
                return loggingEvent.MessageObject.ToString();
            }
        }
    }
}
