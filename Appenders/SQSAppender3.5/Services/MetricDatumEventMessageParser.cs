using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Model;
using CloudWatchAppender.Parsers;
using MetricDatum = CloudWatchAppender.Model.MetricDatum;

namespace CloudWatchAppender.Services
{
    public class MetricDatumEventMessageParser : EventMessageParserBase
    {
        private readonly bool _defaultsOverridePattern;
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

        protected override void SetDefaults()
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

        protected override bool IsSupportedName(string t0)
        {
            return MetricDatum.SupportedNames.Any(x => x.Equals(t0, StringComparison.InvariantCultureIgnoreCase)) ||
                   MetricDatum.SupportedStatistics.Any(x => x.Equals(t0, StringComparison.InvariantCultureIgnoreCase));
        }


        protected override bool FillName(AppenderValue value)
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

        protected override void NewDatum()
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

    


        public MetricDatumEventMessageParser(string renderedMessage, bool useOverrides = true):base(renderedMessage,useOverrides)
        {
            _defaultsOverridePattern = useOverrides;
        }





        protected override void LocalParse(ref List<Match>.Enumerator tokens, string sNum)
        {
            if (!tokens.MoveNext())
                return;

            string name,value;
            if (!string.IsNullOrEmpty(tokens.Current.Groups["lparen"].Value))
            {
                tokens.MoveNext();

                while (tokens.Current != null &&
                       string.IsNullOrEmpty(tokens.Current.Groups["rparen"].Value))
                {
                    if (
                        string.IsNullOrEmpty(
                            name = tokens.Current.Groups["name"].Value.Split(new[] {':'})[0]))
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

                    _dimensions[name] = new Dimension {Name = name, Value = string.IsNullOrEmpty(sNum) ? value : sNum};
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(tokens.Current.Groups["lparen"].Value))
                    tokens.MoveNext();

                if (
                    string.IsNullOrEmpty(
                        name = tokens.Current.Groups["name"].Value.Split(new[] {':'})[0]))
                {
                    tokens.MoveNext();
                    return;
                }

                if (!tokens.MoveNext())
                    return;

                if (string.IsNullOrEmpty(value = tokens.Current.Groups["word"].Value))
                {
                    tokens.MoveNext();
                    return;
                }

                _dimensions[name] = new Dimension {Name = name, Value = string.IsNullOrEmpty(sNum) ? value : sNum};
            }
        }

        public IEnumerable<PutMetricDataRequest> GetParsedData()
        {
            return _data.Select(x => x.Request);
        }
    }
}