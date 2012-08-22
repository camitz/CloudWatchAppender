using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch.Model;

namespace CloudWatchAppender
{
    public class MetricDatum
    {
        Amazon.CloudWatch.Model.MetricDatum _datum = new Amazon.CloudWatch.Model.MetricDatum();
        PutMetricDataRequest _request = new PutMetricDataRequest();

        private double? _value;
        private double? _sum;
        private double? _max;
        private double? _min;
        private double? _sampleCount;

        private bool _statisticsMode;
        private bool _valueMode;

        public double Value
        {
            get
            {
                return _datum.Value;
            }
            set
            {
                if (_value.HasValue)
                    throw new MetricDatumFilledException("Value has been set already.");

                if (StatisticsMode)
                    throw new MetricDatumFilledException("Value cannot be set since we're in statistics mode.");

                ValueMode = true;
                _value = value;
                _datum.Value = value;
            }
        }

        public string Unit
        {
            get { return _datum.Unit; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (!string.IsNullOrEmpty(_datum.Unit) && _datum.Unit != value)
                        throw new MetricDatumFilledException("Value has been set already.");

                    _datum.Unit = SupportedUnits.SingleOrDefault(x => x.Equals(value, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        public string Name
        {
            get { return _datum.MetricName; }
            set
            {
                if (!string.IsNullOrEmpty(_datum.MetricName))
                    throw new MetricDatumFilledException("Value has been set already.");

                _datum.MetricName = value;
            }
        }

        public string NameSpace
        {
            get { return _request.Namespace; }
            set
            {
                if (!string.IsNullOrEmpty(_request.Namespace))
                    throw new MetricDatumFilledException("Value has been set already.");

                _request.Namespace = value.Replace(".", "/");
            }
        }

        public double Maximum
        {
            get { return _datum.StatisticValues.Maximum; }
            set
            {
                if (_max.HasValue)
                    throw new MetricDatumFilledException("Value has been set already.");

                if (ValueMode)
                    throw new MetricDatumFilledException("Statistics cannot be set since we're in value mode.");

                _max = value;

                if (_datum.StatisticValues == null)
                    _datum.StatisticValues = new StatisticSet();

                _datum.StatisticValues.Maximum = value;
            }
        }

        public double Minimum
        {
            get { return _datum.StatisticValues.Minimum; }
            set
            {
                if (_min.HasValue)
                    throw new MetricDatumFilledException("Value has been set already.");

                if (ValueMode)
                    throw new MetricDatumFilledException("Statistics cannot be set since we're in value mode.");

                _min = value;

                if (_datum.StatisticValues == null)
                    _datum.StatisticValues = new StatisticSet();

                _datum.StatisticValues.Minimum = value;
            }
        }

        public double Sum
        {
            get { return _datum.StatisticValues.Sum; }
            set
            {
                if (_sum.HasValue)
                    throw new MetricDatumFilledException("Value has been set already.");

                if (ValueMode)
                    throw new MetricDatumFilledException("Statistics cannot be set since we're in value mode.");

                _sum = value;

                if (_datum.StatisticValues == null)
                    _datum.StatisticValues = new StatisticSet();

                _datum.StatisticValues.Sum = value;
            }
        }
        public double SampleCount
        {
            get { return _datum.StatisticValues.SampleCount; }
            set
            {
                if (_sampleCount.HasValue)
                    throw new MetricDatumFilledException("Value has been set already.");

                if (ValueMode)
                    throw new MetricDatumFilledException("Statistics cannot be set since we're in value mode.");

                _sampleCount = value;

                if (_datum.StatisticValues == null)
                    _datum.StatisticValues = new StatisticSet();

                _datum.StatisticValues.SampleCount = value;
            }
        }

        internal PutMetricDataRequest Request
        {
            get { return _request; }
        }

        internal Amazon.CloudWatch.Model.MetricDatum Datum
        {
            get { return _datum; }
        }

        public List<Dimension> Dimensions 
        {
            get { return Datum.Dimensions; }
            set
            {
                if (Datum.Dimensions.Count != 0)
                    throw new MetricDatumFilledException("Value has been set already.");

                Datum.Dimensions = value;
            }
        }

        public bool StatisticsMode
        {
            get { return _statisticsMode; }
        }

        public bool ValueMode
        {
            get { return _valueMode; }
            set { _valueMode = value; }
        }

        public static readonly string[] SupportedUnits = {
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

        public static readonly string[] SupportedNames = {
                                                        "Value",
                                                        "Unit",
                                                        "Dimension",
                                                        "Dimensions",
                                                        "Dimension0","Dimension1","Dimension2","Dimension3","Dimension4","Dimension5","Dimension6","Dimension7","Dimension8","Dimension9",
                                                        "NameSpace",
                                                        "Name",
                                                        "MetricName"
                                                    };

        public static readonly string[] SupportedStatistics = {
                                                        "Maximum",
                                                        "Minimum",
                                                        "SampleCount",
                                                        "Sum"
                                                    };
    }

    public class MetricDatumFilledException : InvalidOperationException
    {
        public MetricDatumFilledException(string message)
            : base(message)
        {

        }
    }
}