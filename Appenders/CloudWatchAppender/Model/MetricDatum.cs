using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using log4net.Util;

namespace AWSAppender.CloudWatch.Model
{
    public class MetricDatum
    {
        readonly PutMetricDataRequest _request = new PutMetricDataRequest();

        private DatumMode? _mode;

        private DateTimeOffset? _timestamp;

        public double? Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (Mode == DatumMode.StatisticsMode)
                    throw new DatumFilledException("Value cannot be set since we're in statistics mode.");

                _mode = DatumMode.ValueMode;
                _value = value;
            }
        }

        public StandardUnit Unit
        {
            get { return _unit; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (!string.IsNullOrEmpty(_unit) && _unit != value)
                        throw new DatumFilledException("Unit has been set already.");

                    _unit = value;

                    if (_unit != value)
                        LogLog.Warn(typeof(MetricDatum), string.Format("Unit {0} not supported. Using default.", value));
                }
            }
        }

        public string MetricName
        {
            get { return _metricName; }
            set
            {
                if (!string.IsNullOrEmpty(_metricName))
                    throw new DatumFilledException("MetricName has been set already.");

                _metricName = value;
            }
        }

        public string NameSpace
        {
            get { return _request.Namespace; }
            set
            {
                if (!string.IsNullOrEmpty(_request.Namespace))
                    throw new DatumFilledException("NameSpace has been set already.");

                _request.Namespace = value.Replace(".", "/");
            }
        }

        public double Maximum
        {
            get { return _statisticValues.Maximum; }
            set
            {
                if (Mode == DatumMode.ValueMode)
                    throw new DatumFilledException("Statistics cannot be set since we're in value mode.");

                _mode = DatumMode.StatisticsMode;
                if (_statisticValues == null)
                    _statisticValues = new StatisticSet();

                _statisticValues.Maximum = value;
            }
        }

        public double Minimum
        {
            get { return _statisticValues.Minimum; }
            set
            {
                if (Mode == DatumMode.ValueMode)
                    throw new DatumFilledException("Statistics cannot be set since we're in value mode.");

                _mode = DatumMode.StatisticsMode;
                if (_statisticValues == null)
                    _statisticValues = new StatisticSet();

                _statisticValues.Minimum = value;
            }
        }

        public double Sum
        {
            get { return _statisticValues.Sum; }
            set
            {
                if (Mode == DatumMode.ValueMode)
                    throw new DatumFilledException("Statistics cannot be set since we're in value mode.");

                _mode = DatumMode.StatisticsMode;
                if (_statisticValues == null)
                    _statisticValues = new StatisticSet();

                _statisticValues.Sum = value;
            }
        }

        public double SampleCount
        {
            get { return _statisticValues.SampleCount; }
            set
            {
                if (Mode == DatumMode.ValueMode)
                    throw new DatumFilledException("Statistics cannot be set since we're in value mode.");

                _mode = DatumMode.StatisticsMode;
                if (_statisticValues == null)
                    _statisticValues = new StatisticSet();

                _statisticValues.SampleCount = value;
            }
        }

        public DateTimeOffset? Timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                if (_timestamp.HasValue)
                    throw new DatumFilledException("Value has been set already.");

                _timestamp = value.Value;
            }
        }

        internal PutMetricDataRequest Request
        {
            get
            {
                if (!_request.MetricData.Any())
                {
                    _request.MetricData.Add(AWSDatum);
                }
                return _request;
            }
        }

        internal Amazon.CloudWatch.Model.MetricDatum AWSDatum
        {
            get
            {
                if (Mode == DatumMode.StatisticsMode)
                {
                    return new Amazon.CloudWatch.Model.MetricDatum
                    {
                        StatisticValues = _statisticValues,
                        MetricName = _metricName,
                        Dimensions = _dimensions,
                        Timestamp = _timestamp.HasValue ? _timestamp.Value.UtcDateTime : DateTime.UtcNow,
                        Unit = _unit
                    };
                }

                return new Amazon.CloudWatch.Model.MetricDatum
                       {
                           Value = _value ?? 0,
                           Unit = _unit,
                           MetricName = _metricName,
                           Dimensions = _dimensions,
                           Timestamp = _timestamp.HasValue ? _timestamp.Value.UtcDateTime : DateTime.UtcNow
                       };
            }
        }

        public List<Dimension> Dimensions
        {
            get { return _dimensions ?? (_dimensions = new List<Dimension>()); }
            set
            {
                if (_dimensions != null && _dimensions.Any())
                    throw new DatumFilledException("Value has been set already.");

                _dimensions = value;
            }
        }

        public StatisticSet StatisticValues
        {
            get { return _statisticValues; }
            set
            {
                if (Mode == DatumMode.ValueMode)
                    throw new DatumFilledException("Statistics cannot be set since we're in value mode.");

                _mode = DatumMode.StatisticsMode;
                if (_statisticValues == null)
                    _statisticValues = new StatisticSet();

                _statisticValues = value;
            }
        }



        public static readonly HashSet<string> SupportedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                                    {
                                                        "Value",
                                                        "Unit",
                                                        "Dimension",
                                                        "Dimensions",
                                                        "NameSpace",
                                                        "MetricName",
                                                        "Timestamp"
                                                    };

        public static readonly HashSet<string> SupportedStatistics = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                                    {
                                                        "Maximum",
                                                        "Minimum",
                                                        "SampleCount",
                                                        "Sum"
                                                    };

        private double? _value;
        private StandardUnit _unit;
        private string _metricName;
        private StatisticSet _statisticValues;
        private List<Dimension> _dimensions;

        public string Message { get; set; }

        public DatumMode? Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        public MetricDatum(string s)
        {
            Message = s;
        }

        public MetricDatum()
        {
        }

        [Obsolete("Deprecated")]
        public MetricDatum WithNameSpace(string value)
        {
            NameSpace = value;
            return this;
        }

        [Obsolete("Deprecated")]
        public MetricDatum WithMetricName(string value)
        {
            _metricName = value;
            return this;
        }

        [Obsolete("Deprecated")]
        public MetricDatum WithUnit(StandardUnit value)
        {
            _unit = value;
            return this;
        }

        [Obsolete("Deprecated")]
        public MetricDatum WithValue(double value)
        {
            _value = value;
            return this;
        }

        [Obsolete("Deprecated")]
        public MetricDatum WithTimestamp(DateTime value)
        {
            _timestamp = value;
            return this;
        }

        [Obsolete("Deprecated")]
        public MetricDatum WithTimestamp(DateTimeOffset value)
        {
            _timestamp = value.UtcDateTime;
            return this;
        }

        [Obsolete("Deprecated")]
        public MetricDatum WithDimensions(IEnumerable<Dimension> value)
        {
            _dimensions = value.ToList();
            return this;
        }

        [Obsolete("Deprecated")]
        public MetricDatum WithStatisticValues(StatisticSet statisticSet)
        {
            _statisticValues = statisticSet;
            _mode = DatumMode.StatisticsMode;
            return this;
        }

        public override string ToString()
        {
            var s = new StringWriter();
            new MetricDatumRenderer().RenderObject(null, this, s);

            return "MetricDatum, NameSpace: " + NameSpace + ", " + s;
        }
    }

    public enum DatumMode
    {
        StatisticsMode,
        ValueMode
    }

    public class DatumFilledException : InvalidOperationException
    {
        public DatumFilledException(string message)
            : base(message)
        {

        }
    }
}