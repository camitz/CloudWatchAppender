using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using log4net.Core;
using log4net.Util;

namespace AWSAppender.Core.Layout
{
    public class RegexPatternLayout : PatternLayout
    {
        private Regex _regex;
        public string RegexPattern { get; set; }
        public string ReplacementPattern { get; set; }

        public override void Format(TextWriter writer, LoggingEvent loggingEvent)
        {
            if (_regex == null)
                _regex = new Regex(RegexPattern);

            var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            base.Format(stringWriter, loggingEvent);

            var matches = _regex.Match(stringWriter.ToString());

            var s = new List<string>();
            if (matches.Success)
                s.Add(matches.Result(ReplacementPattern));

            writer.Write(string.Join("\n", s.ToArray()));

        }

    }
}