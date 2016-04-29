using System;
using AWSAppender.CloudWatchLogs.Model;
using AWSAppender.Core.Services;

namespace AWSAppender.CloudWatchLogs.Parsers
{

    public interface ILogsEventMessageParser : IEventMessageParser<LogDatum>
    {
        string DefaultStreamName { get; set; }
        string DefaultGroupName { get; set; }
        string DefaultMessage { get; set; }
        DateTime? DefaultTimestamp { get; set; }
    }
}