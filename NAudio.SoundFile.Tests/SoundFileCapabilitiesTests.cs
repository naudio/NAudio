using System.Linq;
using NAudio.SoundFile;
using NUnit.Framework;

namespace NAudio.SoundFile.Tests
{
    [TestFixture]
    public class SoundFileCapabilitiesTests : SoundFileTestBase
    {
        [Test]
        public void WavIsAlwaysSupported()
        {
            Assert.That(SoundFileCapabilities.IsFormatSupported(SoundFileMajorFormat.Wav), Is.True);
        }

        [Test]
        public void SupportedMajorFormatsIncludesWav()
        {
            var formats = SoundFileCapabilities.GetSupportedMajorFormats();
            Assert.That(formats, Is.Not.Empty);
            Assert.That(formats.Any(f => f.Name.Contains("WAV", System.StringComparison.OrdinalIgnoreCase)), Is.True);
        }

        [Test]
        public void ReportsCodecAvailability()
        {
            // Not asserting true/false (build-dependent) — just that the
            // capability probe runs without throwing for optional codecs.
            Assert.DoesNotThrow(() =>
            {
                _ = SoundFileCapabilities.IsFormatSupported(SoundFileMajorFormat.Flac);
                _ = SoundFileCapabilities.IsFormatSupported(SoundFileMajorFormat.OggVorbis);
                _ = SoundFileCapabilities.IsFormatSupported(SoundFileMajorFormat.Opus);
                _ = SoundFileCapabilities.IsFormatSupported(SoundFileMajorFormat.Mp3);
            });
        }
    }
}
