using System;
using Amazon;
using Amazon.Runtime;

namespace AWSAppender.Core.Services
{
    static class AWSClientFactoryWrapper<TClient> where TClient : AmazonServiceClient
    {
        public static AmazonServiceClient CreateServiceClient(ClientConfig cloudWatchConfig)
        {
            return (TClient)Activator.CreateInstance(typeof(TClient), new[] { cloudWatchConfig });
        }

        public static AmazonServiceClient CreateServiceClient(RegionEndpoint regionEndpoint)
        {
            return (TClient)Activator.CreateInstance(typeof(TClient), new[] { regionEndpoint });
        }

        public static AmazonServiceClient CreateServiceClient(string accessKey, string secret, RegionEndpoint regionEndpoint)
        {
            return (TClient)Activator.CreateInstance(typeof(TClient), new object[] { accessKey, secret, regionEndpoint });
        }

        public static AmazonServiceClient CreateServiceClient(string accessKey, string secret)
        {
            return (TClient)Activator.CreateInstance(typeof(TClient), new[] { accessKey, secret });
        }

        public static AmazonServiceClient CreateServiceClient(string accessKey, string secret, ClientConfig clientConfig)
        {
            return (TClient)Activator.CreateInstance(typeof(TClient), new object[] { accessKey, secret, clientConfig });
        }

        public static AmazonServiceClient CreateServiceClient()
        {
            return (TClient)Activator.CreateInstance(typeof(TClient));
        }
    }
}