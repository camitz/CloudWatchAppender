﻿using System;
using System.Diagnostics;
using System.Threading;
using AWSAppender.CloudWatchLogs.Model;
using AWSAppender.Core.Services;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;

namespace BufferedLogs
{
    internal class BufferedLogs
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BufferedLogs));
        private const int nTicks = 100;

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var streams = new[] { "Stream1", "Stream2" };
            var groups = new[] { "GRoup1", "GRaoup2" };
            var random = new Random();

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (int i = 0; i < nTicks; i++)
            {
                //log.Info("A tick! Value: 2, Unit: Bytes, Unit: Kilobytes");

                if (random.Next(2) == 0)
                    log.InfoFormat("A tick! Groupname: {0} Streamname: {1} Gauge: {2}",
                        groups[random.Next(2)], streams[random.Next(2)], random.Next(200));
                else
                    log.Info(new LogDatum("A tick!")
                    {
                        GroupName = groups[random.Next(2)],
                        StreamName = streams[random.Next(2)],
                        Timestamp = DateTime.UtcNow
                    });

                log.Info("Message: sample text for logging");

                Thread.Sleep(10);
            }

            ILoggerRepository rep = LogManager.GetRepository();
            foreach (IAppender appender in rep.GetAppenders())
            {
                var buffered = appender as BufferingAppenderSkeleton;
                if (buffered != null)
                {
                    buffered.Flush();
                }
            }

            stopWatch.Stop();
            Console.WriteLine("All {0} ticks in {1} ms.\nWaiting for requests to complete.", nTicks, stopWatch.ElapsedMilliseconds);

            stopWatch.Start();
            ServiceTasks.WaitForPendingRequests();

            stopWatch.Stop();
            Console.WriteLine("Requests completed in {0} ms.", stopWatch.ElapsedMilliseconds);
        }
    }
}

