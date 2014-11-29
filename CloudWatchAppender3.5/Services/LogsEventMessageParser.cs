using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Model;

namespace CloudWatchAppender.Services
{
    public class LogsEventMessageParser : EventMessageParserBase
    {
        private readonly Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();
        private LogDatum _currentDatum;
        private static string _assemblyName;

        public string DefaultGroupName { get; set; }
        public string DefaultMessage { get; set; }
        public DateTime? DefaultTimestamp { get; set; }
        public string DefaultStreamName { get; set; }

        protected override void SetDefaults()
        {
            if (string.IsNullOrEmpty(_currentDatum.StreamName))
                _currentDatum.StreamName = DefaultStreamName ?? _assemblyName ?? "unspecified";


            if (string.IsNullOrEmpty(_currentDatum.GroupName))
                _currentDatum.GroupName = DefaultGroupName ?? "unspecified";

            if (string.IsNullOrEmpty(_currentDatum.Message))
                _currentDatum.Message = DefaultMessage ?? "";

            if (!_currentDatum.Timestamp.HasValue)
                _currentDatum.Timestamp = DefaultTimestamp;
        }


        protected override bool IsSupportedName(string t0)
        {
            return SupportedNames.Any(x => x.Equals(t0, StringComparison.InvariantCultureIgnoreCase));
        }


        protected override bool FillName(AppenderValue value)
        {
            switch (value.Name.ToLowerInvariant())
            {
                case "rest":
                    if (!string.IsNullOrEmpty(_currentDatum.Message))
                        return false;

                    _currentDatum.Message = DefaultsOverridePattern ? DefaultMessage ?? value.sValue : value.sValue;
                    break;

                case "streamname":
                    if (!string.IsNullOrEmpty(_currentDatum.StreamName))
                        return false;

                    _currentDatum.StreamName = DefaultsOverridePattern ? DefaultStreamName ?? value.sValue : value.sValue;
                    break;

                case "groupname":
                    if (!string.IsNullOrEmpty(_currentDatum.GroupName))
                        return false;

                    _currentDatum.GroupName = DefaultsOverridePattern ? DefaultGroupName ?? value.sValue : value.sValue;
                    break;

                case "timestamp":
                    if (_currentDatum.Timestamp.HasValue)
                        return false;

                    _currentDatum.Timestamp = DefaultsOverridePattern ? DefaultTimestamp ?? value.Time.Value.DateTime : value.Time.Value.DateTime;
                    break;
            }

            return true;
        }

        protected override void NewDatum()
        {
            _currentDatum = new LogDatum { };
        }




        public LogsEventMessageParser(string renderedMessage, bool useOverrides = true)
            : base(renderedMessage, useOverrides)
        {
            if (Assembly.GetEntryAssembly() != null)
                _assemblyName = Assembly.GetEntryAssembly().GetName().Name;
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

        public IEnumerable<LogDatum> GetParsedData()
        {
            if (_currentDatum.GroupName.Contains("instance"))
            {
                
            }
            return new[] { _currentDatum };
        }

        public static readonly HashSet<string> SupportedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                                    {
                                                        "Message",
                                                        "GroupName",
                                                        "StreamName",
                                                        "Timestamp"
                                                    };

    }

}