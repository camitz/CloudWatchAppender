using System;
using Amazon.CloudWatch;
using log4net.Util.TypeConverters;

namespace AWSAppender.CloudWatch.TypeConverters
{
    public class StandardUnitConverter :  IConvertFrom
    {
        public bool CanConvertFrom(Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public object ConvertFrom(object source)
        {
            return StandardUnit.FindValue(source as string);
        }
    }
}