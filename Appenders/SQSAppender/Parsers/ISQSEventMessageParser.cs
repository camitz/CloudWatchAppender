using AWSAppender.Core.Services;
using AWSAppender.SQS.Model;

namespace AWSAppender.SQS.Parsers
{

    public interface ISQSEventMessageParser : IEventMessageParser<SQSDatum>
    {
        string DefaultQueueName { get; set; }
        string DefaultMessage { get; set; }
        int? DefaultDelaySeconds { get; set; }
    }
}