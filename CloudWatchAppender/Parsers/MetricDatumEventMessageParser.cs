using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Model;
using MetricDatum = CloudWatchAppender.Model.MetricDatum;

namespace CloudWatchAppender.Parsers
{
    public class MetricDatumEventMessageParser : EventMessageParserBase<PutMetricDataRequest>
    {
        private Dictionary<string, Dimension> _dimensions;
        private List<MetricDatum> _data;
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
        public new bool ConfigOverrides{ get { return base.ConfigOverrides; }set { base.ConfigOverrides = value; } }

        public MetricDatumEventMessageParser() : base(true) { }
        public MetricDatumEventMessageParser(bool useOverrides)
            : base(useOverrides)
        {
            DefaultsOverridePattern = useOverrides;
        }


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

                    _currentDatum.Value = DefaultsOverridePattern ? DefaultValue ?? value.dValue.Value : value.dValue.Value;
                    _currentDatum.Unit = DefaultsOverridePattern ? DefaultUnit ?? value.Unit : value.Unit;
                    break;

                case "unit":
                    if (_currentDatum.Unit != null)
                        return false;

                    if (DefaultsOverridePattern) _currentDatum.Unit = DefaultUnit ?? value.sValue;
                    else _currentDatum.Unit = value.sValue;
                    break;

                case "metricname":
                    if (!string.IsNullOrEmpty(_currentDatum.MetricName))
                        return false;

                    _currentDatum.MetricName = DefaultsOverridePattern ? DefaultMetricName ?? value.sValue : value.sValue;
                    break;

                case "namespace":
                    if (!string.IsNullOrEmpty(_currentDatum.NameSpace))
                        return false;

                    _currentDatum.NameSpace = DefaultsOverridePattern ? DefaultNameSpace ?? value.sValue : value.sValue;
                    break;

                case "maximum":
                    if (_currentDatum.Mode == DatumMode.StatisticsMode && _currentDatum.Maximum != 0.0)
                        return false;

                    _currentDatum.Maximum = DefaultsOverridePattern ? DefaultMaximum ?? value.dValue.Value : value.dValue.Value;
                    _currentDatum.Unit = DefaultsOverridePattern ? DefaultUnit ?? value.Unit : value.Unit;
                    break;

                case "minimum":
                    if (_currentDatum.Mode == DatumMode.StatisticsMode && _currentDatum.Minimum != 0.0)
                        return false;
                    _currentDatum.Minimum = DefaultsOverridePattern ? DefaultMinimum ?? value.dValue.Value : value.dValue.Value;
                    _currentDatum.Unit = DefaultsOverridePattern ? DefaultUnit ?? value.Unit : value.Unit;
                    break;

                case "samplecount":
                    if (_currentDatum.Mode == DatumMode.StatisticsMode && _currentDatum.SampleCount != 0.0)
                        return false;

                    _currentDatum.SampleCount = DefaultsOverridePattern ? DefaultSampleCount ?? value.dValue.Value : value.dValue.Value;
                    _currentDatum.Unit = DefaultsOverridePattern ? DefaultUnit ?? value.Unit : value.Unit;
                    break;

                case "sum":
                    if (_currentDatum.Mode == DatumMode.StatisticsMode && _currentDatum.Sum != 0.0)
                        return false;

                    _currentDatum.Sum = DefaultsOverridePattern ? DefaultSum ?? value.dValue.Value : value.dValue.Value;
                    _currentDatum.Unit = DefaultsOverridePattern ? DefaultUnit ?? value.Unit : value.Unit;
                    break;

                case "timestamp":
                    if (_currentDatum.Timestamp.HasValue)
                        return false;

                    _currentDatum.Timestamp = DefaultsOverridePattern ? DefaultTimestamp ?? value.Time.Value : value.Time.Value;
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
                    if (!DefaultsOverridePattern)
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







        protected override bool ShouldLocalParse(string t0)
        {
            return t0.StartsWith("Dimension", StringComparison.InvariantCultureIgnoreCase);
        }

        protected override void LocalParse(ref List<Match>.Enumerator tokens, string sNum)
        {
            if (!tokens.MoveNext())
                return;

            string name, value;
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
                    return;
                }

                if (!tokens.MoveNext())
                    return;

                if (string.IsNullOrEmpty(value = tokens.Current.Groups["word"].Value))
                {
                    tokens.MoveNext();
                    return;
                }

                _dimensions[name] = new Dimension { Name = name, Value = string.IsNullOrEmpty(sNum) ? value : sNum };
            }
        }

        protected override void Init()
        {
            base.Init();
            _dimensions = new Dictionary<string, Dimension>();
            _data = new List<MetricDatum>();
            _currentDatum = null;
        }

        protected override IEnumerable<PutMetricDataRequest> GetParsedData()
        {
            return _data.Select(x => x.Request);
        }
    }
}