using System;
using System.Linq;
using NAudio.Wave;

namespace NAudioDemo.AudioPlaybackDemo
{
    /// <summary>
    /// Minimal <see cref="IWavePlayer"/> adapter over <see cref="AsioDevice"/> for the AudioPlaybackDemo plugin contract.
    /// AsioDevice deliberately does not implement IWavePlayer; this adapter exists solely to plug the new API into
    /// the demo's existing playback pipeline. Not intended for production use — callers should use AsioDevice directly.
    /// </summary>
    internal sealed class AsioDeviceAdapter : IWavePlayer
    {
        private readonly AsioDevice device;
        private readonly int outputChannelOffset;
        private PlaybackState state = PlaybackState.Stopped;

        public AsioDeviceAdapter(string driverName, int outputChannelOffset)
        {
            device = AsioDevice.Open(driverName);
            this.outputChannelOffset = outputChannelOffset;
            device.Stopped += (s, e) =>
            {
                state = PlaybackState.Stopped;
                PlaybackStopped?.Invoke(this, e);
            };
        }

        public WaveFormat OutputWaveFormat { get; private set; }

        public PlaybackState PlaybackState => state;

        public float Volume
        {
            get => 1.0f;
            set { /* ASIO bypasses Windows mixer; attenuate upstream in the source if you need volume control. */ }
        }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public void Init(IWaveProvider waveProvider)
        {
            int sourceChannels = waveProvider.WaveFormat.Channels;
            int maxOutputs = device.Capabilities.NbOutputChannels;
            if (outputChannelOffset < 0 || outputChannelOffset + sourceChannels > maxOutputs)
            {
                throw new ArgumentException(
                    $"Starting output channel {outputChannelOffset} + source channels {sourceChannels} exceeds the driver's {maxOutputs} available outputs.");
            }
            var outputChannels = Enumerable.Range(outputChannelOffset, sourceChannels).ToArray();

            device.InitPlayback(new AsioPlaybackOptions
            {
                Source = waveProvider,
                OutputChannels = outputChannels,
                AutoStopOnEndOfStream = true
            });
            OutputWaveFormat = waveProvider.WaveFormat;
        }

        public void Play()
        {
            if (state == PlaybackState.Playing) return;
            device.Start();
            state = PlaybackState.Playing;
        }

        public void Stop()
        {
            if (state != PlaybackState.Playing && state != PlaybackState.Paused) return;
            device.Stop();
            state = PlaybackState.Stopped;
        }

        public void Pause()
        {
            // AsioDevice does not distinguish Paused from Stopped — Stop then Play resumes from the source's current position.
            if (state != PlaybackState.Playing) return;
            device.Stop();
            state = PlaybackState.Paused;
        }

        public void Dispose()
        {
            device.Dispose();
        }
    }
}
