using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using AWSAppender.Core;

namespace AWSAppender.CloudWatch
{
    public interface ICloudWatchAppender : IAWSAppender
    {
        Dimension Dimension { set; }
        string Unit { set; }
        StandardUnit StandardUnit { set; }
        string Value { set; }
        string MetricName { set; }
        string Namespace { get; set; }
    }
}