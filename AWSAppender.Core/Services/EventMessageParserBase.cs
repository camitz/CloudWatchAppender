using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AWSAppender.Core.Services
{
    public abstract class EventMessageParserBase<TDatum> : IEventMessageParser<TDatum>
    {
        protected bool DefaultsOverridePattern;
        private List<AppenderValue> _values;

        protected EventMessageParserBase(bool useOverrides)
        {
            DefaultsOverridePattern = useOverrides;
        }

        protected bool ConfigOverrides { get { return DefaultsOverridePattern; } set { DefaultsOverridePattern = value; } }
        protected abstract void ApplyDefaults();
        protected abstract void NewDatum();
        protected abstract bool FillName(AppenderValue value);

        protected virtual void ParseTokens(ref List<Match>.Enumerator enumerator, string renderedMessage)
        {
            string name, sNum = string.Empty, rest = "";
            int? startRest = 0, startRestJson = 0, jsonDepth = 0, ignoreBelow = null, includeAt = null;

            var collectedTokens = new List<int>();

            AppenderValue currentValue = null;

            var matches = new List<Match>();

            while (enumerator.MoveNext())
                matches.Add(enumerator.Current);

            var tokens = matches.GetEnumerator();

            tokens.MoveNext();
            while (tokens.Current != null)
            {
                if (!string.IsNullOrEmpty(tokens.Current.Groups["lbrace"].Value))
                {
                    jsonDepth++;

                    FlushRest(tokens, renderedMessage, ref startRest, ref startRestJson, ref rest);
                    startRestJson = startRestJson ?? tokens.Current.Index;


                    //collectedTokens.Add(tokens.Current.Index);

                    tokens.MoveNext();
                    if (currentValue != null && includeAt == null)
                        includeAt = jsonDepth;


                    continue;
                }

                if (!string.IsNullOrEmpty(tokens.Current.Groups["rbrace"].Value))
                {
                    jsonDepth--;

                    if (startRestJson.HasValue)
                        startRestJson++;
                    FlushRest(tokens, renderedMessage, ref startRest, ref startRestJson, ref rest);

                    //collectedTokens.Add(tokens.Current.Index);

                    tokens.MoveNext();
                    if (currentValue != null && jsonDepth < includeAt)
                    {
                        includeAt = null;
                        _values.Add(currentValue);
                        currentValue = null;
                    }


                    continue;
                }


                if (ignoreBelow != null && jsonDepth > ignoreBelow)
                {
                    tokens.MoveNext();
                    continue;
                }

                if (!string.IsNullOrEmpty(name = tokens.Current.Groups["name"].Value.Split(new[] { ':' })[0]))
                {
                    if (!IsSupportedName(name))
                    {
                        tokens.MoveNext();
                        ignoreBelow = jsonDepth;
                        continue;
                    }

                    collectedTokens.Add(tokens.Current.Index);

                    ignoreBelow = null;

                    if (currentValue != null && includeAt == null)
                        currentValue = null;

                    if (currentValue == null)
                    {
                        currentValue = NewAppenderValue();
                        currentValue.Name = name;
                    }

                    if (includeAt != null && (IsSupportedValueField(name) || name.Equals("value", StringComparison.OrdinalIgnoreCase)))
                    {
                        tokens.MoveNext();

                        sNum = string.IsNullOrEmpty(tokens.Current.Groups["float"].Value)
                            ? tokens.Current.Groups["int"].Value
                            : tokens.Current.Groups["float"].Value;

                        var sValue = tokens.Current.Groups["word"].Value;

                        if (string.IsNullOrEmpty(sNum) && string.IsNullOrEmpty(sValue))
                        {
                            tokens.MoveNext();
                            continue;
                        }

                        double d;

                        if (!Double.TryParse(sNum, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
                        {
                            if (string.IsNullOrEmpty(sValue))
                            {
                                tokens.MoveNext();
                                continue;
                            }

                            Double.TryParse(sValue.Trim("\" ".ToCharArray()), NumberStyles.AllowDecimalPoint,
                                CultureInfo.InvariantCulture, out d);
                        }


                        if (name.Equals("value", StringComparison.OrdinalIgnoreCase))
                        {
                            currentValue.dValue = d;
                            currentValue.sValue = string.IsNullOrEmpty(sValue) ? sNum : sValue;

                            collectedTokens.Add(tokens.Current.Index);
                            PostElementParse(ref tokens, currentValue);

                            continue;
                        }

                        AssignValueField(currentValue, name, d, sNum, sValue);
                        collectedTokens.Add(tokens.Current.Index);
                        tokens.MoveNext();
                        continue;
                    }

                    FlushRest(tokens, renderedMessage, ref startRest, ref startRestJson, ref rest);

                    if (ShouldLocalParse(name))
                    {
                        LocalParse(ref tokens);
                    }
                    else if (name.StartsWith("Timestamp", StringComparison.InvariantCultureIgnoreCase))
                    {
                        DateTimeOffset time;
                        int length;
                        var start = tokens.Current.Index + "Timestamp".Length;
                        if (ExtractTime(renderedMessage.Substring(start), out time, out length))
                        {
                            _values.Add(new AppenderValue
                                        {
                                            Name = "Timestamp",
                                            Time = time
                                        });

                            collectedTokens.Add(tokens.Current.Index);
                            tokens.MoveNext();
                            do
                            {
                                if (tokens.Current != null)
                                    collectedTokens.Add(tokens.Current.Index);
                            } while (tokens.MoveNext() && tokens.Current.Index < start + length);

                        }

                        tokens.MoveNext();
                    }
                    else
                    {
                        if (!tokens.MoveNext())
                            continue;

                        if (!string.IsNullOrEmpty(tokens.Current.Groups["lbrace"].Value))
                            continue;

                        sNum = string.IsNullOrEmpty(tokens.Current.Groups["float"].Value)
                            ? tokens.Current.Groups["int"].Value
                            : tokens.Current.Groups["float"].Value;

                        var sValue = tokens.Current.Groups["word"].Value;

                        var strings = sValue.Split(' ');
                        if (strings.Count() > 1 && string.IsNullOrEmpty(sNum))
                            sNum = strings[0];

                        if (string.IsNullOrEmpty(sNum) && string.IsNullOrEmpty(sValue))
                        {
                            tokens.MoveNext();
                            continue;
                        }

                        double d;

                        if (!Double.TryParse(sNum, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
                        {
                            if (string.IsNullOrEmpty(sValue))
                            {
                                tokens.MoveNext();
                                continue;
                            }

                            Double.TryParse(sValue.Trim("\" ".ToCharArray()), NumberStyles.AllowDecimalPoint,
                                CultureInfo.InvariantCulture, out d);
                        }


                        currentValue.dValue = d;
                        currentValue.sValue = string.IsNullOrEmpty(sValue) ? sNum : sValue;

                        var aggregate = strings.Skip(1).Aggregate("", (a, b) => a + b);

                        if (tokens.Current != null)
                            collectedTokens.Add(tokens.Current.Index);
                        PostElementParse(ref tokens, currentValue, aggregate);

                        AddValue(currentValue);
                        currentValue = null;
                    }
                }
                else
                {
                    startRest = startRest ?? tokens.Current.Index;
                    startRestJson = startRestJson ?? tokens.Current.Index;
                    tokens.MoveNext();
                }
            }

            FlushRest(tokens, renderedMessage, ref startRest, ref startRestJson, ref rest);

            collectedTokens = collectedTokens.Distinct().ToList();

            tokens = matches.GetEnumerator();

            startRest = 0;
            var rest2 = "";
            while (tokens.MoveNext())
            {
                if (!collectedTokens.Contains(tokens.Current.Index))
                    startRest = startRest ?? tokens.Current.Index;
                else
                {
                    if (startRest != null)
                        rest2 += renderedMessage.Substring(startRest.Value, tokens.Current.Index - startRest.Value);
                    startRest = null;
                }
            }

            if (startRest != null)
                rest2 += renderedMessage.Substring(startRest.Value, renderedMessage.Length - startRest.Value);

            rest2 = rest2.Replace(" {} ", " ").Replace("{}", "");
            rest = rest2;

            AddValue(new AppenderValue { Name = "__cav_rest", sValue = rest.Trim() });
        }

        private static void FlushRest(List<Match>.Enumerator tokens, string renderedMessage, ref int? startRest, ref int? startRestJson, ref string rest)
        {
            var index = tokens.Current != null ? tokens.Current.Index : renderedMessage.Length;
            if (startRest.HasValue || startRestJson.HasValue)
            {
                var substring = "";
                //if (startRestJson.HasValue)
                //    substring = renderedMessage.Substring(startRestJson.Value, tokens.Current.Index - startRestJson.Value);

                if (!(substring.Trim().StartsWith("{") && substring.Trim().EndsWith("}")) && startRest.HasValue)
                {
                    substring = renderedMessage.Substring(startRest.Value, index - startRest.Value);
                }

                rest += substring;
            }
            startRest = null;
        }

        protected void AddValue(AppenderValue currentValue)
        {
            _values.Add(currentValue);
        }

        protected virtual void AssignValueField(AppenderValue currentValue, string fieldName, double d, string sNum, string sValue)
        {
        }

        protected virtual AppenderValue NewAppenderValue()
        {
            var v = new AppenderValue();
            return v;
        }

        protected virtual void PostElementParse(ref List<Match>.Enumerator tokens, AppenderValue appenderValue, string aggregate = null)
        {
            tokens.MoveNext();
        }

        protected virtual bool ShouldLocalParse(string t0) { return false; }


        protected abstract bool IsSupportedName(string t0);
        protected abstract bool IsSupportedValueField(string t0);

        private bool ExtractTime(string s, out DateTimeOffset time, out int length)
        {
            var success = false;
            length = 0;

            time = DateTimeOffset.UtcNow;

            var added = s.Length;
            s = s.TrimStart();
            s = s.TrimStart(":\" ".ToCharArray());

            added -= s.Length;

            s = s.TrimEnd();
            s = s.TrimEnd(":\" ".ToCharArray());

            for (int i = 1; i <= s.Length; i++)
            {
                DateTimeOffset lastTriedTime;
                if (DateTimeOffset.TryParse(s.Substring(0, i), null, DateTimeStyles.AssumeUniversal, out lastTriedTime))
                {
                    success = true;
                    time = lastTriedTime;
                    length = i;
                }
            }

            length += added;

            return success;
        }

        protected virtual void LocalParse(ref List<Match>.Enumerator tokens) { }


        protected abstract IEnumerable<TDatum> GetParsedData();
        public IEnumerable<TDatum> Parse(string renderedMessage)
        {
            Init();
            if (!string.IsNullOrEmpty(renderedMessage))
            {

                var tokens =
                    Regex.Matches(renderedMessage,
                        @"(?<lbrace>{)|(?<rbrace>})|(?<float>(\d+\.\d+))|(?<int>\d+)|(?<name>\w+:)|\((?<word>[\w/ ]+)\)|\""(?<word>.*?)\""|(?<word>[^()}{"", ]+)|(?<lparen>\()|(?<rparen>\))")
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

            ApplyDefaults();

            return GetParsedData();
        }

        protected virtual void Init()
        {
            _values = new List<AppenderValue>();
        }
    }
}