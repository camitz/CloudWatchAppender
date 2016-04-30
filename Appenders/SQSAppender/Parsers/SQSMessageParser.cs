using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AWSAppender.Core.Services;
using AWSAppender.SQS.Model;

namespace AWSAppender.SQS.Parsers
{
    public class SQSMessageParser : EventMessageParserBase<SQSDatum>, ISQSEventMessageParser
    {
        private SQSDatum _currentDatum;
        private static string _assemblyName;

        public string DefaultMessage { get; set; }
        public int? DefaultDelaySeconds { get; set; }
        public string DefaultQueueName { get; set; }
        public new bool ConfigOverrides { get { return base.ConfigOverrides; } set { base.ConfigOverrides = value; } }

        public SQSMessageParser()
            : base(true)
        { }
        public SQSMessageParser(bool useOverrides)
            : base(useOverrides)
        {
            if (Assembly.GetEntryAssembly() != null)
                _assemblyName = Assembly.GetEntryAssembly().GetName().Name;
        }
        protected override void SetDefaults()
        {
            if (string.IsNullOrEmpty(_currentDatum.QueueName))
                _currentDatum.QueueName = DefaultQueueName ?? _assemblyName ?? "unspecified";

            if (string.IsNullOrEmpty(_currentDatum.ID))
                _currentDatum.ID = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(_currentDatum.Message))
                _currentDatum.Message = DefaultMessage ?? "";

            if (!_currentDatum.DelaySeconds.HasValue)
                _currentDatum.DelaySeconds= DefaultDelaySeconds;
        }



        protected override bool IsSupportedName(string t0)
        {
            return SupportedNames.Any(x => x.Equals(t0, StringComparison.InvariantCultureIgnoreCase));
        }

        protected override bool IsSupportedValueField(string t0)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<SQSDatum> GetParsedData()
        {
            return new[] { _currentDatum };
        }


        protected override bool FillName(AppenderValue value)
        {
            switch (value.Name.ToLowerInvariant())
            {
                case "__cav_rest":
                    if (!string.IsNullOrEmpty(_currentDatum.Message))
                        return false;

                    _currentDatum.Message = DefaultsOverridePattern ? DefaultMessage ?? value.sValue : value.sValue;
                    break;

                case "queuename":
                    if (!string.IsNullOrEmpty(_currentDatum.QueueName))
                        return false;

                    _currentDatum.QueueName = DefaultsOverridePattern ? DefaultQueueName ?? value.sValue : value.sValue;
                    break;

                case "delayseconds":
                    if (_currentDatum.DelaySeconds.HasValue)
                        return false;

                    _currentDatum.DelaySeconds = DefaultsOverridePattern ? DefaultDelaySeconds ?? Convert.ToInt32(value.dValue) : Convert.ToInt32(value.dValue);
                    break;

                case "id":
                    if (!string.IsNullOrEmpty(_currentDatum.ID))
                        return false;

                    _currentDatum.ID = value.sValue;
                    break;

            }

            return true;
        }

        protected override void NewDatum()
        {
            _currentDatum = new SQSDatum { };
        }


        public static readonly HashSet<string> SupportedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                                    {
                                                        "Message",
                                                        "QueueName",
                                                        "DelaySeconds",
                                                        "ID"
                                                    };

    }

}