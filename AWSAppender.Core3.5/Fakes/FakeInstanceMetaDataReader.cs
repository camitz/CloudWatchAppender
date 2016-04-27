using System.Collections.Generic;
using AWSAppender.Core.Services;

namespace CloudWatchAppender.Fakes
{

    class FakeInstanceMetaDataReader : IInstanceMetaDataReader
    {
        private Dictionary<string, string> _metaDataKeys = new Dictionary<string, string>
                                                           {
                                                               {"amiid", "fake-ami-id"},
                                                               {"amilaunchindex", "fake-ami-launch-index"},
                                                               {"amimanifestpath", "fake-ami-manifest-path"},
                                                               {"instanceid", "fake-instance-id"},
                                                               {"instancetype", "fake-instance-type"},
                                                               {"kernelid", "fake-kernel-id"},
                                                               {"localhostname", "fake-local-hostname"},
                                                               {"localipv4", "fake-local-ipv4"},
                                                               {"mac", "fake-mac"},
                                                               {"availabilityzone", "fake-placement/availability-zone"},
                                                               {"productcodes", "fake-product-codes"},
                                                               {"publichostname", "fake-public-hostname"},
                                                               {"publicipv4", "fake-public-ipv4"},
                                                               {"publickeys", "fake-public-keys"},
                                                               {"reservationid", "fake-reservation-id"}
                                                           };

        public string GetMetaData(string key,out bool error)
        {
            error = false;
            return _metaDataKeys[key];
        }

        public string GetInstanceID()
        {
            return _metaDataKeys["instanceid"];
        }

        public IDictionary<string, string> MetaDataKeyLookup { get { return _metaDataKeys; } }
    }
}