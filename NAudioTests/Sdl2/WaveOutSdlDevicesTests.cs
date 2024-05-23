using System;
using System.Threading;
using NAudio.Sdl2;
using NAudio.Sdl2.Structures;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Sdl2
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class WaveOutSdlDevicesTests
    {
        [Test]
        public void CanRequestNumberOfWaveOutSdlDevices()
        {
            int deviceCount = WaveOutSdl.DeviceCount;
            Assert.That(deviceCount > 0, "Expected at least one WaveOutSdl device");
        }
        
        [Test]
        public void CanGetWaveOutSdlDeviceCapabilities()
        {
            for (int n = 0; n < WaveOutSdl.DeviceCount; n++)
            {
                WaveOutSdlCapabilities capabilities = WaveOutSdl.GetCapabilities(n);
                Assert.That(!String.IsNullOrEmpty(capabilities.DeviceName), "Needs a device name");
            }
        }

        [Test]
        public void CanGetWaveOutSdlDeviceCapabilitiesList()
        {
            var capabilitiesList = WaveOutSdl.GetCapabilitiesList();
            Assert.That(capabilitiesList.Count > 0, "Expected at least one WaveOutSdlCapabilities");
        }

        [Test]
        public void CanGetWaveOutSdlDeviceDefaultCapabilities()
        {
            Assert.DoesNotThrow(() => WaveOutSdl.GetDefaultDeviceCapabilities());
        }

        [Test]
        public void CanWaveOutSdlPlayAfterStop()
        {
            WaveOutSdl waveOutSdl = new WaveOutSdl();
            AutoResetEvent disposeWait = new AutoResetEvent(false);
            EventHandler<StoppedEventArgs> playbackStopped = (s, e) => disposeWait.Set();
            waveOutSdl.PlaybackStopped += playbackStopped;
            waveOutSdl.Init(new SilenceProvider(new WaveFormat(44100, 16, 1)));
            waveOutSdl.Play();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            waveOutSdl.Stop();
            disposeWait.WaitOne(1500);
            waveOutSdl.Play();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            waveOutSdl.Stop();
            disposeWait.WaitOne(1500);
            waveOutSdl.PlaybackStopped -= playbackStopped;
            waveOutSdl.Dispose();
        }

        [Test]
        public void CanWaveOutSdlPlayAfterPause()
        {
            WaveOutSdl waveOutSdl = new WaveOutSdl();
            AutoResetEvent disposeWait = new AutoResetEvent(false);
            EventHandler<StoppedEventArgs> playbackStopped = (s, e) => disposeWait.Set();
            waveOutSdl.PlaybackStopped += playbackStopped;
            waveOutSdl.Init(new SilenceProvider(new WaveFormat(44100, 16, 1)));
            waveOutSdl.Play();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            waveOutSdl.Pause();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            waveOutSdl.Play();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            waveOutSdl.Stop();
            disposeWait.WaitOne(1500);
            waveOutSdl.PlaybackStopped -= playbackStopped;
            waveOutSdl.Dispose();
        }

        [Test]
        public void WaveOutSdlAdjustLatencyPercentOutOfRangeShouldThrow()
        {
            WaveOutSdl waveOutSdl = new WaveOutSdl();
            Assert.Throws<SdlException>(() => waveOutSdl.AdjustLatencyPercent = -1);
            Assert.Throws<SdlException>(() => waveOutSdl.AdjustLatencyPercent = 2);
            waveOutSdl.Dispose();
        }

        [Test]
        public void WaveOutVolumeOutOfRangeShouldThrow()
        {
            WaveOutSdl waveOutSdl = new WaveOutSdl();
            Assert.Throws<SdlException>(() => waveOutSdl.Volume = -1.0f);
            Assert.Throws<SdlException>(() => waveOutSdl.Volume = 1.29f);
            waveOutSdl.Dispose();
        }
    }
}
