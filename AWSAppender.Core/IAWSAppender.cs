using System;
using System.Net;
using Amazon;
using Amazon.Runtime;

namespace AWSAppender.Core
{
    public interface IAWSAppender
    {
        string AccessKey { set; }
        string Secret { set; }
        string EndPoint { set; }
        string Timestamp { set; }
        bool ConfigOverrides { set; }
        string InstanceMetaDataReaderClass { get; set; }

        #region clientconfig
        string ProxyHost { get; set; }
        int ProxyPort { get; set; }
        int MaxIdleTime { get; set; }
        int ConnectionLimit { get; set; }
        bool UseNagleAlgorithm { get; set; }
        TimeSpan? ReadWriteTimeout { get; set; }
        //abstract string ServiceVersion { get; }
        SigningAlgorithm SignatureMethod {  set; }
        string SignatureVersion { get; set; }
        string UserAgent { get; }
        RegionEndpoint RegionEndpoint { set; }
        string ServiceURL { get; set; }
        bool UseHttp { get; set; }
        string AuthenticationRegion { get; set; }
        string AuthenticationServiceName { get; set; }
        int MaxErrorRetry { get; set; }
        bool LogResponse { get; set; }
        bool ReadEntireResponse { get; set; }
        int AWSBufferSize { get; set; }
        long ProgressUpdateInterval { get; set; }
        bool LogMetrics { get; set; }
        ICredentials ProxyCredentials { get; set; }
        TimeSpan? Timeout { get; set; }
        #endregion
    }
}