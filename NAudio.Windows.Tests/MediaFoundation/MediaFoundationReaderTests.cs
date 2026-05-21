using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NAudioTests.MediaFoundation
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class MediaFoundationReaderTests
    {
        // Round-trips a generated signal through MediaFoundationEncoder.EncodeToMp3(Stream)
        // and StreamMediaFoundationReader. Both legs go through ComStream's source-generated
        // CCW path (Phase 2e' Step 5), so this test is the runtime check for the
        // QI-for-IID hazard (Phase 2f H3): if the IUnknown-vs-IStream vtable mismatch
        // recurred, MFCreateMFByteStreamOnStream would AV on the first IStream::Stat or
        // IStream::Seek invocation rather than failing visibly here.
        [Test]
        public void CanRoundTripStreamThroughMediaFoundationCcwPath()
        {
            MediaFoundationApi.Startup();

            using var encoded = new MemoryStream();
            var signal = new SignalGenerator(44100, 2) { Frequency = 1000, Gain = 0.25 }
                .Take(TimeSpan.FromSeconds(2));
            MediaFoundationEncoder.EncodeToMp3(signal.ToWaveProvider(), encoded, 96000);
            Assert.That(encoded.Length, Is.GreaterThan(0), "encode-to-stream produced no bytes");

            encoded.Position = 0;
            using var reader = new StreamMediaFoundationReader(encoded);
            Assert.That(reader.WaveFormat.SampleRate, Is.GreaterThan(0));

            var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
            long total = 0;
            int bytesRead;
            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += bytesRead;
            }
            Assert.That(total, Is.GreaterThan(0), "read-from-stream produced no bytes");
        }

        [Test]
        public void CanReadAnAac()
        {
            var testFile = @"C:\Users\mheath\Downloads\NAudio\AAC\halfspeed.aac";
            if (!File.Exists(testFile)) Assert.Ignore("Missing test file");
            var reader = new MediaFoundationReader(testFile);
            Console.WriteLine(reader.WaveFormat);
            var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
            int bytesRead;
            long total = 0;
            while((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += bytesRead;
            }
            Assert.That(total, Is.GreaterThan(0));
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
