using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NAudioTests.MediaFoundation
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class MediaFoundationReaderTests
    {
        [Test]
        public void CanReadAnAac()
        {
            var testFile = @"C:\Users\mheath\Downloads\NAudio\AAC\halfspeed.aac";
            if (!File.Exists(testFile)) Assert.Ignore("Missing test file");
            var reader = new MediaFoundationReader(testFile);
            Console.WriteLine(reader.WaveFormat);
            var buffer = new Span<byte>(new byte[reader.WaveFormat.AverageBytesPerSecond]);
            int bytesRead;
            long total = 0;
            while((bytesRead = reader.Read(buffer)) > 0)
            {
                total += bytesRead;
            }
            Assert.IsTrue(total > 0);
        }
    }

    [TestFixture]
    [Category("IntegrationTest")]
    public class MediaFoundationEncoderTests
    {
        [Test]
        public void CanEncodeLargeGSM610FileToMp3()
        {
            string fileInPath = @"C:\Users\mheath\Downloads\CH48_17002346_884_1.wav";
            string fileOutPath = @"C:\Users\mheath\Downloads\CH48_17002346_884_1.mp3";
            if (!File.Exists(fileInPath)) Assert.Ignore("Missing test file"); ;
            Stopwatch sw = Stopwatch.StartNew();
            using (var wavToConvert = new WaveFileReader(fileInPath))
            using (var converter = WaveFormatConversionStream.CreatePcmStream(wavToConvert))
            {
                Console.WriteLine($"Format in = {wavToConvert.WaveFormat}, Sample rate {wavToConvert.WaveFormat.SampleRate}");
                Console.WriteLine($"Format out = {converter.WaveFormat}, Sample rate {converter.WaveFormat.SampleRate}");

                var mediaType = MediaFoundationEncoder.SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, converter.WaveFormat, 0);
                if (mediaType == null) throw new InvalidOperationException("No suitable MP3 encoders available");
                Console.WriteLine($"MediaType = {(mediaType.AverageBytesPerSecond * 8)/1000}kbps, Sample rate {mediaType.SampleRate}, Channels: {mediaType.ChannelCount}");
                using (var encoder = new MediaFoundationEncoder(mediaType))
                {
                    // do a whole minute at a time - makes it faster on long files
                    // n.b. tried 10 minutes, didn't result in any noticable improvement
                    // limitation is now mostly the ACM GSM610 decoder
                    encoder.DefaultReadBufferSize = converter.WaveFormat.AverageBytesPerSecond * 60; 
                    encoder.Encode(fileOutPath, converter);
                }
                //MediaFoundationEncoder.EncodeToMp3(converter, fileOutPath, 2250*8);
            }
            Console.WriteLine($"Converted in {sw.Elapsed}");
        }
    }
}
