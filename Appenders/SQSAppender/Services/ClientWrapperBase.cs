//to core?

using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.Util;
using AWSAppender.Core.Services;
using log4net.Util;

namespace SQSAppender.Services
{
    public abstract class ClientWrapperBase<TConfig, TClient>
        where TConfig : ClientConfig
        where TClient : AmazonServiceClient
    {
        private string _endPoint;
        private string _accessKey;
        private string _secret;

        protected TClient Client { get; private set; }

        protected ClientWrapperBase(string endPoint, string accessKey, string secret, ClientConfig clientConfig)
        {
            _endPoint = endPoint;
            _accessKey = accessKey;
            _secret = secret;

            SetupClient(clientConfig);
        }


        private void SetupClient(ClientConfig clientConfig)
        {
            if (Client != null)
                return;

            if (clientConfig == null)
                    clientConfig =(TConfig) Activator.CreateInstance(typeof(TConfig));


            if (string.IsNullOrEmpty(_endPoint) && clientConfig.RegionEndpoint == null && ConfigurationManager.AppSettings["AWSServiceEndpoint"] != null)
                _endPoint = ConfigurationManager.AppSettings["AWSServiceEndpoint"];

            if (string.IsNullOrEmpty(_accessKey) && ConfigurationManager.AppSettings["AWSAccessKey"] != null)
                _accessKey = ConfigurationManager.AppSettings["AWSAccessKey"];

            if (string.IsNullOrEmpty(_secret) && ConfigurationManager.AppSettings["AWSSecretKey"] != null)
                _secret = ConfigurationManager.AppSettings["AWSSecretKey"];

            if (!string.IsNullOrEmpty(_endPoint))
            {
                if (_endPoint.StartsWith("http"))
                {
                    clientConfig.ServiceURL = _endPoint;
                }
                else
                {
                    clientConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(_endPoint);
                }
            }

            if (string.IsNullOrEmpty(_accessKey))
                try
                {
                    if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["AWSProfileName"]) || ProfileManager.ListProfileNames().Contains("default"))
                    {
                        if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["AWSRegion"]))
                            Client = (TClient)AWSClientFactoryWrapper<TClient>.CreateServiceClient();
                        else if (clientConfig.RegionEndpoint != null)
                            Client = (TClient)AWSClientFactoryWrapper<TClient>.CreateServiceClient(clientConfig);
                    }
                    else
                    {
                        foreach (var availableRole in InstanceProfileAWSCredentials.GetAvailableRoles())
                        {
                            LogLog.Debug(typeof(ClientWrapperBase<,>), "Role: " + availableRole);
                        }
                        Client = (TClient)AWSClientFactoryWrapper<TClient>.CreateServiceClient(clientConfig);
                    }
                }
                catch (AmazonServiceException e)
                {
                    LogLog.Debug(typeof(ClientWrapperBase<,>), "Exception caught while creating client", e);
                }
                catch (Exception e)
                {
                    LogLog.Debug(typeof(ClientWrapperBase<,>), "Exception caught while creating client", e);
                }


            if (Client == null && !string.IsNullOrEmpty(_accessKey))
                if (clientConfig != null)
                    Client = (TClient)AWSClientFactoryWrapper<TClient>.CreateServiceClient(_accessKey, _secret, clientConfig);
                else
                    Client = (TClient)AWSClientFactoryWrapper<TClient>.CreateServiceClient(_accessKey, _secret);

            if (Client == null)
                throw new CloudWatchAppenderException("Couldn't create Amazon client.");

        }


        protected void AddRequest(Func<AmazonWebServiceResponse> func)
        {
            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            try
            {
                Task superTask = null;
                superTask =
                    new Task(() =>
                             {
                                 var nestedTask =
                                     Task.Factory.StartNew(() =>
                                                           {
                                                               try
                                                               {
                                                                   var tmpCulture = Thread.CurrentThread.CurrentCulture;
                                                                   Thread.CurrentThread.CurrentCulture = new CultureInfo(
                                                                       "en-GB", false);

                                                                   LogLog.Debug(GetType(), "Sending");
                                                                   var response = func();
                                                                   LogLog.Debug(GetType(),
                                                                       "RequestID: " + response.ResponseMetadata.RequestId);

                                                                   Thread.CurrentThread.CurrentCulture = tmpCulture;
                                                               }
                                                               catch (Exception e)
                                                               {
                                                                   LogLog.Debug(GetType(), e.ToString());
                                                               }
                                                           }, ct);

                                 try
                                 {
                                     if (!nestedTask.Wait(30000))
                                     {
                                         tokenSource.Cancel();
                                         LogLog.Error(GetType(),
                                             "IsqsAppender timed out while submitting to CloudWatch. Exception (if any): {0}",
                                             nestedTask.Exception);
                                     }
                                 }
                                 catch (Exception e)
                                 {
                                     LogLog.Error(GetType(),
                                         "IsqsAppender encountered an error while submitting to cloudwatch. {0}", e);
                                 }

                                 superTask.ContinueWith(t =>
                                                        {
                                                            Task task2;
                                                            ServiceTasks.Tasks.TryRemove(superTask.Id, out task2);
                                                            LogLog.Debug(GetType(), "Cloudwatch complete");
                                                            if (superTask.Exception != null)
                                                                LogLog.Error(GetType(),
                                                                    string.Format(
                                                                        "IsqsAppender encountered an error while submitting to CloudWatch. {0}",
                                                                        superTask.Exception));
                                                        });
                             });

                if (ServiceTasks.Tasks == null)
                    ServiceTasks.Tasks = new ConcurrentDictionary<int, Task>();
                ServiceTasks.Tasks.TryAdd(superTask.Id, superTask);
                superTask.Start();
            }
            catch (Exception e)
            {
                LogLog.Error(GetType(),
                    string.Format(
                        "IsqsAppender encountered an error while submitting to cloudwatch. {0}", e));
            }
        }
    }

    static class AWSClientFactoryWrapper<TClient> where TClient : AmazonServiceClient
    {
        //to core?
        public static AmazonServiceClient CreateServiceClient(ClientConfig cloudWatchConfig) 
        {
            return (TClient)Activator.CreateInstance(typeof (TClient),new[]{cloudWatchConfig});
        }

        public static AmazonServiceClient CreateServiceClient(RegionEndpoint regionEndpoint)
        {
            return (TClient)Activator.CreateInstance(typeof(TClient), new[] { regionEndpoint });
        }

        public static AmazonServiceClient CreateServiceClient(string accessKey, string secret, RegionEndpoint regionEndpoint)
        {
            return (TClient)Activator.CreateInstance(typeof(TClient), new object[] { accessKey,secret,regionEndpoint });
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