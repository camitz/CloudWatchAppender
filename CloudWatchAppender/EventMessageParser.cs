using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.CloudWatch.Model;

namespace CloudWatchAppender
{
    public class EventMessageParser : IEnumerable<PutMetricDataRequest>, IEnumerator<PutMetricDataRequest>
    {
        private readonly string _renderedMessage;
        private readonly bool _defaultsOverridePattern;
        private readonly List<AppenderValue> _values = new List<AppenderValue>();
        private readonly Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();
        private readonly List<MetricDatum> _data = new List<MetricDatum>();
        private MetricDatum _currentDatum;

        public string DefaultName { get; set; }
        public double? DefaultValue { get; set; }
        public DateTimeOffset? DefaultTimestamp { get; set; }
        public string DefaultUnit { get; set; }
        public string DefaultNameSpace { get; set; }

        public IDictionary<string, Dimension> DefaultDimensions { get; set; }

        public double? DefaultSampleCount { get; set; }
        public double? DefaultSum { get; set; }
        public double? DefaultMaximum { get; set; }
        public double? DefaultMinimum { get; set; }

        public void Parse()
        {
            var tokens =
                Regex.Matches(_renderedMessage,
                              @"(?<float>(\d+\.\d+)|(?<int>\d+))|(?<name>\w+:)|(?<word>[\w/]+)|(?<lparen>\()|(?<rparen>\))")
                    .Cast<Match>()
                    .ToList()
                    .GetEnumerator();

            ParseTokens(tokens, _renderedMessage);

            NewDatum();
            foreach (var p in _values)
            {
                try
                {
                    FillName(p);

                }
                catch (MetricDatumFilledException)
                {
                    NewDatum();
                    FillName(p);
                }
            }

            SetDefaults();
        }

        private void SetDefaults()
        {
            foreach (var datum in _data)
            {
                //Set overrides if not already set.

                if (string.IsNullOrEmpty(datum.Name))
                    datum.Name = DefaultName ?? "CloudWatchAppender";

                if (string.IsNullOrEmpty(datum.NameSpace))
                    datum.NameSpace = DefaultNameSpace ?? "CloudWatchAppender";

                if (string.IsNullOrEmpty(datum.Unit))
                    datum.Unit = DefaultUnit ?? "Count";

                //if (!datum.Timestamp.HasValue)
                //    datum.Timestamp = DateTimeOffset.UtcNow;

                if (!datum.ValueMode && !datum.StatisticsMode)
                    datum.ValueMode = true;

                if (datum.ValueMode)
                {
                    if (datum.Value == 0.0)
                        datum.Value = DefaultValue ?? 1;
                }
                else
                {
                    if (datum.Minimum == 0.0)
                        datum.Minimum = DefaultMinimum ?? 0.0;
                    if (datum.Maximum == 0.0)
                        datum.Maximum = DefaultMaximum ?? 0.0;
                    if (datum.Sum == 0.0)
                        datum.Sum = DefaultSum ?? 0.0;
                    if (datum.SampleCount == 0)
                        datum.SampleCount = DefaultSampleCount ?? 1;
                }
            }
        }

