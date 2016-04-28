using AWSAppender.Core;

namespace SNSAppender
{
    public interface ISNSAppender : IAWSAppender
    {
        string Topic { set; }
        string Message { set; }
        //string DelaySeconds { set; }
    }

}

