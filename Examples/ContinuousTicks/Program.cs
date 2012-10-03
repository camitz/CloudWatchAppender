using System;
using System.Diagnostics;
using CloudWatchAppender;
using log4net;
using log4net.Config;

namespace ContinuousTicks
{
    internal class ContinuousTicks
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ContinuousTicks));
        private const int nTicks = 200;

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (int i = 0; i < nTicks; i++)
            {
                //log.Info("A tick! Value: 2, Unit: Bytes, Unit: Kilobytes");
                //log.Info("A tick! Value: 29.4 Kilobytes");
                //log.Info(String.Format("A tick! Timestamp: {0}", DateTimeOffset.Now.AddMinutes(-10).ToString()));
                log.Info(null);
                //log.Info("A tick! %logger %metadata{instanceid}");
                //log.Info(new CloudWatchAppender.MetricDatum("A tick!")
                //    .WithTimestamp(DateTimeOffset.Now.AddMinutes(-10))
                //    .WithUnit("Kilobytes")
                //    .WithValue(29.4));
            }
            
            stopWatch.Stop();
            Console.WriteLine(String.Format("All {0} ticks in {1} ms.\nWaiting for requests to complete.", nTicks, stopWatch.ElapsedMilliseconds));

            stopWatch.Start();
            CloudWatchAppender.CloudWatchAppender.WaitForPendingRequests();

            stopWatch.Stop();
            Console.WriteLine(String.Format("Requests completed in {0} ms.", stopWatch.ElapsedMilliseconds));
        }
    }
}

