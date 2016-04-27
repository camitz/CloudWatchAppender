using System.Collections.Generic;
using log4net.Core;

namespace AWSAppender.Core.Services
{
    public interface IEventProcessor<T>
    {
        IEnumerable<T> ProcessEvent(LoggingEvent loggingEvent, string renderedString);
        IEventMessageParser<T> EventMessageParser { get; set; }
    }
}