using System;
using System.Collections.Generic;
using SQSAppender.Model;

namespace SQSAppender.Parsers
{
    public interface IEventMessageParser<TDatum>
    {
        IEnumerable<TDatum> Parse(string renderedString);
    }

    public interface ISQSEventMessageParser : IEventMessageParser<SQSDatum>
    {
        string DefaultQueueName { get; set; }
        string DefaultMessage { get; set; }
        string DefaultID { get; set; }
    }
}