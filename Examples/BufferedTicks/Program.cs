using System;
using System.Diagnostics;
using System.Threading;
using Amazon.CloudWatch.Model;
using AWSAppender.Core.Services;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using MetricDatum = AWSAppender.CloudWatch.Model.MetricDatum;

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
                {
                    var metricDatum = new MetricDatum("A tick!");
                    metricDatum.NameSpace = nss[random.Next(2)];
                    metricDatum.Dimensions.Add(new Dimension { Name = "TestDim", Value = dims[random.Next(2)] });
                    metricDatum.Unit = units[random.Next(3)];
                    metricDatum.StatisticValues = new StatisticSet();
                    metricDatum.StatisticValues.Minimum = random.NextDouble() * (3e3 - 1e2) + 1e2;
                    metricDatum.StatisticValues.Maximum = random.NextDouble() * (1.1e5 - .9e4) + .9e4;
                    metricDatum.StatisticValues.Sum = (random.NextDouble() * (6e4 - 4e4) + 4e4) * 100;
                    metricDatum.StatisticValues.SampleCount = 100;
                    log.Info(metricDatum);
                }

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

