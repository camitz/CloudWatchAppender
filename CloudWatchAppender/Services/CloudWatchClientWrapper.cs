using System;
using System.Collections.Generic;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace CloudWatchAppender.Services
{
    public class CloudWatchClientWrapper : CloudWatchClientWrapperBase<AmazonCloudWatchClient>
    {

        public CloudWatchClientWrapper(string endPoint, string accessKey, string secret)
            : base(endPoint, accessKey, secret)
        {
        }

        private PutMetricDataResponse PutMetricData(PutMetricDataRequest metricDataRequest)
        {
            return Client.PutMetricData(metricDataRequest);
        }

        internal void QueuePutMetricData(PutMetricDataRequest metricDataRequest)
        {
            QueueRequest(() => PutMetricData(metricDataRequest));
        }



        private PutMetricDataResponse PutMetricDataResponse(PutMetricDataRequest metricDataRequest)
        {
            var response = PutMetricData(metricDataRequest);
            return response;
        }
    }
}