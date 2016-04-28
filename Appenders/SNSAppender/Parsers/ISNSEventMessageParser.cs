using AWSAppender.Core.Services;
using SQSAppender.Model;

namespace SQSAppender.Parsers
{

    public interface ISNSEventMessageParser : IEventMessageParser<SNSDatum>
    {
        string DefaultTopic { get; set; }
        string DefaultMessage { get; set; }
        //int? DefaultDelaySeconds { get; set; }
    }
}