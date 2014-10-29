using System;
using System.Diagnostics;
using CloudWatchAppender.Services;
using log4net;
using log4net.Config;

namespace Logs
{
    internal class Logs
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Logs));
        private const int nTicks = 100;

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (int i = 0; i < nTicks; i++)
            {
                log.Info("A log event");
            }

            stopWatch.Stop();
            Console.WriteLine(String.Format("All {0} ticks in {1} ms.\nWaiting for requests to complete.", nTicks, stopWatch.ElapsedMilliseconds));

            stopWatch.Start();
            ServiceTasks.WaitForPendingRequests();

            stopWatch.Stop();
            Console.WriteLine(String.Format("Requests completed in {0} ms.", stopWatch.ElapsedMilliseconds));
        }
    }
}

