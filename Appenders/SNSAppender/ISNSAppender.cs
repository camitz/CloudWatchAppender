using AWSAppender.Core;

namespace SQSAppender
{
    public interface ISNSAppender : IAWSAppender
    {
        string Topic { set; }
        string Message { set; }
        //string DelaySeconds { set; }
    }

}

