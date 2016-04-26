using System;
using System.Collections.Generic;
using CloudWatchAppender.Model;

namespace CloudWatchAppender.Parsers
{
    public interface IEventMessageParser<TDatum>
    {
        IEnumerable<TDatum> Parse(string renderedString);
    }

    public interface ISQSEventMessageParser : IEventMessageParser<SQSDatum>
    {
        string DefaultQueueName { get; set; }
        string DefaultMessage { get; set; }
        DateTime? DefaultTimestamp { get; set; }
    }
}