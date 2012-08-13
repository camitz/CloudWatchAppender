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
        private readonly List<AppenderValue> _values = new List<AppenderValue>();
        private readonly List<MetricDatum> _data = new List<MetricDatum>();
        private MetricDatum _currentDatum;

        public string OverrideName { get; set; }
        public double? OverrideValue { get; set; }
        public string OverrideUnit { get; set; }
        public string OverrideNameSpace { get; set; }

        public IEnumerable<Dimension> OverrideDimensions { get; set; }

        public void Parse()
        {
            var matches =
                Regex.Matches(_renderedMessage, @"(?<name>\w+):\s*(?<value>\d+\.\d+|\d+|[\w/]+)?\s*(?<unit>\w+)").Cast<Match>().ToList();

            foreach (var m in matches)
            {
                try
                {
                    var p = new AppenderValue
                                {
                                    name = m.Groups["name"].Value,
                                    sValue = m.Groups["value"].Value,
                                    unit = m.Groups["unit"].Value
                                };

                    double d = 0.0;
                    if (Double.TryParse(m.Groups["value"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
                        p.dValue = d;

                    if (String.IsNullOrEmpty(p.name))
                        return;

                    if (!_availableNames.Any(x => x.Equals(p.name, StringComparison.InvariantCultureIgnoreCase)) &&
                        !_availableStatisticNames.Any(x => x.Equals(p.name, StringComparison.InvariantCultureIgnoreCase)))
                        return;

                    if (!String.IsNullOrEmpty(p.unit) && !_availableUnits.Any(x => x.Equals(p.unit, StringComparison.InvariantCultureIgnoreCase)))
                        return;

                    if (p.name == "Unit" && !_availableUnits.Any(x => x.Equals(p.sValue, StringComparison.InvariantCultureIgnoreCase)))
                        return;

                    _values.Add(p);
                }
                catch (FormatException) { }
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

                if (datum.Value == 0.0)
                    try
                    {
                        datum.Value = OverrideValue ?? 1;
                    }
                    catch (Exception)
                    {
                    }
            }
        }

        private void FillName(AppenderValue p)
        {
            switch (p.name)
            {
                case "Value":
                    _currentDatum.Value = OverrideValue ?? p.dValue.Value;
                    _currentDatum.Unit = OverrideUnit ?? p.unit;
                    break;

                case "Unit":
                    _currentDatum.Unit = OverrideUnit ?? p.sValue;
                    break;

                case "Name":
                case "MetricName":
                    _currentDatum.Name = OverrideName ?? p.sValue;
                    break;

                case "NameSpace":
                    _currentDatum.NameSpace = OverrideNameSpace ?? p.sValue;
                    break;

                case "Maximum":
                    _currentDatum.Maximum = p.dValue.Value;
                    _currentDatum.Unit = p.unit;
                    break;

                case "Minimum":
                    _currentDatum.Minimum = p.dValue.Value;
                    _currentDatum.Unit = p.unit;
                    break;

                case "SampleCount":
                    _currentDatum.SampleCount = p.dValue.Value;
                    _currentDatum.Unit = p.unit;
                    break;

                case "Sum":
                    _currentDatum.Sum = p.dValue.Value;
                    _currentDatum.Unit = p.unit;
                    break;

                default:
                    break;
            }
        }

        private void NewDatum()
        {
            _currentDatum = new MetricDatum();
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

        private readonly string[] _availableUnits = {
                                                        "Seconds",
                                                        "Microseconds",
                                                        "Milliseconds",
                                                        "Bytes",
                                                        "Kilobytes",
                                                        "Megabytes",
                                                        "Gigabytes",
                                                        "Terabytes",
                                                        "Bits",
                                                        "Kilobits",
                                                        "Megabits",
                                                        "Gigabits",
                                                        "Terabits",
                                                        "Percent",
                                                        "Count",
                                                        "Bytes/Second",
                                                        "Kilobytes/Second",
                                                        "Megabytes/Second",
                                                        "Gigabytes/Second",
                                                        "Terabytes/Second",
                                                        "Bits/Second",
                                                        "Kilobits/Second",
                                                        "Megabits/Second",
                                                        "Gigabits/Second",
                                                        "Terabits/Second",
                                                        "Count",
                                                        "Second",
                                                        "None"
                                                    };

        private readonly string[] _availableNames = {
                                                        "Value",
                                                        "Unit",
                                                        "Dimension",
                                                        "Dimension0","Dimension1","Dimension2","Dimension3","Dimension4","Dimension5","Dimension6","Dimension7","Dimension8","Dimension9",
                                                        "NameSpace",
                                                        "Name",
                                                        "MetricName"
                                                    };

        private readonly string[] _availableStatisticNames = {
                                                        "Maximum",
                                                        "Minimum",
                                                        "SampleCount",
                                                        "Sum"
                                                    };

        private List<MetricDatum>.Enumerator _dataEnumerator;
        private bool _initialized = false;

        public EventMessageParser(string renderedMessage)
        {
            _renderedMessage = renderedMessage;
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
            get {
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