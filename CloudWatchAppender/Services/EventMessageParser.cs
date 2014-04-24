using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Model;
using log4net.Util;
using MetricDatum = CloudWatchAppender.Model.MetricDatum;

namespace CloudWatchAppender.Services
{
    public class EventMessageParser
    {
        private readonly string _renderedMessage;
        private readonly bool _defaultsOverridePattern;
        private readonly List<AppenderValue> _values = new List<AppenderValue>();
        private readonly Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();
        private readonly List<MetricDatum> _data = new List<MetricDatum>();
        private MetricDatum _currentDatum;

        public string DefaultMetricName { get; set; }
        public double? DefaultValue { get; set; }
        public DateTimeOffset? DefaultTimestamp { get; set; }
        public StandardUnit DefaultUnit { get; set; }
        public string DefaultNameSpace { get; set; }

        public IDictionary<string, Dimension> DefaultDimensions { get; set; }

        public double? DefaultSampleCount { get; set; }
        public double? DefaultSum { get; set; }
        public double? DefaultMaximum { get; set; }
        public double? DefaultMinimum { get; set; }

        public void Parse()
        {
            if (!string.IsNullOrEmpty(_renderedMessage))
            {

                var tokens =
                    Regex.Matches(_renderedMessage,
                                  @"(?<float>(\d+\.\d+)|(?<int>\d+))|(?<name>\w+:)|(?<word>[\w/]+)|(?<lparen>\()|(?<rparen>\))")
                        .Cast<Match>()
                        .ToList()
                        .GetEnumerator();

                ParseTokens(tokens, _renderedMessage);
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

                if (string.IsNullOrEmpty(datum.MetricName))
                    datum.MetricName = DefaultMetricName ?? "CloudWatchAppender";

                if (string.IsNullOrEmpty(datum.NameSpace))
                    datum.NameSpace = DefaultNameSpace ?? "CloudWatchAppender";

                if (string.IsNullOrEmpty(datum.Unit))
                    datum.Unit = DefaultUnit ?? "Count";

                //if (!datum.Timestamp.HasValue)
                //    datum.Timestamp = DateTimeOffset.UtcNow;

                if (!datum.Mode.HasValue)
                    datum.Mode = DatumMode.ValueMode;

                if (datum.Mode == DatumMode.ValueMode)
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
                        if (ExtractTime(renderedMessage.Substring(tokens.Current.Index + "Timestamp".Length), out time))
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
                            {
                                v.Unit = unit;
                            }

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


        private bool FillName(AppenderValue value)
        {
            switch (value.Name.ToLowerInvariant())
            {
                case "value":
                    if (_currentDatum.Value != 0.0)
                        return false;

                    _currentDatum.Value = _defaultsOverridePattern ? DefaultValue ?? value.dValue.Value : value.dValue.Value;
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? value.Unit : value.Unit;
                    break;

                case "unit":
                    if (_currentDatum.Unit != null)
                        return false;

                    if (_defaultsOverridePattern) _currentDatum.Unit = DefaultUnit ?? value.sValue;
                    else _currentDatum.Unit = value.sValue;
                    break;

                case "metricname":
                    if (!string.IsNullOrEmpty(_currentDatum.MetricName))
                        return false;

                    _currentDatum.MetricName = _defaultsOverridePattern ? DefaultMetricName ?? value.sValue : value.sValue;
                    break;

                case "namespace":
                    if (!string.IsNullOrEmpty(_currentDatum.NameSpace))
                        return false;

                    _currentDatum.NameSpace = _defaultsOverridePattern ? DefaultNameSpace ?? value.sValue : value.sValue;
                    break;

                case "maximum":
                    if (_currentDatum.Mode == DatumMode.StatisticsMode && _currentDatum.Maximum != 0.0)
                        return false;

                    _currentDatum.Maximum = _defaultsOverridePattern ? DefaultMaximum ?? value.dValue.Value : value.dValue.Value;
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? value.Unit : value.Unit;
                    break;

                case "minimum":
                    if (_currentDatum.Mode == DatumMode.StatisticsMode && _currentDatum.Minimum != 0.0)
                        return false;
                    _currentDatum.Minimum = _defaultsOverridePattern ? DefaultMinimum ?? value.dValue.Value : value.dValue.Value;
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? value.Unit : value.Unit;
                    break;

                case "samplecount":
                    if (_currentDatum.Mode == DatumMode.StatisticsMode && _currentDatum.SampleCount != 0.0)
                        return false;

                    _currentDatum.SampleCount = _defaultsOverridePattern ? DefaultSampleCount ?? value.dValue.Value : value.dValue.Value;
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? value.Unit : value.Unit;
                    break;

                case "sum":
                    if (_currentDatum.Mode == DatumMode.StatisticsMode && _currentDatum.Sum != 0.0)
                        return false;

                    _currentDatum.Sum = _defaultsOverridePattern ? DefaultSum ?? value.dValue.Value : value.dValue.Value;
                    _currentDatum.Unit = _defaultsOverridePattern ? DefaultUnit ?? value.Unit : value.Unit;
                    break;

                case "timestamp":
                    if (_currentDatum.Timestamp.HasValue)
                        return false;

                    _currentDatum.Timestamp = _defaultsOverridePattern ? DefaultTimestamp ?? value.Time.Value : value.Time.Value;
                    break;
            }

            return true;
        }

        private void NewDatum()
        {
            var dimensions = DefaultDimensions ?? _dimensions;

            foreach (var dimension in _dimensions.Values.ToArray())
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
                                    Dimensions = dimensions.Any() ?
                                        dimensions.Values.Where(x => !string.IsNullOrEmpty(x.Value)).ToList() :
                                        new List<Dimension>()
                                };

            _data.Add(_currentDatum);
        }

        private struct AppenderValue
        {
            public string Name;
            public double? dValue;
            public StandardUnit Unit;
            public string sValue;
            public DateTimeOffset? Time;
        }



        public EventMessageParser(string renderedMessage, bool useOverrides = true)
        {
            _renderedMessage = renderedMessage;
            _defaultsOverridePattern = useOverrides;
        }




        public IEnumerable<PutMetricDataRequest> GetMetricDataRequests()
        {
            return _data.Select(x => x.Request);
        }
    }
}