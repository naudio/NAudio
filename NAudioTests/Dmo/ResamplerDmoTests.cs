using NAudio.Dmo;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioTests.Utils;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NAudioTests.Dmo
{
    [TestFixture]
    public class ResamplerDmoTests
    {
        [SetUp]
        public void SetUp()
        {
            OSUtils.RequireVista();            
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateResamplerMediaObject()
        {
            DmoResampler dmoResampler = new DmoResampler();
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanExamineInputTypesOnResampler()
        {
            DmoResampler dmoResampler = new DmoResampler();
            Assert.That(dmoResampler.MediaObject.InputStreamCount, Is.EqualTo(1));
            foreach (DmoMediaType mediaType in dmoResampler.MediaObject.GetInputTypes(0))
            {
                Debug.WriteLine(String.Format("{0}:{1}:{2}",
                    mediaType.MajorTypeName,
                    mediaType.SubTypeName,
                    mediaType.FormatTypeName));
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanExamineOutputTypesOnResampler()
        {
            DmoResampler dmoResampler = new DmoResampler();
            Assert.That(dmoResampler.MediaObject.OutputStreamCount, Is.EqualTo(1));
            foreach (DmoMediaType mediaType in dmoResampler.MediaObject.GetOutputTypes(0))
            {
                Debug.WriteLine(String.Format("{0}:{1}:{2}",
                    mediaType.MajorTypeName,
                    mediaType.SubTypeName,
                    mediaType.FormatTypeName));
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupports16BitPCM41000Input()
        {
            WaveFormat waveFormat = new WaveFormat(44100, 16, 2);
            Assert.That(IsResamplerInputFormatSupported(waveFormat), Is.True);
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupports16BitPCM8000Input()
        {
            WaveFormat waveFormat = new WaveFormat(8000, 16, 2);
            Assert.That(IsResamplerInputFormatSupported(waveFormat), Is.True);
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupportsIEEE44100Input()
        {
            WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            Assert.That(IsResamplerInputFormatSupported(waveFormat), Is.True);
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupportsIEEE8000Input()
        {
            WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(8000, 2);
            Assert.That(IsResamplerInputFormatSupported(waveFormat), Is.True);
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupports8000To44100IEEE()
        {
            WaveFormat inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(8000, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            Assert.That(IsResamplerConversionSupported(inputFormat, outputFormat), Is.True);
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupports41000To48000IEEE()
        {
            WaveFormat inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            Assert.That(IsResamplerConversionSupported(inputFormat, outputFormat), Is.True);
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupportsPCMToIEEE()
        {
            WaveFormat inputFormat = new WaveFormat(44100, 16, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            Assert.That(IsResamplerConversionSupported(inputFormat, outputFormat), Is.True);
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerCanGetInputAndOutputBufferSizes()
        {
            DmoResampler dmoResampler = new DmoResampler();
            dmoResampler.MediaObject.SetInputWaveFormat(0, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            dmoResampler.MediaObject.SetOutputWaveFormat(0, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            MediaObjectSizeInfo inputSizeInfo = dmoResampler.MediaObject.GetInputSizeInfo(0);
            Assert.That(inputSizeInfo, Is.Not.Null, "Input Size Info");
            Debug.WriteLine(inputSizeInfo.ToString());
            MediaObjectSizeInfo outputSizeInfo = dmoResampler.MediaObject.GetOutputSizeInfo(0);
            Assert.That(outputSizeInfo, Is.Not.Null, "Output Size Info");
            Debug.WriteLine(outputSizeInfo.ToString());
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerCanCallProcessInput()
        {
            DmoResampler dmoResampler = new DmoResampler();
            dmoResampler.MediaObject.SetInputWaveFormat(0, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            dmoResampler.MediaObject.SetOutputWaveFormat(0, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            using (MediaBuffer buffer = new MediaBuffer(44100 * 2 * 4))
            {
                buffer.Length = 8000;
                dmoResampler.MediaObject.ProcessInput(0, buffer, DmoInputDataBufferFlags.None, 0, 0);
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerCanCallProcessOutput()
        {
            DmoResampler dmoResampler = new DmoResampler();
            WaveFormat inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            dmoResampler.MediaObject.SetInputWaveFormat(0, inputFormat);
            dmoResampler.MediaObject.SetOutputWaveFormat(0, outputFormat);
            dmoResampler.MediaObject.AllocateStreamingResources();
            using (MediaBuffer inputBuffer = new MediaBuffer(inputFormat.AverageBytesPerSecond))
            {
                inputBuffer.Length = inputFormat.AverageBytesPerSecond / 10;
                Debug.WriteLine(String.Format("Input Length {0}", inputBuffer.Length));
                dmoResampler.MediaObject.ProcessInput(0, inputBuffer, DmoInputDataBufferFlags.None, 0, 0);
                Debug.WriteLine(String.Format("Input Length {0}", inputBuffer.Length));
                Debug.WriteLine(String.Format("Input Lookahead {0}", dmoResampler.MediaObject.GetInputSizeInfo(0).MaxLookahead));
                //Debug.WriteLine(String.Format("Input Max Latency {0}", resampler.MediaObject.GetInputMaxLatency(0)));
                using (DmoOutputDataBuffer outputBuffer = new DmoOutputDataBuffer(outputFormat.AverageBytesPerSecond))
                {
                    // one buffer for each output stream
                    dmoResampler.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, new DmoOutputDataBuffer[] { outputBuffer });
                    Debug.WriteLine(String.Format("Converted length: {0}", outputBuffer.Length));
                    Debug.WriteLine(String.Format("Converted flags: {0}", outputBuffer.StatusFlags));
                    //Assert.AreEqual((int)(inputBuffer.Length * 48000.0 / inputFormat.SampleRate), outputBuffer.Length, "Converted buffer length");
                }

                using (DmoOutputDataBuffer outputBuffer = new DmoOutputDataBuffer(48000 * 2 * 4))
                {
                    // one buffer for each output stream
                    dmoResampler.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, new DmoOutputDataBuffer[] { outputBuffer });
                    Debug.WriteLine(String.Format("Converted length: {0}", outputBuffer.Length));
                    Debug.WriteLine(String.Format("Converted flags: {0}", outputBuffer.StatusFlags));
                    //Assert.AreEqual((int)(inputBuffer.Length * 48000.0 / inputFormat.SampleRate), outputBuffer.Length, "Converted buffer length");
                }
            }
            dmoResampler.MediaObject.FreeStreamingResources();
        }

        [Test]
        [Category("IntegrationTest")]
        public void Experimental_CanCreateOnOneThreadAndUseOnAnother()
        {
            const int inputRate = 44100;
            const int outputRate = 48000;
            const double frequency = 1000;
            const double durationSeconds = 0.5;

            var sourceBytes = GenerateSineWaveBytes(inputRate, 1, durationSeconds, frequency);
            var inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(inputRate, 1);
            var outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(outputRate, 1);
            var source = new RawSourceWaveStream(new MemoryStream(sourceBytes), inputFormat);

            ResamplerDmoStream resampler = null;
            int creationThreadId = -1;
            Exception createError = null;
            var createThread = new Thread(() =>
            {
                try
                {
                    creationThreadId = Environment.CurrentManagedThreadId;
                    resampler = new ResamplerDmoStream(source, outputFormat);
                }
                catch (Exception ex) { createError = ex; }
            }) { IsBackground = true };
            createThread.Start();
            createThread.Join();
            if (createError != null) throw createError;

            try
            {
                byte[] outputBytes = null;
                int readThreadId = -1;
                Exception readError = null;
                var readThread = new Thread(() =>
                {
                    try
                    {
                        readThreadId = Environment.CurrentManagedThreadId;
                        using var output = new MemoryStream();
                        var buffer = new byte[outputFormat.AverageBytesPerSecond / 100];
                        int read;
                        while ((read = resampler.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            output.Write(buffer, 0, read);
                        }
                        outputBytes = output.ToArray();
                    }
                    catch (Exception ex) { readError = ex; }
                }) { IsBackground = true };
                readThread.Start();
                readThread.Join();
                if (readError != null) throw readError;

                Assert.That(readThreadId, Is.Not.EqualTo(creationThreadId), "Test invalid: threads must differ");

                var samples = new float[outputBytes.Length / sizeof(float)];
                Buffer.BlockCopy(outputBytes, 0, samples, 0, samples.Length * sizeof(float));
                Assert.That(samples.Length, Is.GreaterThan(outputRate / 4), "Expected substantial output samples");

                var estimated = EstimateFrequencyByPositiveZeroCrossings(samples, outputRate);
                Assert.That(estimated, Is.InRange(frequency - 30, frequency + 30),
                    "Resampler created on thread A and read on thread B should still resample correctly");
            }
            finally
            {
                resampler?.Dispose();
                source.Dispose();
            }
        }

        private static byte[] GenerateSineWaveBytes(int sampleRate, int channels, double durationSeconds, double frequency)
        {
            var signal = new SignalGenerator(sampleRate, channels)
            {
                Type = SignalGeneratorType.Sin,
                Frequency = frequency,
                Gain = 0.8
            };
            var sampleCount = (int)(sampleRate * channels * durationSeconds);
            var samples = new float[sampleCount];
            signal.Read(samples.AsSpan());
            var bytes = new byte[samples.Length * sizeof(float)];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static double EstimateFrequencyByPositiveZeroCrossings(float[] samples, int sampleRate)
        {
            if (samples.Length < 3) return 0;
            int start = samples.Length / 10;
            int end = samples.Length - start;
            int crossings = 0;
            for (int n = Math.Max(start + 1, 1); n < end; n++)
            {
                if (samples[n - 1] <= 0 && samples[n] > 0) crossings++;
            }
            double seconds = (end - start) / (double)sampleRate;
            return seconds <= 0 ? 0 : crossings / seconds;
        }

        [Test]
        [Category("IntegrationTest")]
        public void Experimental_CanCreateOnStaThreadAndUseOnMtaThread()
        {
            const int inputRate = 44100;
            const int outputRate = 48000;
            const double frequency = 1000;
            const double durationSeconds = 0.5;

            var sourceBytes = GenerateSineWaveBytes(inputRate, 1, durationSeconds, frequency);
            var inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(inputRate, 1);
            var outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(outputRate, 1);
            var source = new RawSourceWaveStream(new MemoryStream(sourceBytes), inputFormat);

            ResamplerDmoStream resampler = null;
            int creationThreadId = -1;
            ApartmentState creationApartment = ApartmentState.Unknown;
            Exception createError = null;
            var createThread = new Thread(() =>
            {
                try
                {
                    creationThreadId = Environment.CurrentManagedThreadId;
                    creationApartment = Thread.CurrentThread.GetApartmentState();
                    resampler = new ResamplerDmoStream(source, outputFormat);
                }
                catch (Exception ex) { createError = ex; }
            }) { IsBackground = true };
            createThread.SetApartmentState(ApartmentState.STA);
            createThread.Start();
            createThread.Join();
            if (createError != null) throw createError;
            Assert.That(creationApartment, Is.EqualTo(ApartmentState.STA), "Create thread should be STA");

            try
            {
                byte[] outputBytes = null;
                int readThreadId = -1;
                ApartmentState readApartment = ApartmentState.Unknown;
                Exception readError = null;
                var readThread = new Thread(() =>
                {
                    try
                    {
                        readThreadId = Environment.CurrentManagedThreadId;
                        readApartment = Thread.CurrentThread.GetApartmentState();
                        using var output = new MemoryStream();
                        var buffer = new byte[outputFormat.AverageBytesPerSecond / 100];
                        int read;
                        while ((read = resampler.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            output.Write(buffer, 0, read);
                        }
                        outputBytes = output.ToArray();
                    }
                    catch (Exception ex) { readError = ex; }
                }) { IsBackground = true };
                readThread.SetApartmentState(ApartmentState.MTA);
                readThread.Start();
                readThread.Join();
                if (readError != null) throw readError;

                Assert.That(readThreadId, Is.Not.EqualTo(creationThreadId), "Test invalid: threads must differ");
                Assert.That(readApartment, Is.EqualTo(ApartmentState.MTA), "Read thread should be MTA");

                var samples = new float[outputBytes.Length / sizeof(float)];
                Buffer.BlockCopy(outputBytes, 0, samples, 0, samples.Length * sizeof(float));
                Assert.That(samples.Length, Is.GreaterThan(outputRate / 4), "Expected substantial output samples");

                var estimated = EstimateFrequencyByPositiveZeroCrossings(samples, outputRate);
                Assert.That(estimated, Is.InRange(frequency - 30, frequency + 30),
                    "Resampler created on STA thread and read on MTA thread should still resample correctly");
            }
            finally
            {
                resampler?.Dispose();
                source.Dispose();
            }
        }

        #region Helper Functions
        private bool IsResamplerInputFormatSupported(WaveFormat waveFormat)
        {
            DmoResampler dmoResampler = new DmoResampler();
            return dmoResampler.MediaObject.SupportsInputWaveFormat(0, waveFormat);
        }

        private bool IsResamplerConversionSupported(WaveFormat from, WaveFormat to)
        {
            DmoResampler dmoResampler = new DmoResampler();
            // need to set an input format before we can ask for an output format to 
            dmoResampler.MediaObject.SetInputWaveFormat(0, from);
            return dmoResampler.MediaObject.SupportsOutputWaveFormat(0, to);
        }
        #endregion
    }
}
