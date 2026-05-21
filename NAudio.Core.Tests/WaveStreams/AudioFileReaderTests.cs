using NAudio.Wave;
using NUnit.Framework;
using System.IO;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class AudioFileReaderTests
    {
        [Test]
        [Category("IntegrationTest")]
        public void CanBeDisposedMoreThanOnce()
        {
            var path = @"..\..\..\SampleData\Drums\closed-hat-trimmed.wav";
            if (!File.Exists(path))
                Assert.Ignore("test file not found");
            var reader = new AudioFileReader(path);
            reader.Dispose();
            Assert.DoesNotThrow(() => reader.Dispose());
        }
    }
}
