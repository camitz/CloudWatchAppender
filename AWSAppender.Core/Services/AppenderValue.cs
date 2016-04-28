//using Amazon.CloudWatch;

using System;

namespace AWSAppender.Core.Services
{
    public class AppenderValue
    {
        public string Name;
        public double? dValue;
        public string sValue;
        public DateTimeOffset? Time;
    }

}