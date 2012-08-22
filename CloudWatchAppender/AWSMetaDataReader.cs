using System.IO;
using System.Net;

namespace CloudWatchAppender
{
    static class AWSMetaDataReader
    {
        private static string _readToEnd;

        public static string GetInstanceID()
        {
            try
            {
                if (string.IsNullOrEmpty(_readToEnd))
                    _readToEnd = new StreamReader(
                        WebRequest.Create("http://169.254.169.254/latest/meta-data/instance-id")
                            .GetResponse()
                            .GetResponseStream(), true)
                        .ReadToEnd();

                return _readToEnd;
            }
            catch (WebException)
            {
                return null;
            }
        }
    }
}