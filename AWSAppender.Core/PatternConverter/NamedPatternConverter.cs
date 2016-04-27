using System;
using System.IO;
using log4net.Core;
using log4net.Layout.Pattern;
using log4net.Util;

namespace AWSAppender.Core.PatternConverter
{
    public abstract class NamedPatternConverter : PatternLayoutConverter, IOptionHandler
    {
        private const string DOT = ".";
        protected int m_precision = 0;
        /// <summary>
        /// The fully qualified type of the NamedPatternConverter class.
        /// </summary>
        /// <remarks>
        /// Used by the internal logger to record the Type of the
        /// log message.
        /// </remarks>
        private static readonly Type declaringType = typeof(NamedPatternConverter);
        /// <summary>
        /// Initialize the converter 
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is part of the <see cref="T:log4net.Core.IOptionHandler" /> delayed object
        /// activation scheme. The <see cref="M:log4net.Layout.Pattern.NamedPatternConverter.ActivateOptions" /> method must 
        /// be called on this object after the configuration properties have
        /// been set. Until <see cref="M:log4net.Layout.Pattern.NamedPatternConverter.ActivateOptions" /> is called this
        /// object is in an undefined state and must not be used. 
        /// </para>
        /// <para>
        /// If any of the configuration properties are modified then 
        /// <see cref="M:log4net.Layout.Pattern.NamedPatternConverter.ActivateOptions" /> must be called again.
        /// </para>
        /// </remarks>
        public void ActivateOptions()
        {
            this.m_precision = 0;
            if (this.Option != null)
            {
                string text = this.Option.Trim();
                if (text.Length > 0)
                {
                    int num;
                    if (SystemInfo.TryParse(text, out num))
                    {
                        if (num == 0)
                        {
                            LogLog.Error(NamedPatternConverter.declaringType, "NamedPatternConverter: Precision option (" + text + ") is zero.");
                        }
                        else
                        {
                            this.m_precision = num;
                        }
                    }
                    else
                    {
                        LogLog.Error(NamedPatternConverter.declaringType, "NamedPatternConverter: Precision option \"" + text + "\" not a decimal integer.");
                    }
                }
            }
        }
        /// <summary>
        /// Get the fully qualified string data
        /// </summary>
        /// <param name="loggingEvent">the event being logged</param>
        /// <returns>the fully qualified name</returns>
        /// <remarks>
        /// <para>
        /// Overridden by subclasses to get the fully qualified name before the
        /// precision is applied to it.
        /// </para>
        /// <para>
        /// Return the fully qualified <c>'.'</c> (dot/period) separated string.
        /// </para>
        /// </remarks>
        protected abstract string GetFullyQualifiedName(LoggingEvent loggingEvent);
        /// <summary>
        /// Convert the pattern to the rendered message
        /// </summary>
        /// <param name="writer"><see cref="T:System.IO.TextWriter" /> that will receive the formatted result.</param>
        /// <param name="loggingEvent">the event being logged</param>
        /// <remarks>
        /// Render the <see cref="M:log4net.Layout.Pattern.NamedPatternConverter.GetFullyQualifiedName(log4net.Core.LoggingEvent)" /> to the precision
        /// specified by the <see cref="P:log4net.Util.PatternConverter.Option" /> property.
        /// </remarks>
        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            string text = this.GetFullyQualifiedName(loggingEvent);
            if (this.m_precision <= 0 || text == null || text.Length < 2)
            {
                writer.Write(text);
            }
            else
            {
                int num = text.Length;
                string str = string.Empty;
                if (text.EndsWith("."))
                {
                    str = ".";
                    text = text.Substring(0, num - 1);
                    num--;
                }
                int num2 = text.LastIndexOf(".");
                int num3 = 1;
                while (num2 > 0 && num3 < this.m_precision)
                {
                    num2 = text.LastIndexOf('.', num2 - 1);
                    num3++;
                }
                if (num2 == -1)
                {
                    writer.Write(text + str);
                }
                else
                {
                    writer.Write(text.Substring(num2 + 1, num - num2 - 1) + str);
                }
            }
        }
    }
}