using System.IO;
using System.Net;

namespace CloudWatchAppender
{
    static class AWSMetaDataReader
    {
        private static string _instanceID;

        public static string GetInstanceID()
        {
            try
            {
                if (string.IsNullOrEmpty(_instanceID))
                    _instanceID = new StreamReader(
                        WebRequest.Create("http://169.254.169.254/latest/meta-data/instance-id")
                            .GetResponse()
                            .GetResponseStream(), true)
                        .ReadToEnd();

                return _instanceID;
            }
            catch (WebException)
            {
                return null;
            }
        }
    }
}