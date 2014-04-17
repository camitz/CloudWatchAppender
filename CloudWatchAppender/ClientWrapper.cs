using System.Configuration;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Runtime;

namespace CloudWatchAppender
{
    public class ClientWrapper
    {
        private IAmazonCloudWatch _client;

        public ClientWrapper(string endPoint, string accessKey, string secret)
        {
            _endPoint = endPoint;
            _accessKey = accessKey;
            _secret = secret;

            SetupClient();
        }

        private string _endPoint;
        private string _accessKey;
        private string _secret;
       

        private void SetupClient()
        {
            if (_client != null)
                return;

            AmazonCloudWatchConfig cloudWatchConfig = null;
            RegionEndpoint regionEndpoint = null;

            if (string.IsNullOrEmpty(_endPoint) && ConfigurationManager.AppSettings["AWSServiceEndpoint"] != null)
                _endPoint = ConfigurationManager.AppSettings["AWSServiceEndpoint"];

            if (string.IsNullOrEmpty(_accessKey) && ConfigurationManager.AppSettings["AWSAccessKey"] != null)
                _accessKey = ConfigurationManager.AppSettings["AWSAccessKey"];

            if (string.IsNullOrEmpty(_secret) && ConfigurationManager.AppSettings["AWSSecretKey"] != null)
                _secret = ConfigurationManager.AppSettings["AWSSecretKey"];

            //_client = AWSClientFactory.CreateAmazonCloudWatchClient(_accessKey, _secret);

            try
            {

                if (!string.IsNullOrEmpty(_endPoint))
                {
                    if (_endPoint.StartsWith("http"))
                    {
                        cloudWatchConfig = new AmazonCloudWatchConfig { ServiceURL = _endPoint };
                        if (string.IsNullOrEmpty(_accessKey))
                            _client = AWSClientFactory.CreateAmazonCloudWatchClient(cloudWatchConfig);
                    }
                    else
                    {
                        regionEndpoint = RegionEndpoint.GetBySystemName(_endPoint);
                        if (string.IsNullOrEmpty(_accessKey))
                            _client = AWSClientFactory.CreateAmazonCloudWatchClient(regionEndpoint);
                    }
                }
            }
            catch (AmazonServiceException)
            {
            }

            if (!string.IsNullOrEmpty(_accessKey))
                if (regionEndpoint != null)
                    _client = AWSClientFactory.CreateAmazonCloudWatchClient(_accessKey, _secret, regionEndpoint);
                else if (cloudWatchConfig != null)
                    _client = AWSClientFactory.CreateAmazonCloudWatchClient(_accessKey, _secret, cloudWatchConfig);
                else
                    _client = AWSClientFactory.CreateAmazonCloudWatchClient(_accessKey, _secret);

            //Debug
            var metricDatum = new Amazon.CloudWatch.Model.MetricDatum
                              {
                                  MetricName = "CloudWatchAppender",
                                  Value = 1,
                                  Unit = "Count"
                              };
            //_client.PutMetricData(new PutMetricDataRequest().WithNamespace("CloudWatchAppender").WithMetricData(metricDatum));
        }

        public PutMetricDataResponse PutMetricData(PutMetricDataRequest metricDataRequest)
        {
            return _client.PutMetricData(metricDataRequest);
        }
    }
}