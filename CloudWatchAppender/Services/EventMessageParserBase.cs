using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace CloudWatchAppender.Services
{
    public abstract class EventMessageParserBase<Datum>
    {
        protected readonly List<AppenderValue> Values = new List<AppenderValue>();
        protected abstract void SetDefaults();
        protected abstract void NewDatum();
        protected abstract bool FillName(AppenderValue value);

        protected void ParseTokens(List<Match>.Enumerator tokens, string renderedMessage)
        {
            string t0, unit, value, name, sNum = string.Empty;

            tokens.MoveNext();
            while (tokens.Current != null)
            {
                if (!string.IsNullOrEmpty(t0 = tokens.Current.Groups["name"].Value.Split(new[] { ':' })[0]))
                {
                    if (IsSupportedName(t0))
                    {
                        tokens.MoveNext();
                        continue;
                    }

                    if (ShouldLocalParse(t0))
                    {
                        LocalParse(tokens, sNum);
                    }
                    else if (t0.StartsWith("Timestamp", StringComparison.InvariantCultureIgnoreCase))
                    {
                        DateTimeOffset time;
                        if (ExtractTime(renderedMessage.Substring(tokens.Current.Index + "Timestamp".Length), out time))
                            Values.Add(new AppenderValue
                            {
                                Name = "Timestamp",
                                Time = time
                            });

                        tokens.MoveNext();
                    }
                    else
                    {
                        if (!tokens.MoveNext())
                            continue;

                        sNum = string.IsNullOrEmpty(tokens.Current.Groups["float"].Value)
                            ? tokens.Current.Groups["int"].Value
                            : tokens.Current.Groups["float"].Value;

                        var sValue = tokens.Current.Groups["word"].Value;

                        if (string.IsNullOrEmpty(sNum) && string.IsNullOrEmpty(sValue))
                        {
                            tokens.MoveNext();
                            continue;
                        }

                        var d = 0.0;
                        if (!Double.TryParse(sNum, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d) &&
                            string.IsNullOrEmpty(sValue))
                        {
                            tokens.MoveNext();
                            continue;
                        }

                        var v = new AppenderValue
                                {
                                    dValue = d,
                                    sValue = string.IsNullOrEmpty(sValue) ? sNum : sValue,
                                    Name = t0
                                };

                        if (tokens.MoveNext())
                            if (!string.IsNullOrEmpty(unit = tokens.Current.Groups["word"].Value))
                            {
                                v.Unit = unit;
                            }

                        Values.Add(v);
                    }
                }
                else
                    tokens.MoveNext();
            }
        }

        private static bool ShouldLocalParse(string t0)
        {
            return t0.StartsWith("Dimension", StringComparison.InvariantCultureIgnoreCase);
        }

        protected abstract bool IsSupportedName(string t0);

        private bool ExtractTime(string s, out DateTimeOffset time)
        {
            var success = false;
            DateTimeOffset lastTriedTime;

            time = DateTimeOffset.UtcNow;

            s = s.Trim();
            s = s.Trim(new[] { ':' });

            for (int i = 1; i <= s.Length; i++)
            {
                if (DateTimeOffset.TryParse(s.Substring(0, i), null, DateTimeStyles.AssumeUniversal, out lastTriedTime))
                {
                    success = true;
                    time = lastTriedTime;
                }
            }

            return success;
        }

        protected virtual void LocalParse(List<Match>.Enumerator tokens, string sNum) { }
    }

    public struct AppenderValue
    {
        public string Name;
        public double? dValue;
        public StandardUnit Unit;
        public string sValue;
        public DateTimeOffset? Time;
    }

}