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
        public void ResamplerSupports16BitPCM41000()
        {
            WaveFormat waveFormat = new WaveFormat(44100, 16, 2);
            Assert.IsTrue(ResamplerSupports(waveFormat));
        }

        [Test]
        public void ResamplerSupports16BitPCM8000()
        {
            WaveFormat waveFormat = new WaveFormat(8000, 16, 2);
            Assert.IsTrue(ResamplerSupports(waveFormat));
        }

        [Test]
        public void ResamplerSupportsIEEE44100()
        {
            WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100,2);
            Assert.IsTrue(ResamplerSupports(waveFormat));
        }

        [Test]
        public void ResamplerSupportsIEEE8000()
        {
            WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(8000, 2);
            Assert.IsTrue(ResamplerSupports(waveFormat));
        }


        private bool ResamplerSupports(WaveFormat waveFormat)
        {
            Resampler resampler = new Resampler();
            DmoMediaType mediaType = new DmoMediaType();
            int waveFormatExSize = 18;
            DmoInterop.MoInitMediaType(ref mediaType, waveFormatExSize);
            mediaType.SetWaveFormat(waveFormat);
            bool supported = resampler.MediaObject.SupportsInputType(0, mediaType);
            DmoInterop.MoFreeMediaType(ref mediaType);
            return supported;
        }
    }
}

