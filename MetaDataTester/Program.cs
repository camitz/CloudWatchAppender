using System;
using System.Collections.Generic;
using CloudWatchAppender.Services;

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

            foreach (var key in keys)
                Console.WriteLine(string.Format("{0}: {1}",
                    InstanceMetaDataReader.Instance.MetaDataKeyLookup[key],
                    InstanceMetaDataReader.Instance.GetMetaData(key)));
        }


    }
}
