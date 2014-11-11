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
        private readonly ConcurrentDictionary<string, string> _validatedGroupNames = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, string> _validatedStreamNames = new ConcurrentDictionary<string, string>();
        private volatile string _nextSequenceToken;
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
            if (!_validatedGroupNames.ContainsKey(putLogEventsRequest.LogGroupName))
            {
                try
                {
                    Client.CreateLogGroup(new CreateLogGroupRequest { LogGroupName = putLogEventsRequest.LogGroupName });
                }
                catch (ResourceAlreadyExistsException e)
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
                catch (ResourceAlreadyExistsException e)
                {
                }
                _validatedStreamNames.TryAdd(putLogEventsRequest.LogStreamName, putLogEventsRequest.LogStreamName);
            }


            AmazonWebServiceResponse ret = null;

            var nextSequenceToken = _nextSequenceToken;
            for (int i = 0; i < 10 && ret == null; i++)
            {
                try
                {
                    ret = PutWithSequenceToken(putLogEventsRequest, nextSequenceToken);
                }
                catch (DataAlreadyAcceptedException e)
                {
                    nextSequenceToken = Regex.Matches(e.Message, @"[0-9]{20,}")[0].Value;
                }
                catch (InvalidSequenceTokenException e)
                {
                    nextSequenceToken = Regex.Matches(e.Message, @"[0-9]{20,}")[0].Value;
                }
                catch (OperationAbortedException e)
                {
                    LogLog.Debug(typeof(CloudWatchLogsClientWrapper), "Task lost due to conflicting operation");
                }
            }

            return ret;
        }

        private AmazonWebServiceResponse PutWithSequenceToken(PutLogEventsRequest putLogEventsRequest, string sequenceToken)
        {
            putLogEventsRequest.SequenceToken = sequenceToken;
            var putLogEventsResponse = Client.PutLogEvents(putLogEventsRequest);
            _nextSequenceToken = putLogEventsResponse.NextSequenceToken;
            return putLogEventsResponse;
        }
    }
}