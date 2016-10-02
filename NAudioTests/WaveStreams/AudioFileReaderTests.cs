using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class AudioFileReaderTests
    {
        [Test]
        [Category("IntegrationTest")]
        public void CanBeDisposedMoreThanOnce()
        {
            var reader = new AudioFileReader(@"..\..\..\SampleData\Drums\closed-hat-trimmed.wav");
            reader.Dispose();
            Assert.DoesNotThrow(() => reader.Dispose());
        }
    }
}
