using AWSAppender.Core.Services;
using SQSAppender.Model;

namespace SQSAppender.Parsers
{

    public interface ISQSEventMessageParser : IEventMessageParser<SQSDatum>
    {
        string DefaultQueueName { get; set; }
        string DefaultMessage { get; set; }
        int? DefaultDelaySeconds { get; set; }
    }
}