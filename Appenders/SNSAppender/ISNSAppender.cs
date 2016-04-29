using AWSAppender.Core;

namespace AWSAppender.SNS
{
    public interface ISNSAppender : IAWSAppender
    {
        string Topic { set; }
        string Message { set; }
        //string DelaySeconds { set; }
    }

}

