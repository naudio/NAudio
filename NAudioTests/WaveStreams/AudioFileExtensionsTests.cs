using NAudio;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    public class AudioFileExtensionsTests
    {
        [Test]
        public void LowerCaseWave()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".wav");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.WAV);
        }

        [Test]
        public void MixedCaseWave()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".Wav");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.WAV);
        }

        [Test]
        public void UpperCaseWave()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".WAV");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.WAV);
        }

        [Test]
        public void LowerCaseMp3()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".mp3");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.MP3);
        }

        [Test]
        public void MixedCaseMp3()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".Mp3");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.MP3);
        }

        [Test]
        public void UpperCaseMp3()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".MP3");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.MP3);
        }

        [Test]
        public void LowerCaseAiff()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".aiff");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void MixedCaseAiff()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".Aiff");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void UpperCaseAiff()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".AIFF");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void LowerCaseShortAiff()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".aif");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void MixedCaseShortAiff()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".Aif");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void UpperCaseShortAiff()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".AIF");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void NullValue()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(null);

            Assert.AreEqual(enumVal, AudioFileFormatEnum.Unknown);
        }

        [Test]
        public void EmptyValue()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt("");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.Unknown);
        }

        [Test]
        public void UnknownValue()
        {
            var converter = new AudioFileExtensions();

            var enumVal = converter.GetFormatFromFileExt(".abc");

            Assert.AreEqual(enumVal, AudioFileFormatEnum.Unknown);
        }
    }
}
