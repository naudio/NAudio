using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Dmo;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudioTests
{
    [TestFixture]
    public class DmoTests
    {
        [Test]
        public void CanEnumerateAudioEffects()
        {
            Console.WriteLine("Audio Effects:");
            foreach (string name in DmoEnumerator.GetAudioEffectNames())
            {
                Console.WriteLine(name);
            }
        }

        [Test]
        public void CanEnumerateAudioEncoders()
        {
            Console.WriteLine("Audio Encoders:");
            foreach (string name in DmoEnumerator.GetAudioEncoderNames())
            {
                Console.WriteLine(name);
            }
        }

        [Test]
        public void CanEnumerateAudioDecoders()
        {
            Console.WriteLine("Audio Decoders:");
            foreach (string name in DmoEnumerator.GetAudioDecoderNames())
            {
                Console.WriteLine(name);
            }
        }

        [Test]
        public void CanCreateResamplerMediaObject()
        {
            Resampler resampler = new Resampler();
        }

        [Test]
        public void CanExamineInputTypesOnResampler()
        {
            Resampler resampler = new Resampler();
            Assert.AreEqual(resampler.MediaObject.InputStreamCount,1);
            foreach (DmoMediaType mediaType in resampler.MediaObject.GetInputTypes(0))
            {
                Console.WriteLine(String.Format("{0}:{1}:{2}",
                    mediaType.MajorTypeName,
                    mediaType.SubTypeName,
                    mediaType.FormatTypeName));
            }
        }

        [Test]
        public void CanExamineOutputTypesOnResampler()
        {
            Resampler resampler = new Resampler();
            Assert.AreEqual(resampler.MediaObject.OutputStreamCount, 1);
            foreach (DmoMediaType mediaType in resampler.MediaObject.GetOutputTypes(0))
            {
                Console.WriteLine(String.Format("{0}:{1}:{2}",
                    mediaType.MajorTypeName,
                    mediaType.SubTypeName,
                    mediaType.FormatTypeName));
            }
        }

        [Test]
        public void ResamplerSupports16BitPCM41000Input()
        {
            WaveFormat waveFormat = new WaveFormat(44100, 16, 2);
            Assert.IsTrue(IsResamplerInputFormatSupported(waveFormat));
        }

        [Test]
        public void ResamplerSupports16BitPCM8000Input()
        {
            WaveFormat waveFormat = new WaveFormat(8000, 16, 2);
            Assert.IsTrue(IsResamplerInputFormatSupported(waveFormat));
        }

        [Test]
        public void ResamplerSupportsIEEE44100Input()
        {
            WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100,2);
            Assert.IsTrue(IsResamplerInputFormatSupported(waveFormat));
        }

        [Test]
        public void ResamplerSupportsIEEE8000Input()
        {
            WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(8000, 2);
            Assert.IsTrue(IsResamplerInputFormatSupported(waveFormat));
        }

        [Test]
        public void ResamplerSupports8000To44100IEEE()
        {
            WaveFormat inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(8000, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            Assert.IsTrue(IsResamplerConversionSupported(inputFormat, outputFormat));
        }

        [Test]
        public void ResamplerSupports41000To48000IEEE()
        {
            WaveFormat inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            Assert.IsTrue(IsResamplerConversionSupported(inputFormat, outputFormat));
        }

        [Test]
        public void ResamplerSupportsPCMToIEEE()
        {
            WaveFormat inputFormat = new WaveFormat(44100,16,2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            Assert.IsTrue(IsResamplerConversionSupported(inputFormat, outputFormat));
        }

        [Test]
        public void ResamplerCanGetInputAndOutputBufferSizes()
        {
            Resampler resampler = new Resampler();
            resampler.MediaObject.SetInputWaveFormat(0,WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            resampler.MediaObject.SetOutputWaveFormat(0,WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            MediaObjectSizeInfo inputSizeInfo = resampler.MediaObject.GetInputSizeInfo(0);
            Assert.IsNotNull(inputSizeInfo, "Input Size Info");
            Console.WriteLine(inputSizeInfo.ToString());
            MediaObjectSizeInfo outputSizeInfo = resampler.MediaObject.GetOutputSizeInfo(0);
            Assert.IsNotNull(outputSizeInfo, "Output Size Info");
            Console.WriteLine(outputSizeInfo.ToString());
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

