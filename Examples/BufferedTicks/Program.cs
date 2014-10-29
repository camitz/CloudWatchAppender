using System;
using System.Diagnostics;
using System.Threading;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Services;
using log4net;
using log4net.Config;

namespace BufferedTicks
{
    internal class BufferedTicks
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BufferedTicks));
        private const int nTicks = 1000;

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var dims = new[] { "TestDimenValue1", "TestDimenValue2" };
            var names = new[] { "TestName1", "TestName2" };
            var nss = new[] { "TestNameSpace1", "TestNameSpace2" };
            var units = new[] { "Kilobytes", "Megabytes", "Gigabytes" };
            var random = new Random();


            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (int i = 0; i < nTicks; i++)
            {
                //log.Info("A tick! Value: 2, Unit: Bytes, Unit: Kilobytes");

                if (random.Next(2) == 0)
                    log.InfoFormat("A tick! Namespace: {1} MetricCOMMENTName: {2} Dimension: TestDim: {3} Value: {0} {4}",
                        random.NextDouble() * (1e5 - 1e2) + 1e2,
                        nss[random.Next(2)], names[random.Next(2)], dims[random.Next(2)], units[random.Next(3)]);
                else
                    log.Info(new CloudWatchAppender.Model.MetricDatum("A tick!")
                    {
                        NameSpace = nss[random.Next(2)],
                        Dimensions = { new Dimension { Name = "TestDim", Value = dims[random.Next(2)] } },
                        //MetricName = names[random.Next(2)],
                        Unit = units[random.Next(3)],
                        StatisticValues = new StatisticSet
                                             {
                                                 Minimum = random.NextDouble() * (3e3 - 1e2) + 1e2,
                                                 Maximum = random.NextDouble() * (1.1e5 - .9e4) + .9e4,
                                                 Sum = (random.NextDouble() * (6e4 - 4e4) + 4e4) * 100,
                                                 SampleCount = 100
                                             }
                    });

                Thread.Sleep(10);
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

