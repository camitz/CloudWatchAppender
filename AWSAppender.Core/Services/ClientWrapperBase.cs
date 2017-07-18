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
using log4net.Util;

namespace AWSAppender.Core.Services
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
                clientConfig = (TConfig)Activator.CreateInstance(typeof(TConfig));


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
                        else if (clientConfig.RegionEndpoint != null || clientConfig.ServiceURL != null)
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
                throw new AWSAppenderException("Couldn't create Amazon client.");

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
                                     if (!nestedTask.Wait(30000))//should be configurable
                                     {
                                         tokenSource.Cancel();
                                         LogLog.Error(GetType(),
                                             String.Format("Appender timed out while submitting to CloudWatch. Exception (if any): {0}", nestedTask.Exception), nestedTask.Exception);
                                     }
                                 }
                                 catch (Exception e)
                                 {
                                     LogLog.Error(GetType(),
                                         String.Format("Appender encountered an error while submitting to cloudwatch. {0}", e.Message), e);
                                 }

                                 superTask.ContinueWith(t =>
                                                        {
                                                            Task task2;
                                                            ServiceTasks.Tasks.TryRemove(superTask.Id, out task2);
                                                            LogLog.Debug(GetType(), "AWSAppender complete");
                                                            if (superTask.Exception != null)
                                                                LogLog.Error(GetType(),
                                                                    string.Format(
                                                                        "IAWSAppender encountered an error while submitting. {0}",
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

    internal class AWSAppenderException : Exception
    {
        public AWSAppenderException(string msg, Exception innerException)
            : base(msg, innerException)
        {

        }

        public AWSAppenderException(string msg)
            : base(msg)
        {
        }
    }
}
