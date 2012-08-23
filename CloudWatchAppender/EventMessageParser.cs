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
        private readonly bool _useOverrides;
        private readonly List<AppenderValue> _values = new List<AppenderValue>();
        private readonly Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();
        private readonly List<MetricDatum> _data = new List<MetricDatum>();
        private MetricDatum _currentDatum;

        public string OverrideName { get; set; }
        public double? OverrideValue { get; set; }
        public string OverrideUnit { get; set; }
        public string OverrideNameSpace { get; set; }

        public IDictionary<string, Dimension> OverrideDimensions { get; set; }

        public double? OverrideSampleCount { get; set; }
        public double? OverrideSum { get; set; }
        public double? OverrideMaximum { get; set; }
        public double? OverrideMinimum { get; set; }

        public void Parse()
        {
            var tokens =
                Regex.Matches(_renderedMessage,
                              @"(?<float>(\d+\.\d+)|(?<int>\d+))|(?<name>\w+:)|(?<word>[\w/]+)|(?<lparen>\()|(?<rparen>\))")
                    .Cast<Match>()
                    .ToList()
                    .GetEnumerator();

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

                    if (t0.StartsWith("Dimension", StringComparison.InvariantCultureIgnoreCase))
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

                                if (string.IsNullOrEmpty(value = tokens.Current.Groups["word"].Value) && string.IsNullOrEmpty(sNum = tokens.Current.Groups["float"].Value))
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

                        sNum = string.IsNullOrEmpty(tokens.Current.Groups["float"].Value) ? tokens.Current.Groups["int"].Value : tokens.Current.Groups["float"].Value;

                        var sValue = tokens.Current.Groups["word"].Value;

                        if (string.IsNullOrEmpty(sNum) && string.IsNullOrEmpty(sValue))
                        {
                            tokens.MoveNext();
                            continue;
                        }

                        var d = 0.0;
                        if (!Double.TryParse(sNum, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d) && string.IsNullOrEmpty(sValue))
                        {
                            tokens.MoveNext();
                            continue;
                        }

                        var v = new AppenderValue
                                    {
                                        dValue = d,
                                        sValue = string.IsNullOrEmpty(sValue) ? sNum : sValue,
                                        name = t0
                                    };

                        if (tokens.MoveNext())
                            if (!string.IsNullOrEmpty(unit = tokens.Current.Groups["word"].Value))
                                if (MetricDatum.SupportedUnits.Any(x => x.Equals(unit, StringComparison.InvariantCultureIgnoreCase)))
                                    v.unit = unit;

                        _values.Add(v);
                    }
                }
                else
                    tokens.MoveNext();
            }

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

            foreach (var datum in _data)
            {
                //Set overrides if not already set.

                if (string.IsNullOrEmpty(datum.Name))
                    datum.Name = OverrideName ?? "CloudWatchAppender";

                if (string.IsNullOrEmpty(datum.NameSpace))
                    datum.NameSpace = OverrideNameSpace ?? "CloudWatchAppender";

                if (string.IsNullOrEmpty(datum.Unit))
                    datum.Unit = OverrideUnit ?? "Count";

                if (!datum.ValueMode && !datum.StatisticsMode)
                    datum.ValueMode = true;

                if (datum.ValueMode)
                {
                    if (datum.Value == 0.0)
                        datum.Value = OverrideValue ?? 1;
                }
                else
                {
                    if (datum.Minimum == 0.0)
                        datum.Minimum = OverrideMinimum ?? 0.0;
                    if (datum.Maximum == 0.0)
                        datum.Maximum = OverrideMaximum ?? 0.0;
                    if (datum.Sum == 0.0)
                        datum.Sum = OverrideSum ?? 0.0;
                    if (datum.SampleCount == 0)
                        datum.SampleCount = OverrideSampleCount ?? 1;
                }
            }
        }


        private AppenderValue? GetValueFromMatch(Match m)
        {

            var p = new AppenderValue
                    {
                        name = m.Groups["name"].Value,
                        sValue = m.Groups["value"].Value,
                        unit = m.Groups["unit"].Value
                    };

            try
            {

                var d = 0.0;
                if (Double.TryParse(m.Groups["value"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
                    p.dValue = d;

                if (String.IsNullOrEmpty(p.name))
                    return null;

                if (!MetricDatum.SupportedNames.Any(x => x.Equals(p.name, StringComparison.InvariantCultureIgnoreCase)) &&
                    !MetricDatum.SupportedStatistics.Any(x => x.Equals(p.name, StringComparison.InvariantCultureIgnoreCase)))
                    return null;

                if (!String.IsNullOrEmpty(p.unit) &&
                    !MetricDatum.SupportedUnits.Any(x => x.Equals(p.unit, StringComparison.InvariantCultureIgnoreCase)))

                    return null;

                if (p.name == "Unit" && !MetricDatum.SupportedUnits.Any(x => x.Equals(p.sValue, StringComparison.InvariantCultureIgnoreCase)))
                    return null;
            }
            catch (FormatException)
            {
                return null;
            }

            return p;
        }

        private void FillName(AppenderValue p)
        {
            switch (p.name)
            {
                case "Value":
                    _currentDatum.Value = _useOverrides ? OverrideValue ?? p.dValue.Value : p.dValue.Value;
                    _currentDatum.Unit = _useOverrides ? OverrideUnit ?? p.unit : p.unit;
                    break;

                case "Unit":
                    _currentDatum.Unit = _useOverrides ? OverrideUnit ?? p.sValue : p.sValue;
                    break;

                case "Name":
                case "MetricName":
                    _currentDatum.Name = _useOverrides ? OverrideName ?? p.sValue : p.sValue;
                    break;

                case "NameSpace":
                    _currentDatum.NameSpace = _useOverrides ? OverrideNameSpace ?? p.sValue : p.sValue;
                    break;

                case "Maximum":
                    _currentDatum.Maximum = _useOverrides ? OverrideMaximum ?? p.dValue.Value : p.dValue.Value;
                    _currentDatum.Unit = _useOverrides ? OverrideUnit ?? p.unit : p.unit;
                    break;

                case "Minimum":
                    _currentDatum.Minimum = _useOverrides ? OverrideMinimum ?? p.dValue.Value : p.dValue.Value;
                    _currentDatum.Unit = _useOverrides ? OverrideUnit ?? p.unit : p.unit;
                    break;

                case "SampleCount":
                    _currentDatum.SampleCount = _useOverrides ? OverrideSampleCount ?? p.dValue.Value : p.dValue.Value;
                    _currentDatum.Unit = _useOverrides ? OverrideUnit ?? p.unit : p.unit;
                    break;

                case "Sum":
                    _currentDatum.Sum = _useOverrides ? OverrideSum ?? p.dValue.Value : p.dValue.Value;
                    _currentDatum.Unit = _useOverrides ? OverrideUnit ?? p.unit : p.unit;
                    break;
            }
        }

        private void NewDatum()
        {
            var dimensions = OverrideDimensions ?? _dimensions;

            foreach (var dimension in _dimensions.Values)
            {
                if (dimensions[dimension.Name] != null)
                {
                    if (!_useOverrides)
                        dimensions[dimension.Name] = dimension;
                } else
                    dimensions[dimension.Name] = dimension;
            }

            _currentDatum = new MetricDatum
                                {
                                    Dimensions = dimensions.Values.ToList(),
                                    Unit = OverrideUnit
                                };

            _data.Add(_currentDatum);
        }

        public struct AppenderValue
        {
            public string name;
            public double? dValue;
            public string unit;
            public string sValue;
        }

        private IEnumerable<MetricDatum> GetData()
        {
            return _data;
        }



        private List<MetricDatum>.Enumerator _dataEnumerator;
        private bool _initialized;

        public EventMessageParser(string renderedMessage, bool useOverrides = true)
        {
            _renderedMessage = renderedMessage;
            _useOverrides = useOverrides;
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
                r.MetricData.Add(_dataEnumerator.Current.Datum); //Todo: if namespace is the same we can just, add to the list. Needs ordering of namespace etc.
                return r;
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}