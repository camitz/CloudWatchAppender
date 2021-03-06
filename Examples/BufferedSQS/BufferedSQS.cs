﻿using System;
using System.Diagnostics;
using System.Globalization;
using AWSAppender.Core.Services;
using AWSAppender.SQS.Model;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;

namespace BufferedLogs
{
    internal class BufferedSQS
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BufferedSQS));
        private const int nTicks = 100;

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var queues = new[] { "Queue1", "Queue2" };
            var random = new Random();

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (int i = 0; i < nTicks; i++)
            {
                //log.Info("A tick! Value: 2, Unit: Bytes, Unit: Kilobytes");

                switch (random.Next(4))
                {
                    case 0:
                        log.InfoFormat("A tick! Queuename: {0} ID: {1}", "queue0",
                            random.NextDouble().ToString(CultureInfo.InvariantCulture).Replace(".", ""));
                        break;
                    case 1:
                        log.InfoFormat("A tick! Queuename: {0} ID: {1} DelaySeconds: {2}", "queue1",
                            random.NextDouble().ToString(CultureInfo.InvariantCulture).Replace(".", ""), random.Next(10));
                        break;
                    case 2:
                        log.Info(new SQSDatum("A tick!")
                                 {
                                     QueueName = "queue2",
                                     ID = random.NextDouble().ToString(CultureInfo.InvariantCulture).Replace(".", "")
                                 });
                        break;
                    case 3:
                        log.Info(new SQSDatum("A tick!")
                                 {
                                     QueueName = "queue3",
                                     ID = random.NextDouble().ToString(CultureInfo.InvariantCulture).Replace(".", ""),
                                     DelaySeconds = random.Next(10) + 10
                                 });
                        break;
                }

                //Thread.Sleep(10);
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

