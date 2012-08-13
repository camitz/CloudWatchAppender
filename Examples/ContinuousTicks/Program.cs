using System;
using System.Diagnostics;
using log4net;
using log4net.Config;

namespace ContinuousTicks
{
    internal class ContinuousTicks
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ContinuousTicks));
        private const int nTicks = 5;

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (int i = 0; i < nTicks; i++)
            {
                log.Info("A tick! Value: 2 Bytes");
            }
            stopWatch.Stop();
            Console.WriteLine(String.Format("All {0} ticks in {1} ms.\nWaiting for requests to complete.",nTicks,stopWatch.ElapsedMilliseconds));

            stopWatch.Restart();

            CloudWatchAppender.CloudWatchAppender.WaitForPendingRequests();

            stopWatch.Stop();
            Console.WriteLine(String.Format("Requests completed in {0} ms.", stopWatch.ElapsedMilliseconds));
        }
    }
}

