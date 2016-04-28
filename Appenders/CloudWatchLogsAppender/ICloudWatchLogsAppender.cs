using AWSAppender.Core;

namespace CloudWatchLogsAppender
{
    public interface ICloudWatchLogsAppender : IAWSAppender
    {
        string GroupName { set; }
        string StreamName { set; }
        string Message { set; }
    }

}

