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
        public void DefaultFormatIsSupported()
        {
            AudioClient client = GetAudioClient();
            WaveFormatExtensible defaultFormat = client.MixFormat;
            CheckFormatSupported(client, defaultFormat);
        }

        [Test]
        public void CanRequestIfFormatIsSupportedExtensible44100()
        {
            WaveFormatExtensible desiredFormat = new WaveFormatExtensible(44100, 32, 2);
            Console.Write(desiredFormat);
            CheckFormatSupported(GetAudioClient(), desiredFormat);
        }

        [Test]
        public void CanRequestIfFormatIsSupportedExtensible48000()
        {
            WaveFormatExtensible desiredFormat = new WaveFormatExtensible(48000, 32, 2);
            Console.Write(desiredFormat);
            CheckFormatSupported(GetAudioClient(), desiredFormat);
        }

        [Test]
        public void CanRequestIfFormatIsSupportedExtensible48000_16bit()
        {
            WaveFormatExtensible desiredFormat = new WaveFormatExtensible(48000, 16, 2);
            Console.Write(desiredFormat);
            CheckFormatSupported(GetAudioClient(), desiredFormat);
        }


        [Test]
        public void CanRequestIfFormatIsSupportedPCMStereo()
        {
            CheckFormatSupported(GetAudioClient(), new WaveFormat(44100, 16, 2));
        }

        [Test]
        public void CanRequestIfFormatIsSupported8KHzMono()
        {
            CheckFormatSupported(GetAudioClient(), new WaveFormat(8000, 16, 1));
        }

        [Test]
        public void CanRequest48kHz16BitStereo()
        {
            CheckFormatSupported(GetAudioClient(), new WaveFormat(48000, 16, 2));
        }

        [Test]
        public void CanRequest48kHz16BitMono()
        {
            CheckFormatSupported(GetAudioClient(), new WaveFormat(48000, 16, 1));
        }


        [Test]
        public void CanRequestIfFormatIsSupportedIeee()
        {
            CheckFormatSupported(GetAudioClient(), WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        }

        private void CheckFormatSupported(AudioClient audioClient, WaveFormat waveFormat)
        {
            WaveFormat closestMatch;
            closestMatch = GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared,
                waveFormat);
            Assert.IsNotNull(closestMatch, "Closest Match");
            Assert.AreEqual(closestMatch.SampleRate, waveFormat.SampleRate, "Sample Rate");
            Assert.AreEqual(closestMatch.BitsPerSample, waveFormat.BitsPerSample, "BitsPerSample");
            Assert.AreEqual(closestMatch.Channels, waveFormat.Channels, "Channels");
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
