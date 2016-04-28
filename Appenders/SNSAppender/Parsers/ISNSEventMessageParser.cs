using AWSAppender.Core.Services;
using SNSAppender.Model;

namespace SNSAppender.Parsers
{

    public interface ISNSEventMessageParser : IEventMessageParser<SNSDatum>
    {
        string DefaultTopic { get; set; }
        string DefaultMessage { get; set; }
        //int? DefaultDelaySeconds { get; set; }
    }
}