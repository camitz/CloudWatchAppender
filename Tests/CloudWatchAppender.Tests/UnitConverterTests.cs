using Amazon.CloudWatch;
using AWSAppender.CloudWatch.Services;
using NUnit.Framework;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class UnitConverterTests
    {

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void Test()
        {
            Assert.That(new StandardUnit("Bytes"), Is.EqualTo(new StandardUnit("Bytes")));

            Assert.That(UnitConverter.Convert(1000).From(StandardUnit.Kilobytes).To(StandardUnit.Bytes), Is.EqualTo(1024000.0));
            Assert.That(UnitConverter.Convert(1000).From(StandardUnit.Megabytes).To(StandardUnit.Bytes), Is.EqualTo(1048576000.0));
            Assert.That(() => { UnitConverter.Convert(1000).From(StandardUnit.Megabytes).To(StandardUnit.Seconds); }, Throws.Exception);

            Assert.That(UnitConverter.Convert(1000).From(StandardUnit.Terabytes).To(StandardUnit.Terabits), Is.EqualTo(8000));
            Assert.That(UnitConverter.Convert(1000).From(StandardUnit.TerabytesSecond).To(StandardUnit.TerabitsSecond), Is.EqualTo(8000));

            Assert.That(UnitConverter.Convert(1000).From(StandardUnit.Kilobytes).To(StandardUnit.Kilobytes), Is.EqualTo(1000));
            Assert.That(UnitConverter.Convert(1000).From(StandardUnit.Count).To(StandardUnit.Count), Is.EqualTo(1000));
        }
    }
}