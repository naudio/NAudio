using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using NAudioTests.Utils;

namespace NAudioTests.Wasapi
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class AudioClientTests
    {
        [SetUp]
        public void SetUp()
        {
            OSUtils.RequireVista();
        }

        [Test]
        public void CanGetMixFormat()
        {
            // don't need to initialize before asking for MixFormat
            Debug.WriteLine(String.Format("Mix Format: {0}", GetAudioClient().MixFormat));
        }

        [Test]
        public void CanInitializeInSharedMode()
        {
            InitializeClient(AudioClientShareMode.Shared);
        }

        [Test]
        public void CanInitializeInExclusiveMode()
        {
            using (AudioClient audioClient = GetAudioClient())
            {
                WaveFormat waveFormat = new WaveFormat(44100, 16, 2); //audioClient.MixFormat;
                long refTimesPerSecond = 10000000;
                audioClient.Initialize(AudioClientShareMode.Exclusive,
                    AudioClientStreamFlags.None,
                    refTimesPerSecond / 10,
                    0,
                    waveFormat,
                    Guid.Empty);
            }
        }

        [Test]
        public void CanGetAudioRenderClient()
        {
            Assert.IsNotNull(InitializeClient(AudioClientShareMode.Shared).AudioRenderClient);
        }


        [Test]
        public void CanGetBufferSize()
        {
            Debug.WriteLine(String.Format("Buffer Size: {0}", InitializeClient(AudioClientShareMode.Shared).BufferSize));
        }

        [Test]
        public void CanGetCurrentPadding()
        {
            Debug.WriteLine(String.Format("CurrentPadding: {0}", InitializeClient(AudioClientShareMode.Shared).CurrentPadding));
        }

        [Test]
        public void CanGetDefaultDevicePeriod()
        {
            // should not need initialization
            Debug.WriteLine(String.Format("DefaultDevicePeriod: {0}", GetAudioClient().DefaultDevicePeriod));
        }

        [Test]
        public void CanGetMinimumDevicePeriod()
        {
            // should not need initialization
            Debug.WriteLine(String.Format("MinimumDevicePeriod: {0}", GetAudioClient().MinimumDevicePeriod));
        }

        [Test]
        public void DefaultFormatIsSupportedInSharedMode()
        {
            AudioClient client = GetAudioClient();
            WaveFormat defaultFormat = client.MixFormat;
            Assert.IsTrue(client.IsFormatSupported(AudioClientShareMode.Shared, defaultFormat), "Is Format Supported");
        }

        /* strange as this may seem, WASAPI doesn't seem to like the default format in exclusive mode
         * it prefers 16 bit (presumably 24 bit on some devices)
        [Test]
        public void DefaultFormatIsSupportedInExclusiveMode()
        {
            AudioClient client = GetAudioClient();
            WaveFormat defaultFormat = client.MixFormat;
            Assert.IsTrue(client.IsFormatSupported(AudioClientShareMode.Exclusive, defaultFormat), "Is Format Supported");
        }*/


        [Test]
        public void CanRequestIfFormatIsSupportedExtensible44100SharedMode()
        {
            WaveFormatExtensible desiredFormat = new WaveFormatExtensible(44100, 32, 2);
            Debug.WriteLine(desiredFormat);
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, desiredFormat);
        }

        [Test]
        public void CanRequestIfFormatIsSupportedExtensible44100ExclusiveMode()
        {
            WaveFormatExtensible desiredFormat = new WaveFormatExtensible(44100, 32, 2);
            Debug.WriteLine(desiredFormat);
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Exclusive, desiredFormat);
        }

        [Test]
        public void CanRequestIfFormatIsSupportedExtensible48000()
        {
            WaveFormatExtensible desiredFormat = new WaveFormatExtensible(48000, 32, 2);
            Debug.WriteLine(desiredFormat);
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, desiredFormat);
        }

        [Test]
        public void CanRequestIfFormatIsSupportedExtensible48000_16bit()
        {
            WaveFormatExtensible desiredFormat = new WaveFormatExtensible(48000, 16, 2);
            Debug.WriteLine(desiredFormat);
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, desiredFormat);
        }

        [Test]
        public void CanRequestIfFormatIsSupportedPCMStereo()
        {
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, new WaveFormat(44100, 16, 2));
        }

        [Test]
        public void CanRequestIfFormatIsSupported8KHzMono()
        {
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, new WaveFormat(8000, 16, 1));
        }

        [Test]
        public void CanRequest48kHz16BitStereo()
        {
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, new WaveFormat(48000, 16, 2));

        }

        [Test]
        public void CanRequest48kHz16BitMono()
        {
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, new WaveFormat(48000, 16, 1));
        }

        [Test]
        public void CanRequestIfFormatIsSupportedIeee()
        {
            GetAudioClient().IsFormatSupported(AudioClientShareMode.Shared, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        }

        [Test]
        public void CanPopulateABuffer()
        {
            AudioClient audioClient = InitializeClient(AudioClientShareMode.Shared);
            AudioRenderClient renderClient = audioClient.AudioRenderClient;
            int bufferFrameCount = audioClient.BufferSize;
            IntPtr buffer = renderClient.GetBuffer(bufferFrameCount);
            // TODO put some stuff in
            // will tell it it has a silent buffer
            renderClient.ReleaseBuffer(bufferFrameCount, AudioClientBufferFlags.Silent);
        }

        [Test, MaxTime(2000)]
        public void CanCaptureDefaultDeviceInDefaultFormatUsingWasapiCapture()
        {
            using (var wasapiClient = new WasapiCapture())
            {
                wasapiClient.StartRecording();
                Thread.Sleep(1000);
                wasapiClient.StopRecording();
            }
        }
 
        [Test, MaxTime(3000)]
        public void CanReuseWasapiCapture()
        {
            using (var wasapiClient = new WasapiCapture())
            {
                wasapiClient.StartRecording();
                Thread.Sleep(1000);
                wasapiClient.StopRecording();
                Thread.Sleep(1000);
                wasapiClient.StartRecording();
            }
        } 

        private AudioClient InitializeClient(AudioClientShareMode shareMode)
        {
            AudioClient audioClient = GetAudioClient();
            WaveFormat waveFormat = audioClient.MixFormat;
            long refTimesPerSecond = 10000000;
            audioClient.Initialize(shareMode,
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
