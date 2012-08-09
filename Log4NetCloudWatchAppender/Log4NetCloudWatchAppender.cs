using System;
using System.Collections.Generic;
using System.Configuration;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using log4net.Appender;
using log4net.Core;

namespace Log4NetCloudWatchAppender
{
    public class Log4NetCloudWatchAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            var CWClient = AWSClientFactory.CreateAmazonCloudWatchClient(
                  ConfigurationManager.AppSettings["AWSAccessKey"],
                  ConfigurationManager.AppSettings["AWSSecretKey"],
                  new AmazonCloudWatchConfig { ServiceURL = ConfigurationManager.AppSettings["AWSServiceEndpoint"] }
                );

            var data = new List<MetricDatum>
                           {
                               new MetricDatum()
                                   .WithMetricName("RandomTicks")
                                   .WithUnit("Count")
                                   .WithValue(13)
                           };

            try
            {

                var response = CWClient.PutMetricData(new PutMetricDataRequest()
                   .WithNamespace("RandomTicks")
                   .WithMetricData(data));

            }
            catch (Exception e)
            {
                //Don't log this exception :)
            }
        }
    }
}