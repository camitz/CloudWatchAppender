using System;
using AWSAppender.Core.Services;
using CloudWatchLogsAppender.Model;

namespace CloudWatchLogsAppender.Parsers
{

    public interface ILogsEventMessageParser : IEventMessageParser<LogDatum>
    {
        string DefaultStreamName { get; set; }
        string DefaultGroupName { get; set; }
        string DefaultMessage { get; set; }
        DateTime? DefaultTimestamp { get; set; }
    }
}