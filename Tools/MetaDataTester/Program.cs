using System;
using System.Collections.Generic;
using AWSAppender.Core.Services;

namespace MetaDataTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var keys = new List<string>
                        {
                            "amiid",
                            "amilaunchindex",
                            "amimanifestpath",
                            "instanceid",
                            "instancetype",
                            "kernelid",
                            "localhostname",
                            "localipv4",
                            "mac",
                            "availabilityzone",
                            "productcodes",
                            "publichostname",
                            "publicipv4",
                            "publickeys",
                            "reservationid"
                        };

            bool error;

            foreach (var key in keys)
                Console.WriteLine("{0}: {1}", InstanceMetaDataReader.Instance.MetaDataKeyLookup[key],
                    InstanceMetaDataReader.Instance.GetMetaData(key, out error));
        }


    }
}
