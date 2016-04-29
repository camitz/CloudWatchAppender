using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Runtime;

namespace AWSAppender.CloudWatch.Services
{
    public class CloudWatchClientWrapper : AWSAppender.Core.Services.ClientWrapperBase<AmazonCloudWatchConfig, AmazonCloudWatchClient>
    {

        public CloudWatchClientWrapper(string endPoint, string accessKey, string secret, ClientConfig clientConfig)
            : base(endPoint, accessKey, secret, clientConfig)
        {
        }

        private PutMetricDataResponse PutMetricData(PutMetricDataRequest metricDataRequest)
        {
            return Client.PutMetricData(metricDataRequest);
        }

        internal void QueuePutMetricData(PutMetricDataRequest metricDataRequest)
        {
            AddRequest(() => PutMetricData(metricDataRequest));
        }



        private PutMetricDataResponse PutMetricDataResponse(PutMetricDataRequest metricDataRequest)
        {
            var response = PutMetricData(metricDataRequest);
            return response;
        }
    }
}