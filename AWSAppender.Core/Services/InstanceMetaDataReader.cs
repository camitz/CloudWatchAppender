using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AWSAppender.Core.Services
{
    public static class MetaDataKeys
    {
        public const string amiid = "amiid";
        public const string amilaunchindex = "amilaunchindex";
        public const string amimanifestpath = "amimanifestpath";
        public const string instanceid = "instanceid";
        public const string instancetype = "instancetype";
        public const string kernelid = "kernelid";
        public const string localhostname = "localhostname";
        public const string localipv4 = "localipv4";
        public const string mac = "mac";
        public const string availabilityzone = "placement/availabilityzone";
        public const string productcodes = "productcodes";
        public const string publichostname = "publichostname";
        public const string publicipv4 = "publicipv4";
        public const string publickeys = "publickeys";
        public const string reservationid = "reservationid";
    }


    public interface IInstanceMetaDataReader
    {
        string GetMetaData(string key, out bool error);

        [Obsolete]
        string GetInstanceID();

        IDictionary<string, string> MetaDataKeyLookup { get; }
    }

    public class InstanceMetaDataReader : IInstanceMetaDataReader
    {
        private const string serviceUrl = "http://169.254.169.254/latest/meta-data/";

        private Dictionary<string, string> _metaDataKeys = new Dictionary<string, string>
                        {
                                {"amiid", "ami-id"},
                                {"amilaunchindex", "ami-launch-index"},
                                {"amimanifestpath", "ami-manifest-path"},
                                {"instanceid", "instance-id"},
                                {"instancetype", "instance-type"},
                                {"kernelid", "kernel-id"},
                                {"localhostname", "local-hostname"},
                                {"localipv4", "local-ipv4"},
                                {"mac", "mac"},
                                {"availabilityzone", "placement/availability-zone"},
                                {"productcodes", "product-codes"},
                                {"publichostname", "public-hostname"},
                                {"publicipv4", "public-ipv4"},
                                {"publickeys", "public-keys"},
                                {"reservationid", "reservation-id"}
                        };

        public IDictionary<string, string> MetaDataKeyLookup { get { return _metaDataKeys; } }

        private Dictionary<string, string> _cachedValues = new Dictionary<string, string>();

        private Dictionary<string, Task> _pendingTasks = new Dictionary<string, Task>();

        private Dictionary<string, int> _attempts = new Dictionary<string, int>();

        [Obsolete]
        public string GetInstanceID()
        {
            bool error;
            return GetMetaData(MetaDataKeys.instanceid, out error);
        }

        public string GetMetaData(string key, out bool outError)
        {
            if (!_metaDataKeys.ContainsKey(key))
                throw new InvalidOperationException(string.Format("Meta data key {0} is not supported or does not exist.", key));

            outError = false;
            var error = false;

            try
            {
                if (_pendingTasks.ContainsKey(key))
                {
                    Debug.WriteLine("Waiting for pending {0}", key);
                    return
                        _pendingTasks[key].ContinueWith(x =>
                                                            {
                                                                Debug.WriteLine("Pending {0} completed", key);

                                                                if (_cachedValues.ContainsKey(key))
                                                                    return _cachedValues[key];

                                                                return null;
                                                            })
                                                            .Result;
                }

                if (!_attempts.ContainsKey(key))
                    _attempts[key] = 0;

                if (!_cachedValues.ContainsKey(key))
                {
                    var uri = serviceUrl + _metaDataKeys[key];
                    Debug.WriteLine("Requesting {0}", uri);

                    var tokenSource = new CancellationTokenSource();
                    var ct = tokenSource.Token;


                    Stream responseStream = null;

                    var task1 =
                    _pendingTasks[key] =
                        Task.Factory.StartNew(() =>
                                                  {
                                                      if (++_attempts[key] > 10)
                                                          _cachedValues[key] = key+":MaxAttemptsExceeded";

                                                      var task =
                                                          Task.Factory.StartNew(() =>
                                                                                    {
                                                                                        try
                                                                                        {
                                                                                            responseStream =
                                                                                                WebRequest.Create(uri)
                                                                                                    .GetResponse()
                                                                                                    .GetResponseStream();
                                                                                        }
                                                                                        catch (Exception)
                                                                                        {
                                                                                            error = true;
                                                                                        }
                                                                                    }, ct);

                                                      if (!task.Wait(2000))
                                                          tokenSource.Cancel();

                                                      if (responseStream != null)
                                                      {
                                                          var s = new StreamReader(responseStream, true).ReadToEnd();
                                                          if (!string.IsNullOrEmpty(s))
                                                          {
                                                              _cachedValues[key] = s;
                                                              _attempts[key] = 0;
                                                          }
                                                          else
                                                              error = true;
                                                      }
                                                      else
                                                          error = true;
                                                  });

                    var result = task1
                            .ContinueWith(x =>
                                          {
                                              try
                                              {
                                                  _pendingTasks.Remove(key);
                                              }
                                              catch (Exception) { }

                                              if (error || !_cachedValues.ContainsKey(key))
                                              {
                                                  error = true;
                                                  return "error_" + key;
                                              }
                                              Debug.WriteLine(string.Format("Got {0}: {1}", key, _cachedValues[key]));

                                              return _cachedValues[key];
                                          })
                            .Result;

                    outError = error;
                    return result;
                }

                Debug.WriteLine(string.Format("Returning cached {0}: {1}", key, _cachedValues[key]));

                return _cachedValues[key];
            }
            catch (WebException)
            {
                return null;
            }
        }

        private static IInstanceMetaDataReader _instance;

        public static IInstanceMetaDataReader Instance
        {
            get { return _instance ?? (_instance = new InstanceMetaDataReader()); }
            set { _instance = value; }
        }
    }

}

