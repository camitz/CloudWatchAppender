using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.CloudWatch;
using CloudWatchAppender.Model;

namespace CloudWatchAppender.Parsers
{
   
    public abstract class EventMessageParserBase<TDatum> : IEventMessageParser<TDatum>
    {
        protected bool DefaultsOverridePattern;
        private List<AppenderValue> _values;

        protected EventMessageParserBase(bool useOverrides)
        {
            DefaultsOverridePattern = useOverrides;
        }

        public virtual bool ConfigOverrides { get { return DefaultsOverridePattern; } set { DefaultsOverridePattern = value; } }
        public bool Aggresive { get; set; }
        protected abstract void SetDefaults();
        protected abstract void NewDatum();
        protected abstract bool FillName(AppenderValue value);

        protected void ParseTokens(ref List<Match>.Enumerator tokens, string renderedMessage)
        {
            string t0, unit, sNum = string.Empty, rest = "";
            int? startRest = 0;

            tokens.MoveNext();
            while (tokens.Current != null)
            {
                if (!string.IsNullOrEmpty(t0 = tokens.Current.Groups["name"].Value.Split(new[] { ':' })[0]))
                {
                    if (!IsSupportedName(t0))
                    {
                        tokens.MoveNext();
                        continue;
                    }

                    if (startRest.HasValue)
                        rest += renderedMessage.Substring(startRest.Value, tokens.Current.Index - startRest.Value);

                    startRest = null;

                    if (ShouldLocalParse(t0))
                    {
                        LocalParse(ref tokens, sNum);
                    }
                    else if (t0.StartsWith("Timestamp", StringComparison.InvariantCultureIgnoreCase))
                    {
                        DateTimeOffset time;
                        int length;
                        if (ExtractTime(renderedMessage.Substring(tokens.Current.Index + "Timestamp".Length), out time, out length))
                        {
                            _values.Add(new AppenderValue
                                        {
                                            Name = "Timestamp",
                                            Time = time
                                        });

                            tokens.MoveNext();
                            while (tokens.MoveNext() && tokens.Current.Index <= tokens.Current.Index + length);
                        }

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
                        if (
                            !Double.TryParse(sNum, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d) &&
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
                                var t = StandardUnit.FindValue(unit.ToLowerInvariant());
                                if (t.ToString() != unit) //If conversion capitalizes unit then it is valid and should not be included in rest.
                                    tokens.MoveNext();
                            }

                        _values.Add(v);
                    }
                }
                else
                {
                    startRest = startRest ?? tokens.Current.Index;
                    tokens.MoveNext();
                }
            }

            if (startRest.HasValue)
                rest += renderedMessage.Substring(startRest.Value, renderedMessage.Length - startRest.Value);

            _values.Add(new AppenderValue { Name = "__cav_rest", sValue = rest.Trim() });
        }

        protected abstract bool ShouldLocalParse(string t0);


        protected abstract bool IsSupportedName(string t0);

        private bool ExtractTime(string s, out DateTimeOffset time, out int length)
        {
            var success = false;
            length = 0;
            DateTimeOffset lastTriedTime;

            time = DateTimeOffset.UtcNow;

            s = s.Trim();
            s = s.Trim(":".ToCharArray());

            for (int i = 1; i <= s.Length; i++)
            {
                if (DateTimeOffset.TryParse(s.Substring(0, i), null, DateTimeStyles.AssumeUniversal, out lastTriedTime))
                {
                    success = true;
                    time = lastTriedTime;
                    length = i;
                }
            }

            return success;
        }

        protected virtual void LocalParse(ref List<Match>.Enumerator tokens, string sNum) { }


        protected abstract IEnumerable<TDatum> GetParsedData();
        public IEnumerable<TDatum> Parse(string renderedMessage)
        {
            Init();
            if (!string.IsNullOrEmpty(renderedMessage))
            {

                var tokens =
                    Regex.Matches(renderedMessage,
                        @"(?<float>(\d+\.\d+)|(?<int>\d+))|(?<name>\w+:)|\((?<word>[\w /]+)\)|(?<word>[\w/]+)|(?<lparen>\()|(?<rparen>\))")
                        .Cast<Match>()
                        .ToList()
                        .GetEnumerator();

                ParseTokens(ref tokens, renderedMessage);
            }

            NewDatum();
            foreach (var p in _values)
            {
                try
                {
                    if (!FillName(p))
                    {
                        NewDatum();
                        FillName(p);
                    }
                }
                catch (DatumFilledException)
                {
                    NewDatum();
                    FillName(p);
                }
            }

            SetDefaults();

            return GetParsedData();
        }

        protected virtual void Init()
        {
            _values = new List<AppenderValue>();
        }
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