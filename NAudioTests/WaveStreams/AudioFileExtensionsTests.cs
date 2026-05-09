using NAudio;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    public class AudioFileExtensionsTests
    {
        [Test]
        public void LowerCaseWave()
        {
            var enumVal = ".wav".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.WAV);
        }

        [Test]
        public void MixedCaseWave()
        {
            var enumVal = ".Wav".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.WAV);
        }

        [Test]
        public void UpperCaseWave()
        {
            var enumVal = ".WAV".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.WAV);
        }

        [Test]
        public void LowerCaseMp3()
        {
            var enumVal = ".mp3".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.MP3);
        }

        [Test]
        public void MixedCaseMp3()
        {
            var enumVal = ".Mp3".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.MP3);
        }

        [Test]
        public void UpperCaseMp3()
        {
            var enumVal = ".MP3".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.MP3);
        }

        [Test]
        public void LowerCaseAiff()
        {
            var enumVal = ".aiff".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void MixedCaseAiff()
        {
            var enumVal = ".Aiff".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void UpperCaseAiff()
        {
            var enumVal = ".AIFF".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void LowerCaseShortAiff()
        {
            var enumVal = ".aif".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void MixedCaseShortAiff()
        {
            var enumVal = ".Aif".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void UpperCaseShortAiff()
        {
            var enumVal = ".AIF".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.AIFF);
        }

        [Test]
        public void NullValue()
        {
            var enumVal = ((string)null).GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.Unknown);
        }

        [Test]
        public void EmptyValue()
        {
            var enumVal = "".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.Unknown);
        }

        [Test]
        public void UnknownValue()
        {
            var enumVal = ".abc".GetFormatFromFileExt();

            Assert.AreEqual(enumVal, AudioFileFormatEnum.Unknown);
        }
    }
}
