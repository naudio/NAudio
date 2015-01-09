using System;
using NUnit.Framework;
using NAudio.FileFormats.Mp3;
using NAudio.Wave;
using NAudio.Dmo;
using System.Diagnostics;
using System.IO;
using NAudioTests.Utils;

namespace NAudioTests.Dmo
{
    [TestFixture]
    public class DmoMp3FrameDecompressorTests
    {
        [SetUp]
        public void SetUp()
        {
            OSUtils.RequireVista();
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoMp3FrameDecompressor()
        {
            var mp3Format = new Mp3WaveFormat(44100,2,215,32000);
            var frameDecompressor = new DmoMp3FrameDecompressor(mp3Format);
            Assert.IsNotNull(frameDecompressor);
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanDecompressAnMp3()
        {
            var testFile = @"C:\Users\Public\Music\Coldplay\X&Y\01-Square One.mp3";
            if (!File.Exists(testFile))
            {
                Assert.Ignore("{0} not found", testFile);
            }
            using(var reader = new Mp3FileReader(testFile))
            {
                var frameDecompressor = new DmoMp3FrameDecompressor(reader.Mp3WaveFormat);
                Mp3Frame frame = null;
                var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    int decompressed = frameDecompressor.DecompressFrame(frame, buffer, 0);
                    Debug.WriteLine(String.Format("Decompressed {0} bytes to {1}", frame.FrameLength, decompressed));
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanExamineInputTypesOnMp3Decoder()
        {
            var decoder = new WindowsMediaMp3Decoder();
            Assert.AreEqual(decoder.MediaObject.InputStreamCount, 1);
            foreach (DmoMediaType mediaType in decoder.MediaObject.GetInputTypes(0))
            {
                Debug.WriteLine(String.Format("{0}:{1}:{2}",
                    mediaType.MajorTypeName,
                    mediaType.SubTypeName,
                    mediaType.FormatTypeName));
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanExamineOutputTypesOnDecoder()
        {
            var decoder = new WindowsMediaMp3Decoder();
            decoder.MediaObject.SetInputWaveFormat(0,new Mp3WaveFormat(44100, 2, 200, 32000));
            Assert.AreEqual(decoder.MediaObject.OutputStreamCount, 1);

            foreach (DmoMediaType mediaType in decoder.MediaObject.GetOutputTypes(0))
            {
                Debug.WriteLine(String.Format("{0}:{1}:{2}",
                    mediaType.MajorTypeName,
                    mediaType.SubTypeName,
                    mediaType.FormatTypeName));
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void WindowsMediaMp3DecoderSupportsStereoMp3()
        {
            WaveFormat waveFormat = new Mp3WaveFormat(44100, 2, 0, 32000);
            Assert.IsTrue(IsInputFormatSupported(waveFormat));
        }

        [Test]
        [Category("IntegrationTest")]
        [Ignore("Doesn't seem to be true anymore")]
        public void WindowsMediaMp3DecoderSupportsPcmOutput()
        {
            var waveFormat = new WaveFormat(44100, 2);
            Assert.IsTrue(IsOutputFormatSupported(waveFormat));
        }

        private bool IsInputFormatSupported(WaveFormat waveFormat)
        {
            var decoder = new WindowsMediaMp3Decoder();
            return decoder.MediaObject.SupportsInputWaveFormat(0, waveFormat);
        }

        private bool IsOutputFormatSupported(WaveFormat waveFormat)
        {
            var decoder = new WindowsMediaMp3Decoder();
            return decoder.MediaObject.SupportsOutputWaveFormat(0, waveFormat);
        }
    }
}
