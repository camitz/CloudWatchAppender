using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CloudWatchAppender
{
    static class InstanceMetaDataReader
    {
        private const string serviceUrl = "http://169.254.169.254/latest/meta-data/";

        static class MetaDataKeys
        {
            public const string amiid = "ami-id";
            public const string amilaunchindex = "ami-launch-index";
            public const string amimanifestpath = "ami-manifest-path";
            public const string instanceid = "instance-id";
            public const string instancetype = "instance-type";
            public const string kernelid = "kernel-id";
            public const string localhostname = "local-hostname";
            public const string localipv4 = "local-ipv4";
            public const string mac = "mac";
            public const string availabilityzone = "placement/availability-zone";
            public const string productcodes = "product-codes";
            public const string publichostname = "public-hostname";
            public const string publicipv4 = "public-ipv4";
            public const string publickeys = "public-keys";
            public const string reservationid = "reservation-id";
        }

        private static Dictionary<string, string> _metaDataKeys = new Dictionary<string, string>
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

        public static IDictionary<string, string> MetaDataKeyLookup { get { return _metaDataKeys; } }

        private static Dictionary<string, string> _cachedValues = new Dictionary<string, string>();

        private static Dictionary<string, Task> _pendingTasks = new Dictionary<string, Task>();

        [Obsolete]
        public static string GetInstanceID()
        {
            return GetMetaData(MetaDataKeys.instanceid);
        }

        public static string GetMetaData(string key)
        {
            if (!_metaDataKeys.ContainsKey(key))
                throw new InvalidOperationException(string.Format("Meta data key {0} is not supported or does not exist.", key));

            try
            {
                if (_pendingTasks.ContainsKey(key))
                {
                    Debug.WriteLine(string.Format("Waiting for pending {0}", key));
                    return
                        _pendingTasks[key].ContinueWith(x =>
                                                            {
                                                                Debug.WriteLine(string.Format("Pending {0} completed", key));

                                                                if (_cachedValues.ContainsKey(key))
                                                                    return _cachedValues[key];

                                                                return null;
                                                            })
                                                            .Result;
                }

                if (!_cachedValues.ContainsKey(key))
                {
                    var uri = serviceUrl + _metaDataKeys[key];
                    Debug.WriteLine(string.Format("Requesting {0}", uri));

                    var tokenSource2 = new CancellationTokenSource();
                    CancellationToken ct = tokenSource2.Token;


                    Stream responseStream = null;

                    var task1 =
                    _pendingTasks[key] =
                        Task.Factory.StartNew(() =>
                                                  {
                                                      var task =
                                                          Task.Factory.StartNew(() =>
                                                                                    {
                                                                                        responseStream =
                                                                                            WebRequest.Create(uri)
                                                                                                .GetResponse()
                                                                                                .GetResponseStream();
                                                                                    }, ct);

                                                      if (!task.Wait(500))
                                                          tokenSource2.Cancel();

                                                      if (responseStream == null)
                                                          _cachedValues[key] = null;
                                                      else
                                                          _cachedValues[key] = new StreamReader(
                                                              responseStream, true)
                                                              .ReadToEnd();

                                                  });

                    return task1
                            .ContinueWith(x =>
                                              {
                                                  Debug.WriteLine(string.Format("Got {0}", key));
                                                  _pendingTasks.Remove(key);
                                                  return _cachedValues[key];
                                              })
                            .Result;


                }

                Debug.WriteLine(string.Format("Returning cached {0}", key));
                return _cachedValues[key];
            }
            catch (WebException)
            {
                return null;
            }
        }
    }
}

