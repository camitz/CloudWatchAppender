using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using log4net.Appender;
using log4net.Core;

namespace CloudWatchAppender
{
    public class CloudWatchAppender : AppenderSkeleton
    {
        public string Unit { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }

        public Dimension Dimension { set { AddDimension(value); } }

        private bool _configOverrides = true;
        public bool ConfigOverrides
        {
            get { return _configOverrides; }
            set { _configOverrides = value; }
        }

        private Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();

        private void AddDimension(Dimension value)
        {
            _dimensions[value.Name] = value;
        }

        private static ConcurrentDictionary<int, Task> _tasks = new ConcurrentDictionary<int, Task>();

        private readonly AmazonCloudWatch _client = AWSClientFactory.CreateAmazonCloudWatchClient(
                       ConfigurationManager.AppSettings["AWSAccessKey"],
                       ConfigurationManager.AppSettings["AWSSecretKey"],
                       new AmazonCloudWatchConfig { ServiceURL = ConfigurationManager.AppSettings["AWSServiceEndpoint"] }
                       );

        public static bool HasPendingRequests
        {
            get { return _tasks.Values.Any(t => !t.IsCompleted); }
        }

        public static void WaitForPendingRequests(TimeSpan timeout)
        {
            var startedTime = DateTime.Now;
            var timeConsumed = TimeSpan.Zero;
            while (HasPendingRequests && timeConsumed < timeout)
            {
                Task.WaitAll(_tasks.Values.ToArray(), timeout - timeConsumed);
                timeConsumed = DateTime.Now - startedTime;
            }
        }

        public static void WaitForPendingRequests()
        {
            while (HasPendingRequests)
                Task.WaitAll(_tasks.Values.ToArray());
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var renderedString = RenderLoggingEvent(loggingEvent);

            var patternParser = new PatternParser(loggingEvent);

            EventMessageParser parser;
            if (ConfigOverrides)
                parser = new EventMessageParser(renderedString)
                             {
                                 OverrideName = string.IsNullOrEmpty(Name)
                                                    ? null
                                                    : patternParser.Parse(Name),
                                 OverrideNameSpace = string.IsNullOrEmpty(Namespace)
                                                         ? null
                                                         : patternParser.Parse(Namespace),
                                 OverrideUnit = String.IsNullOrEmpty(Unit)
                                                    ? null
                                                    : patternParser.Parse(Unit),
                                 OverrideDimensions = _dimensions.Any()
                                                          ? _dimensions.Values.Select(
                                                              d =>
                                                              new Dimension
                                                                  {
                                                                      Name = d.Name,
                                                                      Value = patternParser.Parse(d.Value)
                                                                  })
                                                          : null
                             };
            else
                parser = new EventMessageParser(renderedString);

            if (!string.IsNullOrEmpty(Value) && ConfigOverrides)
                parser.OverrideValue = Double.Parse(Value, CultureInfo.InvariantCulture);

            parser.Parse();

            foreach (var r in parser)
                SendItOff(r);
        }

        private void SendItOff(PutMetricDataRequest r)
        {
            Task task =
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        _client.PutMetricData(r);
                    }
                    catch (AmazonCloudWatchException e)
                    {
                        //Don't log this exception! ;)
                        Console.WriteLine(e.Message);

                        throw new CloudWatchAppenderException("CloudWatchAppender encountered an error while submitting to CloudWatch. Maybe value has decimal part? We don't know why, but it doesn't work.", e);
                    }
                });

            if (!task.IsCompleted)
                _tasks.TryAdd(task.Id, task);

            task.ContinueWith(t => _tasks.TryRemove(task.Id, out task));
        }

        private string GetInstanceID()
        {
            return AWSMetaDataReader.GetInstanceID();
        }
    }

    internal class CloudWatchAppenderException : Exception
    {
        public CloudWatchAppenderException(string msg, Exception innerException)
            : base(msg, innerException)
        {

        }
    }
}