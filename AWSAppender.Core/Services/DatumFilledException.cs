using System;

namespace AWSAppender.Core.Services
{
    public class DatumFilledException : InvalidOperationException
    {
        public DatumFilledException(string message)
            : base(message)
        {

        }
    }
}