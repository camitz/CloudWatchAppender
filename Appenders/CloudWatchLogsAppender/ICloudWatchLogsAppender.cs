using AWSAppender.Core;

namespace AWSAppender.CloudWatchLogs
{
    public interface ICloudWatchLogsAppender : IAWSAppender
    {
        string GroupName { set; }
        string StreamName { set; }
        string Message { set; }
    }

}

