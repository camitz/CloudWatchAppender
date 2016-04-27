using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using AWSAppender.Core.Services;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using SQSAppender.Model;

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

                if (random.Next(2) == 0)
                    log.InfoFormat("A tick! Queuename: {0} ID: {1}", queues[random.Next(2)], random.NextDouble().ToString(CultureInfo.InvariantCulture).Replace(".",""));
                else
                    log.Info(new SQSDatum("A tick!")
                    {
                        QueueName = queues[random.Next(2)],
                        ID = random.NextDouble().ToString(CultureInfo.InvariantCulture).Replace(".", "")
                    });

                log.Info("Message: sample text for logging");

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

