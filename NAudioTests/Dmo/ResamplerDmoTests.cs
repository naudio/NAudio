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
            DmoResampler dmoResampler = new DmoResampler();
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanExamineInputTypesOnResampler()
        {
            DmoResampler dmoResampler = new DmoResampler();
            Assert.AreEqual(dmoResampler.MediaObject.InputStreamCount, 1);
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
            Assert.AreEqual(dmoResampler.MediaObject.OutputStreamCount, 1);
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
            DmoResampler dmoResampler = new DmoResampler();
            dmoResampler.MediaObject.SetInputWaveFormat(0, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            dmoResampler.MediaObject.SetOutputWaveFormat(0, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            MediaObjectSizeInfo inputSizeInfo = dmoResampler.MediaObject.GetInputSizeInfo(0);
            Assert.IsNotNull(inputSizeInfo, "Input Size Info");
            Debug.WriteLine(inputSizeInfo.ToString());
            MediaObjectSizeInfo outputSizeInfo = dmoResampler.MediaObject.GetOutputSizeInfo(0);
            Assert.IsNotNull(outputSizeInfo, "Output Size Info");
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
