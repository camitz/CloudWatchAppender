using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
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
        private static ConcurrentDictionary<int, Task> _tasks = new ConcurrentDictionary<int, Task>();

        public static bool HasPendingRequests { get { return _tasks.Values.Any(t => !t.IsCompleted); } }

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
            var CWClient = AWSClientFactory.CreateAmazonCloudWatchClient(
                  ConfigurationManager.AppSettings["AWSAccessKey"],
                  ConfigurationManager.AppSettings["AWSSecretKey"],
                  new AmazonCloudWatchConfig { ServiceURL = ConfigurationManager.AppSettings["AWSServiceEndpoint"] }
                );

            var data = new List<MetricDatum>
                           {
                               new MetricDatum()
                                   .WithMetricName("RandomTicks")
                                   .WithUnit("Count")
                                   .WithValue(1)
                           };

            try
            {
                Task task =
                    Task.Factory.StartNew(() =>
                                          CWClient.PutMetricData(new PutMetricDataRequest()
                                                                     .WithNamespace("RandomTicks")
                                                                     .WithMetricData(data)));
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
    }
}