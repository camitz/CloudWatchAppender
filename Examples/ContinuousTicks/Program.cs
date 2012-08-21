using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
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
            //Testing the core mechinery.
            var CWClient = AWSClientFactory.CreateAmazonCloudWatchClient(
                               ConfigurationManager.AppSettings["AWSAccessKey"],
                               ConfigurationManager.AppSettings["AWSSecretKey"],
                               new AmazonCloudWatchConfig { ServiceURL = "https://monitoring.eu-west-1.amazonaws.com" }
                             );

            var data = new List<MetricDatum>
                           {
                               new MetricDatum()
                                   .WithMetricName("CloudWatchAppender")
                                   .WithUnit("Seconds")
                                   .WithValue(1.0)
                           };

            //var response = CWClient.PutMetricData(new PutMetricDataRequest()
            //                     .WithNamespace("CloudWatchAppender")
            //                     .WithMetricData(data));

            XmlConfigurator.Configure();

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (int i = 0; i < nTicks; i++)
            {
                //log.Info("A tick! Value: 2, Unit: Bytes, Unit: Kilobytes");
                //log.Info("A tick! Value: 29.4 Kilobytes");
                log.Info("A tick!");
            }

            stopWatch.Stop();
            Console.WriteLine(String.Format("All {0} ticks in {1} ms.\nWaiting for requests to complete.",nTicks,stopWatch.ElapsedMilliseconds));

            stopWatch.Start();
            CloudWatchAppender.CloudWatchAppender.WaitForPendingRequests();

            stopWatch.Stop();
            Console.WriteLine(String.Format("Requests completed in {0} ms.", stopWatch.ElapsedMilliseconds));
        }
    }
}

