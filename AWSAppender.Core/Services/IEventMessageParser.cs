using System.Collections.Generic;

namespace AWSAppender.Core.Services
{
    public interface IEventMessageParser<TDatum>
    {
        IEnumerable<TDatum> Parse(string renderedString);
    }
}