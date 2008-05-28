using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudioTests
{
    [TestFixture]
    public class AudioClientTests
    {
        [Test]
        public void CanGetMixFormat()
        {
            // don't need to initialize before asking for MixFormat
            Console.WriteLine("Mix Format: {0}", GetAudioClient().MixFormat);
        }

        [Test]
        public void CanInitialize()
        {
            InitializeClient();        
        }

        [Test]
        public void CanGetAudioRenderClient()
        {
            Assert.IsNotNull(InitializeClient().AudioRenderClient);
        }


        [Test]
        public void CanGetBufferSize()
        {
            Console.WriteLine("Buffer Size: {0}", InitializeClient().BufferSize);
        }

        [Test]
        public void CanGetCurrentPadding()
        {
            Console.WriteLine("CurrentPadding: {0}", InitializeClient().CurrentPadding);
        }

        [Test]
        public void CanGetDefaultDevicePeriod()
        {
            // should not need initialization
            Console.WriteLine("DefaultDevicePeriod: {0}", GetAudioClient().DefaultDevicePeriod);
        }

        [Test]
        public void CanGetMinimumDevicePeriod()
        {
            // should not need initialization
            Console.WriteLine("MinimumDevicePeriod: {0}", GetAudioClient().MinimumDevicePeriod);
        }

        [Test]
        public void CanRequestIfFormatIsSupported()
        {
            WaveFormatExtensible closestMatch;
            closestMatch = GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared,
                new WaveFormatExtensible(44100, 16, 2));
            Assert.IsNotNull(closestMatch, "Closest Match");
            Assert.AreEqual(closestMatch.SampleRate, 44100);
            Assert.AreEqual(closestMatch.BitsPerSample, 32);
            Assert.AreEqual(closestMatch.Channels, 2);
        }

        private AudioClient InitializeClient()
        {
            AudioClient audioClient = GetAudioClient();
            WaveFormatExtensible waveFormat = audioClient.MixFormat;
            long refTimesPerSecond = 10000000;
            audioClient.Initialize(AudioClientShareMode.Shared,
                AudioClientStreamFlags.None,
                refTimesPerSecond,
                0,
                waveFormat,
                Guid.Empty);
            return audioClient;
        }

        private AudioClient GetAudioClient()
        {
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            MMDevice defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            AudioClient audioClient = defaultAudioEndpoint.AudioClient;
            Assert.IsNotNull(audioClient);
            return audioClient;
        }
    
    }
}
