using AWSAppender.Core;

namespace CloudWatchAppender
{
    public interface ICloudWatchLogsAppender : IAWSAppender
    {
        string GroupName { set; }
        string StreamName { set; }
        string Message { set; }
    }

}

