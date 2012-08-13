using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
        public bool UseLoggerName { get; set; }

        public Dimension Dimension0 { set { AddDimension(value); } }
        public Dimension Dimension1 { set { AddDimension(value); } }
        public Dimension Dimension2 { set { AddDimension(value); } }
        public Dimension Dimension3 { set { AddDimension(value); } }
        public Dimension Dimension4 { set { AddDimension(value); } }
        public Dimension Dimension5 { set { AddDimension(value); } }
        public Dimension Dimension6 { set { AddDimension(value); } }
        public Dimension Dimension7 { set { AddDimension(value); } }
        public Dimension Dimension8 { set { AddDimension(value); } }
        public Dimension Dimension9 { set { AddDimension(value); } }

        private List<Dimension> _dimensions = new List<Dimension>();

        private void AddDimension(Dimension value)
        {
            if (value != null && value.Name == "InstanceID" && value.Value == null)
                value.Value = GetInstanceID();
            _dimensions.Add(value);
        }

        private static ConcurrentDictionary<int, Task> _tasks = new ConcurrentDictionary<int, Task>();

        readonly AmazonCloudWatch _client = AWSClientFactory.CreateAmazonCloudWatchClient(
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
            var parser = new EventMessageParser(loggingEvent.RenderedMessage)
                             {
                                 OverrideName = string.IsNullOrEmpty(Name)
                                                    ? UseLoggerName
                                                          ? loggingEvent.LoggerName.Split(new[] { '.' }).Last()
                                                          : null
                                                    : Name,

                                 OverrideNameSpace = string.IsNullOrEmpty(Namespace)
                                                         ? UseLoggerName
                                                               ? String.Join("/",
                                                                             loggingEvent.LoggerName.Split(new[] { '.' }).
                                                                                 Reverse().Skip(1).
                                                                                 Reverse())
                                                               : null
                                                         : Namespace,

                                 OverrideUnit = String.IsNullOrEmpty(Unit)
                                                    ? null
                                                    : Unit,

                                 OverrideDimensions = _dimensions.Any() ? _dimensions : null
                             };

                            if (!string.IsNullOrEmpty(Value))
                                parser.OverrideValue = Double.Parse(Value, CultureInfo.InvariantCulture);

            parser.Parse();

            foreach (var r in parser)
            {
                SendItOff(r);
            }
        }

        private void SendItOff(PutMetricDataRequest r)
        {
            try
            {
                Task task =
                    Task.Factory.StartNew(() =>
                                          _client.PutMetricData(r));

                if (!task.IsCompleted)
                    _tasks.TryAdd(task.Id, task);

                task.ContinueWith(t => _tasks.TryRemove(task.Id, out task));
            }
            catch (AmazonCloudWatchException e)
            {
                //Don't log this exception! ;)
                Console.WriteLine(e.Message);
            }
        }


        private string GetInstanceID()
        {
            return new StreamReader(
                WebRequest.Create("http://169.254.169.254/latest/meta-data/instance-id")
                    .GetResponse()
                    .GetResponseStream(), true)
                .ReadToEnd();
        }
    }
}