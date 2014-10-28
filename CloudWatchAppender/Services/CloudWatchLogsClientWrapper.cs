using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Runtime;

namespace CloudWatchAppender.Services
{
    public class CloudWatchLogsClientWrapper : CloudWatchClientWrapperBase<AmazonCloudWatchLogsClient>
    {
        private readonly Dictionary<string, string> _validatedGroupNames = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _validatedStreamNames = new Dictionary<string, string>();
        private string _nextSequenceToken;
        public CloudWatchLogsClientWrapper(string endPoint, string accessKey, string secret)
            : base(endPoint, accessKey, secret)
        {
        }

        internal void QueuePutLogRequest(PutLogEventsRequest putLogEventsRequest)
        {

            QueueRequest(() => PutLogEvents(putLogEventsRequest));
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
                _validatedGroupNames.Add(putLogEventsRequest.LogGroupName, putLogEventsRequest.LogGroupName);
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
                _validatedStreamNames.Add(putLogEventsRequest.LogStreamName, putLogEventsRequest.LogStreamName);
            }

            try
            {
                return PutWithSequenceToken(putLogEventsRequest, _nextSequenceToken);
            }
            catch (DataAlreadyAcceptedException e)
            {
                return PutWithSequenceToken(putLogEventsRequest, Regex.Matches(e.Message, @"[0-9]{20,}")[0].Value);
            }
            catch (InvalidSequenceTokenException e)
            {
                return PutWithSequenceToken(putLogEventsRequest, Regex.Matches(e.Message, @"[0-9]{20,}")[0].Value);
            }
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