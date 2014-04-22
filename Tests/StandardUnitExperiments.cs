using System;
using System.Collections.Generic;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Services;
using NUnit.Framework;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class StandardUnitExperiments
    {



        [Test]
        public void CreateStandardUnit()
        {
            var type = typeof(StandardUnit);
            Assert.IsNull(type.GetConstructor(Type.EmptyTypes));
            var t = type.GetConstructors();
            foreach (var constructorInfo in t)
            {
                foreach (var parameterInfo in constructorInfo.GetParameters())
                {
                    if (parameterInfo.Name == "value")
                    {
                        object t2;
                        t2 = Activator.CreateInstance(type, new[] {"Kilobytes"});
                        Assert.That(t2,Is.TypeOf<StandardUnit>());
                        Assert.That(t2.ToString(), Is.EqualTo("Kilobytes"));
                    }
                }
            }
        }
    }
}