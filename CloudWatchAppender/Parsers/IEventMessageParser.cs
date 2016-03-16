using System;
using System.Collections.Generic;
using CloudWatchAppender.Model;

namespace CloudWatchAppender.Parsers
{
    public interface IEventMessageParser<TDatum>
    {
        IEnumerable<TDatum> Parse(string renderedString);
    }

    public interface ILogsEventMessageParser : IEventMessageParser<LogDatum>
    {
        string DefaultStreamName { get; set; }
        string DefaultGroupName { get; set; }
        string DefaultMessage { get; set; }
        DateTime? DefaultTimestamp { get; set; }
    }
}