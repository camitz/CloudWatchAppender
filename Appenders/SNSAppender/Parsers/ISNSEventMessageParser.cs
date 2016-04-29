using AWSAppender.Core.Services;
using AWSAppender.SNS.Model;

namespace AWSAppender.SNS.Parsers
{

    public interface ISNSEventMessageParser : IEventMessageParser<SNSDatum>
    {
        string DefaultTopic { get; set; }
        string DefaultMessage { get; set; }
        //int? DefaultDelaySeconds { get; set; }
    }
}