        private void ParseTokens(List<Match>.Enumerator tokens, string renderedMessage)
        {
            string t0, unit, value, name, sNum = string.Empty;

            tokens.MoveNext();
            while (tokens.Current != null)
            {
                if (!string.IsNullOrEmpty(t0 = tokens.Current.Groups["name"].Value.Split(new[] { ':' })[0]))
                {
                    if (!MetricDatum.SupportedNames.Any(x => x.Equals(t0, StringComparison.InvariantCultureIgnoreCase)) &&
                        !MetricDatum.SupportedStatistics.Any(x => x.Equals(t0, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        tokens.MoveNext();
                        continue;
                    }

                    if (t0.StartsWith("Timestamp", StringComparison.InvariantCultureIgnoreCase))
                    {
                        DateTimeOffset time;
                        if(ExtractTime(renderedMessage.Substring(tokens.Current.Index + "Timestamp".Length), out time))
                            _values.Add(new AppenderValue
                                            {
                                                Name = "Timestamp",
                                                Time = time
                                            });

                        tokens.MoveNext();
                    }
                    else if (t0.StartsWith("Dimension", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!tokens.MoveNext())
                            continue;

                        if (!string.IsNullOrEmpty(tokens.Current.Groups["lparen"].Value))
                        {
                            tokens.MoveNext();

                            while (tokens.Current != null &&
                                   string.IsNullOrEmpty(tokens.Current.Groups["rparen"].Value))
                            {
                                if (
                                    string.IsNullOrEmpty(
                                        name = tokens.Current.Groups["name"].Value.Split(new[] { ':' })[0]))
                                {
                                    tokens.MoveNext();
                                    continue;
                                }

                                if (!tokens.MoveNext())
                                    continue;

                                if (string.IsNullOrEmpty(value = tokens.Current.Groups["word"].Value) &&
                                    string.IsNullOrEmpty(sNum = tokens.Current.Groups["float"].Value))
                                {
                                    tokens.MoveNext();
                                    continue;
                                }

                                _dimensions[name] = new Dimension { Name = name, Value = string.IsNullOrEmpty(sNum) ? value : sNum };
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(tokens.Current.Groups["lparen"].Value))
                                tokens.MoveNext();

                            if (
                                string.IsNullOrEmpty(
                                    name = tokens.Current.Groups["name"].Value.Split(new[] { ':' })[0]))
                            {
                                tokens.MoveNext();
                                continue;
                            }

                            if (!tokens.MoveNext())
                                continue;

                            if (string.IsNullOrEmpty(value = tokens.Current.Groups["word"].Value))
                            {
                                tokens.MoveNext();
                                continue;
                            }

                            _dimensions[name] = new Dimension { Name = name, Value = string.IsNullOrEmpty(sNum) ? value : sNum };
                        }
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
                                if (
                                    MetricDatum.SupportedUnits.Any(
                                        x => x.Equals(unit, StringComparison.InvariantCultureIgnoreCase)))
                                    v.Unit = unit;

                        _values.Add(v);
                    }
                }
                else
                    tokens.MoveNext();
            }
        }

        private bool ExtractTime(string s, out DateTimeOffset time)
        {
            var success = false;
            DateTimeOffset lastTriedTime;

            time = DateTimeOffset.UtcNow;

            s = s.Trim();
            s = s.Trim(new[] {':'});

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


        private void FillName(AppenderValue p)
        {
            switch (p.Name)
            {
                case "Value":
                    _currentDatum.Value = _defaultsOverridePattern ? DefaultValue ?? p.dValue.Value : p.dValue.Value;
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? p.Unit : p.Unit;
                    break;

                case "Unit":
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? p.sValue : p.sValue;
                    break;

                case "Name":
                case "MetricName":
                    _currentDatum.Name = _defaultsOverridePattern ? DefaultName ?? p.sValue : p.sValue;
                    break;

                case "NameSpace":
                    _currentDatum.NameSpace = _defaultsOverridePattern ? DefaultNameSpace ?? p.sValue : p.sValue;
                    break;

                case "Maximum":
                    _currentDatum.Maximum = _defaultsOverridePattern ? DefaultMaximum ?? p.dValue.Value : p.dValue.Value;
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? p.Unit : p.Unit;
                    break;

                case "Minimum":
                    _currentDatum.Minimum = _defaultsOverridePattern ? DefaultMinimum ?? p.dValue.Value : p.dValue.Value;
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? p.Unit : p.Unit;
                    break;

                case "SampleCount":
                    _currentDatum.SampleCount = _defaultsOverridePattern ? DefaultSampleCount ?? p.dValue.Value : p.dValue.Value;
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? p.Unit : p.Unit;
                    break;

                case "Sum":
                    _currentDatum.Sum = _defaultsOverridePattern ? DefaultSum ?? p.dValue.Value : p.dValue.Value;
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? p.Unit : p.Unit;
                    break;

                case "Timestamp":
                    _currentDatum.Timestamp = _defaultsOverridePattern ? DefaultTimestamp ?? p.Time.Value : p.Time.Value;
                    break;
            }
        }

        private void NewDatum()
        {
            var dimensions = DefaultDimensions ?? _dimensions;

            foreach (var dimension in _dimensions.Values)
            {
                if (dimensions.ContainsKey(dimension.Name))
                {
                    if (!_defaultsOverridePattern)
                        dimensions[dimension.Name] = dimension;
                }
                else
                    dimensions[dimension.Name] = dimension;
            }

            _currentDatum = new MetricDatum
                                {
                                    Dimensions = dimensions.Values.ToList(),
                                    Unit = DefaultUnit
                                };

            _data.Add(_currentDatum);
        }

        private struct AppenderValue
        {
            public string Name;
            public double? dValue;
            public string Unit;
            public string sValue;
            public DateTimeOffset? Time;
        }


        private List<MetricDatum>.Enumerator _dataEnumerator;
        private bool _initialized;

        public EventMessageParser(string renderedMessage, bool useOverrides = true)
        {
            _renderedMessage = renderedMessage;
            _defaultsOverridePattern = useOverrides;
        }

        public IEnumerator<PutMetricDataRequest> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {

        }

        public bool MoveNext()
        {
            if (!_initialized)
                Reset();

            return _dataEnumerator.MoveNext();
        }

        public void Reset()
        {
            _dataEnumerator = _data.GetEnumerator();
            _initialized = true;
        }

        public PutMetricDataRequest Current
        {
            get
            {
                var r = _dataEnumerator.Current.Request;
                r.MetricData.Add(_dataEnumerator.Current.AWSDatum); //Todo: if namespace is the same we can just, add to the list. Needs ordering of namespace etc.
                return r;
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}