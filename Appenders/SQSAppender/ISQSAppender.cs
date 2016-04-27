using AWSAppender.Core;

namespace SQSAppender
{
    public interface ISQSAppender : IAWSAppender
    {
        string QueueName { set; }
        string Message { set; }
    }

}

