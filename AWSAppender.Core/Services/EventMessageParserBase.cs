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
        protected abstract void SetDefaults();
        protected abstract void NewDatum();
        protected abstract bool FillName(AppenderValue value);

        protected virtual void ParseTokens(ref List<Match>.Enumerator tokens, string renderedMessage)
        {
            string name, sNum = string.Empty, rest = "";
            int? startRest = 0, jsonDepth = 0, ignoreBelow = null, includeAt = null;

            AppenderValue currentValue = null;

            tokens.MoveNext();
            while (tokens.Current != null)
            {
                if (!string.IsNullOrEmpty(tokens.Current.Groups["lbrace"].Value))
                {
                    jsonDepth++;
                    tokens.MoveNext();
                    if (currentValue != null && includeAt == null)
                        includeAt = jsonDepth;

                    continue;
                }

                if (!string.IsNullOrEmpty(tokens.Current.Groups["rbrace"].Value))
                {
                    jsonDepth--;
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

                    ignoreBelow = null;

                    if (currentValue != null && includeAt == null)
                        currentValue = null;

                    if (currentValue == null)
                    {
                        currentValue = NewAppenderValue();
                        currentValue.Name = name;
                    }

                    if (includeAt != null && (IsSupportedValueField(name) || name.Equals("value",StringComparison.OrdinalIgnoreCase)))
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

                        var d = 0.0;
                        if (
                            !Double.TryParse(sNum, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d) &&
                            string.IsNullOrEmpty(sValue))
                        {
                            tokens.MoveNext();
                            continue;
                        }


                        if (name.Equals("value", StringComparison.OrdinalIgnoreCase))
                        {
                            currentValue.dValue = d;
                            currentValue.sValue = string.IsNullOrEmpty(sValue) ? sNum : sValue;

                            PostElementParse(ref tokens, currentValue);
                            
                            continue;
                        }

                        AssignValueField(currentValue,name, d, sNum, sValue);
                        tokens.MoveNext();
                        continue;
                    }

                    if (startRest.HasValue)
                        rest += renderedMessage.Substring(startRest.Value, tokens.Current.Index - startRest.Value);

                    startRest = null;

                    if (ShouldLocalParse(name))
                    {
                        LocalParse(ref tokens, sNum);
                    }
                    else if (name.StartsWith("Timestamp", StringComparison.InvariantCultureIgnoreCase))
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
                            while (tokens.MoveNext() && tokens.Current.Index <= tokens.Current.Index + length) ;
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
                        if (!Double.TryParse(sNum, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d) &&
                            string.IsNullOrEmpty(sValue))
                        {
                            tokens.MoveNext();
                            continue;
                        }


                        currentValue.dValue = d;
                        currentValue.sValue = string.IsNullOrEmpty(sValue) ? sNum : sValue;

                        var aggregate = strings.Skip(1).Aggregate("",(a,b)=>a+b);

                        PostElementParse(ref tokens, currentValue,aggregate);

                        _values.Add(currentValue);
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

        protected virtual void AssignValueField(AppenderValue currentValue, string fieldName, double d, string sNum, string sValue)
        {
        }

        protected virtual AppenderValue NewAppenderValue()
        {
            var v = new AppenderValue();
            return v;
        }

        protected virtual void PostElementParse(ref List<Match>.Enumerator tokens, AppenderValue appenderValue, string aggregate=null)
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

            s = s.Trim();
            s = s.Trim(":\" ".ToCharArray());

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
                        @"(?<lbrace>{)|(?<rbrace>})|(?<float>(\d+\.\d+)|(?<int>\d+))|(?<name>\w+:)|[(""](?<word>[\w /]+)[)""]|(?<word>[\w/]+)|(?<lparen>\()|(?<rparen>\))")
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
}