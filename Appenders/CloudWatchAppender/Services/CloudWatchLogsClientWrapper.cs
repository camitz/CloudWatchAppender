using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Runtime;
using log4net.Util;

namespace CloudWatchAppender.Services
{
    public class CloudWatchLogsClientWrapper : CloudWatchClientWrapperBase<AmazonCloudWatchLogsClient>
    {
        private static readonly LockObject _lockObject = new LockObject();

        private readonly ConcurrentDictionary<string, string> _validatedGroupNames = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, string> _validatedStreamNames = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, string> _nextSequenceToken = new ConcurrentDictionary<string, string>();
        public CloudWatchLogsClientWrapper(string endPoint, string accessKey, string secret, ClientConfig clientConfig)
            : base(endPoint, accessKey, secret, clientConfig)
        {
        }

        internal void AddLogRequest(PutLogEventsRequest putLogEventsRequest)
        {
            AddRequest(() => PutLogEvents(putLogEventsRequest));
        }

        private AmazonWebServiceResponse PutLogEvents(PutLogEventsRequest putLogEventsRequest)
        {
            if (!_validatedGroupNames.ContainsKey(putLogEventsRequest.LogGroupName) || !_validatedStreamNames.ContainsKey(putLogEventsRequest.LogStreamName))
            {
                lock (_lockObject)
                {
                    if (!_validatedGroupNames.ContainsKey(putLogEventsRequest.LogGroupName))
                    {
                        try
                        {
                            Client.CreateLogGroup(new CreateLogGroupRequest { LogGroupName = putLogEventsRequest.LogGroupName });
                        }
                        catch (ResourceAlreadyExistsException)
                        {
                        }
                        _validatedGroupNames.TryAdd(putLogEventsRequest.LogGroupName, putLogEventsRequest.LogGroupName);
                    }

                    if (!_validatedStreamNames.ContainsKey(putLogEventsRequest.LogStreamName))
                    {
                        try
                        {
                            Client.CreateLogStream(new CreateLogStreamRequest { LogGroupName = putLogEventsRequest.LogGroupName, LogStreamName = putLogEventsRequest.LogStreamName });
                        }
                        catch (ResourceAlreadyExistsException)
                        {
                        }
                        _validatedStreamNames.TryAdd(putLogEventsRequest.LogStreamName, putLogEventsRequest.LogStreamName);
                    }
                }
            }

            lock (_lockObject)
            {
                AmazonWebServiceResponse ret = null;

                string nextSequenceToken;
                var key = putLogEventsRequest.LogGroupName + "/" + putLogEventsRequest.LogStreamName;
                if (!_nextSequenceToken.ContainsKey(key))
                    _nextSequenceToken[key] = null;
                nextSequenceToken = _nextSequenceToken[key];

                for (var i = 0; i < 10 && ret == null; i++)
                {
                    try
                    {
                        try
                        {
                            putLogEventsRequest.SequenceToken = nextSequenceToken;
                            var putLogEventsResponse = Client.PutLogEvents(putLogEventsRequest);
                            _nextSequenceToken[key] = putLogEventsResponse.NextSequenceToken;
                            ret = putLogEventsResponse;
                        }
                        catch (ResourceNotFoundException)
                        {
                            throw;
                        }
                    }
                    catch (DataAlreadyAcceptedException e)
                    {
                        var matchCollection = Regex.Matches(e.Message, @"[0-9]{20,}");
                        if (matchCollection.Count > 0)
                            nextSequenceToken = matchCollection[0].Value;
                        else
                            nextSequenceToken = null;
                    }
                    catch (InvalidSequenceTokenException e)
                    {
                        var matchCollection = Regex.Matches(e.Message, @"[0-9]{20,}");
                        if (matchCollection.Count > 0)
                            nextSequenceToken = matchCollection[0].Value;
                        else
                            nextSequenceToken = null;
                    }
                    catch (OperationAbortedException)
                    {
                        LogLog.Debug(typeof(CloudWatchLogsClientWrapper), "Task lost due to conflicting operation");
                    }
                }
                return ret;
            }

        }
    }

    internal class LockObject
    {
        private int _id;

        public LockObject()
        {
            _id = new Random().Next();
        }
    }
}