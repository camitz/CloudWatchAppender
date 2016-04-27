using System;
using Amazon;
using log4net.Util.TypeConverters;

namespace CloudWatchAppender.TypeConverters
{
    public class RegionConverter :  IConvertFrom
    {
        public bool CanConvertFrom(Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public object ConvertFrom(object source)
        {
            return RegionEndpoint.GetBySystemName(source as string);
        }
    }
}