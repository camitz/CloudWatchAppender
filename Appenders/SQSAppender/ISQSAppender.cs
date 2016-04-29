using AWSAppender.Core;

namespace AWSAppender.SQS
{
    public interface ISQSAppender : IAWSAppender
    {
        string QueueName { set; }
        string Message { set; }
        string DelaySeconds { set; }
    }

}

