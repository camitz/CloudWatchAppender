using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace CloudWatchAppender
{
    public class CloudWatchAppender : AppenderSkeleton
    {
        public string AccessKey { get; set; }
        public string Secret { get; set; }
        public string EndPoint { get; set; }

        public string Unit { get; set; }
        public string Value { get; set; }
        public string MetricName { get; set; }
        public string Namespace { get; set; }
        public string Timestamp { get; set; }

        public Dimension Dimension { set { AddDimension(value); } }

        private bool _configOverrides = true;
        public bool ConfigOverrides
        {
            get { return _configOverrides; }
            set { _configOverrides = value; }
        }

        private void AddDimension(Dimension value)
        {
            _eventProcessor.Dimensions[value.Name] = value;
        }

        public int RateLimit
        {
            set { _eventRateLimiter = new EventRateLimiter(value); }
        }

        private string _instanceMetaDataReaderClass;
        public string InstanceMetaDataReaderClass
        {
            get { return _instanceMetaDataReaderClass; }
            set
            {
                _instanceMetaDataReaderClass = value;
                InstanceMetaDataReader.Instance =
                    Activator.CreateInstance(Type.GetType(value)) as IInstanceMetaDataReader;
            }
        }


        private static ConcurrentDictionary<int, Task> _tasks = new ConcurrentDictionary<int, Task>();


        public CloudWatchAppender()
        {
            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(Amazon.CloudWatch.Model.MetricDatum), new MetricDatumRenderer());
            _eventProcessor = new EventProcessor(ConfigOverrides, Unit, Namespace, MetricName, Timestamp, Value);
        }


        public static bool HasPendingRequests
        {
            get { return _tasks.Values.Any(t => !t.IsCompleted); }
        }

        public static void WaitForPendingRequests(TimeSpan timeout)
        {
            var startedTime = DateTime.UtcNow;
            var timeConsumed = TimeSpan.Zero;
            while (HasPendingRequests && timeConsumed < timeout)
            {
                Task.WaitAll(_tasks.Values.ToArray(), timeout - timeConsumed);
                timeConsumed = DateTime.UtcNow - startedTime;
            }
        }

        public static void WaitForPendingRequests()
        {
            while (HasPendingRequests)
                Task.WaitAll(_tasks.Values.ToArray());
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            System.Diagnostics.Debug.WriteLine("Appending");

            if (!_eventRateLimiter.Request(loggingEvent.TimeStamp))
            {
                System.Diagnostics.Debug.WriteLine("Appending denied due to event saturation.");
                return;
            }

            if (Layout == null)
                Layout = new PatternLayout("%message");

            var parser = _eventProcessor.ProcessEvent(loggingEvent, RenderLoggingEvent(loggingEvent));

            foreach (var putMetricDataRequest in _eventProcessor.GetMetricDataRequests())
                SendItOff(putMetricDataRequest);
        }

        private EventRateLimiter _eventRateLimiter = new EventRateLimiter();
        private ClientWrapper _client;
        private readonly EventProcessor _eventProcessor;

        private void SendItOff(PutMetricDataRequest metricDataRequest)
        {
            if (_client == null)
                _client = new ClientWrapper(EndPoint, AccessKey, Secret);


            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            try
            {

                var task1 =
                    Task.Factory.StartNew(() =>
                    {
                        var task =
                            Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    var tmpCulture = Thread.CurrentThread.CurrentCulture;
                                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB", false);

                                    System.Diagnostics.Debug.WriteLine("Sending");
                                    var response = _client.PutMetricData(metricDataRequest);
                                    System.Diagnostics.Debug.WriteLine("RequestID: " + response.ResponseMetadata.RequestId);

                                    Thread.CurrentThread.CurrentCulture = tmpCulture;
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine(e);
                                }
                            }, ct);

                        try
                        {
                            if (!task.Wait(30000))
                            {
                                tokenSource.Cancel();
                                System.Diagnostics.Debug.WriteLine(
                                        "CloudWatchAppender timed out while submitting to CloudWatch. Exception (if any): {0}",
                                        task.Exception);
                            }
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                    "CloudWatchAppender encountered an error while submitting to cloudwatch. {0}", e);
                        }
                    });

                if (!task1.IsCompleted)
                    _tasks.TryAdd(task1.Id, task1);

                task1.ContinueWith(t =>
                {
                    Task task2;
                    _tasks.TryRemove(task1.Id, out task2);
                    System.Diagnostics.Debug.WriteLine("Cloudwatch complete");
                    if (task1.Exception != null)
                        System.Diagnostics.Debug.WriteLine(string.Format("CloudWatchAppender encountered an error while submitting to CloudWatch. {0}", task1.Exception));
                });

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format(
                        "CloudWatchAppender encountered an error while submitting to cloudwatch. {0}", e));
            }

        }
    }
}