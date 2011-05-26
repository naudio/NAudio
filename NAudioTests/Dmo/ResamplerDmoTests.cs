using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Dmo;
using NAudio.Wave;
using System.Diagnostics;
using NAudioTests.Utils;

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
            Resampler resampler = new Resampler();
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanExamineInputTypesOnResampler()
        {
            Resampler resampler = new Resampler();
            Assert.AreEqual(resampler.MediaObject.InputStreamCount, 1);
            foreach (DmoMediaType mediaType in resampler.MediaObject.GetInputTypes(0))
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
            Resampler resampler = new Resampler();
            Assert.AreEqual(resampler.MediaObject.OutputStreamCount, 1);
            foreach (DmoMediaType mediaType in resampler.MediaObject.GetOutputTypes(0))
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
            Assert.IsTrue(IsResamplerInputFormatSupported(waveFormat));
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupports16BitPCM8000Input()
        {
            WaveFormat waveFormat = new WaveFormat(8000, 16, 2);
            Assert.IsTrue(IsResamplerInputFormatSupported(waveFormat));
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupportsIEEE44100Input()
        {
            WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            Assert.IsTrue(IsResamplerInputFormatSupported(waveFormat));
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupportsIEEE8000Input()
        {
            WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(8000, 2);
            Assert.IsTrue(IsResamplerInputFormatSupported(waveFormat));
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupports8000To44100IEEE()
        {
            WaveFormat inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(8000, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            Assert.IsTrue(IsResamplerConversionSupported(inputFormat, outputFormat));
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupports41000To48000IEEE()
        {
            WaveFormat inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            Assert.IsTrue(IsResamplerConversionSupported(inputFormat, outputFormat));
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerSupportsPCMToIEEE()
        {
            WaveFormat inputFormat = new WaveFormat(44100, 16, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            Assert.IsTrue(IsResamplerConversionSupported(inputFormat, outputFormat));
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerCanGetInputAndOutputBufferSizes()
        {
            Resampler resampler = new Resampler();
            resampler.MediaObject.SetInputWaveFormat(0, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            resampler.MediaObject.SetOutputWaveFormat(0, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            MediaObjectSizeInfo inputSizeInfo = resampler.MediaObject.GetInputSizeInfo(0);
            Assert.IsNotNull(inputSizeInfo, "Input Size Info");
            Debug.WriteLine(inputSizeInfo.ToString());
            MediaObjectSizeInfo outputSizeInfo = resampler.MediaObject.GetOutputSizeInfo(0);
            Assert.IsNotNull(outputSizeInfo, "Output Size Info");
            Debug.WriteLine(outputSizeInfo.ToString());
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerCanCallProcessInput()
        {
            Resampler resampler = new Resampler();
            resampler.MediaObject.SetInputWaveFormat(0, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            resampler.MediaObject.SetOutputWaveFormat(0, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            using (MediaBuffer buffer = new MediaBuffer(44100 * 2 * 4))
            {
                buffer.Length = 8000;
                resampler.MediaObject.ProcessInput(0, buffer, DmoInputDataBufferFlags.None, 0, 0);
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void ResamplerCanCallProcessOutput()
        {
            Resampler resampler = new Resampler();
            WaveFormat inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            resampler.MediaObject.SetInputWaveFormat(0, inputFormat);
            resampler.MediaObject.SetOutputWaveFormat(0, outputFormat);
            resampler.MediaObject.AllocateStreamingResources();
            using (MediaBuffer inputBuffer = new MediaBuffer(inputFormat.AverageBytesPerSecond))
            {
                inputBuffer.Length = inputFormat.AverageBytesPerSecond / 10;
                Debug.WriteLine(String.Format("Input Length {0}", inputBuffer.Length));
                resampler.MediaObject.ProcessInput(0, inputBuffer, DmoInputDataBufferFlags.None, 0, 0);
                Debug.WriteLine(String.Format("Input Length {0}", inputBuffer.Length));
                Debug.WriteLine(String.Format("Input Lookahead {0}", resampler.MediaObject.GetInputSizeInfo(0).MaxLookahead));
                //Debug.WriteLine(String.Format("Input Max Latency {0}", resampler.MediaObject.GetInputMaxLatency(0)));
                using (DmoOutputDataBuffer outputBuffer = new DmoOutputDataBuffer(outputFormat.AverageBytesPerSecond))
                {
                    // one buffer for each output stream
                    resampler.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, new DmoOutputDataBuffer[] { outputBuffer });
                    Debug.WriteLine(String.Format("Converted length: {0}", outputBuffer.Length));
                    Debug.WriteLine(String.Format("Converted flags: {0}", outputBuffer.StatusFlags));
                    //Assert.AreEqual((int)(inputBuffer.Length * 48000.0 / inputFormat.SampleRate), outputBuffer.Length, "Converted buffer length");
                }

                using (DmoOutputDataBuffer outputBuffer = new DmoOutputDataBuffer(48000 * 2 * 4))
                {
                    // one buffer for each output stream
                    resampler.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, new DmoOutputDataBuffer[] { outputBuffer });
                    Debug.WriteLine(String.Format("Converted length: {0}", outputBuffer.Length));
                    Debug.WriteLine(String.Format("Converted flags: {0}", outputBuffer.StatusFlags));
                    //Assert.AreEqual((int)(inputBuffer.Length * 48000.0 / inputFormat.SampleRate), outputBuffer.Length, "Converted buffer length");
                }
            }
            resampler.MediaObject.FreeStreamingResources();
        }

        #region Helper Functions
        private bool IsResamplerInputFormatSupported(WaveFormat waveFormat)
        {
            Resampler resampler = new Resampler();
            return resampler.MediaObject.SupportsInputWaveFormat(0, waveFormat);
        }

        private bool IsResamplerConversionSupported(WaveFormat from, WaveFormat to)
        {
            Resampler resampler = new Resampler();
            // need to set an input format before we can ask for an output format to 
            resampler.MediaObject.SetInputWaveFormat(0, from);
            return resampler.MediaObject.SupportsOutputWaveFormat(0, to);
        }
        #endregion
    }
}
