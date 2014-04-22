using System;
using System.Diagnostics;
using log4net;
using log4net.Config;

namespace BufferedTicks
{
    internal class BufferedTicks
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BufferedTicks));
        private const int nTicks = 500;

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var dims = new[] { "TestDimenValue1", "TestDimenValue2" };
            var names = new[] { "TestName1", "TestName2" };
            var nss = new[] { "TestNameSpace1", "TestNameSpace2" };
            var random = new Random();


            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (int i = 0; i < nTicks; i++)
            {
                //log.Info("A tick! Value: 2, Unit: Bytes, Unit: Kilobytes");

                log.InfoFormat("A tick! Namespace: {1} MetricName: {2} Dimension: TestDim: {3} Value: {0} Kilobyte",
                    random.NextDouble() * (1e5 - 1e2) + 1e2,
                    nss[random.Next(2)], names[random.Next(2)], dims[random.Next(2)]);

                //log.Info(String.Format("A tick! Timestamp: {0}", DateTimeOffset.Now.AddMinutes(-10).ToString()));
                //log.Info(null);
                //log.Info("A tick! %logger %metadata{instanceid}");
                //log.Info(new CloudWatchAppender.MetricDatum("A tick!")
                //    .WithTimestamp(DateTimeOffset.Now.AddMinutes(-10))
                //    .WithUnit("Kilobytes")
                //    .WithValue(29.4));
            }

            stopWatch.Stop();
            Console.WriteLine("All {0} ticks in {1} ms.\nWaiting for requests to complete.", nTicks, stopWatch.ElapsedMilliseconds);

            stopWatch.Start();
            CloudWatchAppender.BufferingAggregatingCloudWatchAppender.WaitForPendingRequests();

            stopWatch.Stop();
            Console.WriteLine("Requests completed in {0} ms.", stopWatch.ElapsedMilliseconds);
        }
    }
}

