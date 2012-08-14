using System.IO;
using System.Net;

namespace CloudWatchAppender
{
    static class AWSMetaDataReader
    {
        public static string GetInstanceID()
        {
            try
            {
                return new StreamReader(
                    WebRequest.Create("http://169.254.169.254/latest/meta-data/instance-id")
                        .GetResponse()
                        .GetResponseStream(), true)
                    .ReadToEnd();
            }
            catch (WebException)
            {
                return null;
            }
        }
    }
}