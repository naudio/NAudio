using System;
using NAudio.Effects;
using NAudio.Wave;

namespace NAudioWpfDemo.RealtimeEffectsDemo
{
    /// <summary>
    /// Drives full-duplex ASIO audio through an effect chain. The chain is held as an
    /// atomically-swappable array so the UI thread can rebuild it while the real-time
    /// callback keeps running. Includes feedback protection (starts muted; auto-mutes on
    /// sustained near-full-scale output).
    /// </summary>
    class RealtimeAudioEngine : IDisposable
    {
        private AsioDevice device;
        private float[] scratch = Array.Empty<float>();
        private volatile IAudioEffect[] effects = Array.Empty<IAudioEffect>();
        private volatile bool muted = true;
        private volatile bool autoMuted;
        private int inputChannels = 2;
        private int runawaySamples;
        private float outputLevel;

        public bool IsRunning => device != null && device.State == AsioDeviceState.Running;

        public int SampleRate { get; private set; }

        /// <summary>When true the output is silenced (feedback-safe default).</summary>
        public bool Muted
        {
            get => muted;
            set => muted = value;
        }

        /// <summary>Most recent output peak (0..1), for the level meter.</summary>
        public float OutputLevel => outputLevel;

        /// <summary>
        /// Replaces the processing chain. The effects must already be configured for the
        /// engine's sample rate and stereo. Thread-safe against the audio callback.
        /// </summary>
        public void SetEffects(IAudioEffect[] chain) => effects = chain ?? Array.Empty<IAudioEffect>();

        /// <summary>Returns true once if a feedback auto-mute fired since the last call.</summary>
        public bool ConsumeAutoMuted()
        {
            if (!autoMuted)
                return false;
            autoMuted = false;
            return true;
        }

        public void Start(string driverName, int inputChannelCount)
        {
            Stop();
            inputChannels = inputChannelCount <= 1 ? 1 : 2;
            device = AsioDevice.Open(driverName);
            var capabilities = device.Capabilities;
            var inputs = inputChannels == 1 ? new[] { 0 } : new[] { 0, 1 };
            var outputs = capabilities.NbOutputChannels >= 2 ? new[] { 0, 1 } : new[] { 0 };

            device.InitDuplex(new AsioDuplexOptions
            {
                InputChannels = inputs,
                OutputChannels = outputs,
                Processor = OnBuffer
            });

            SampleRate = device.CurrentSampleRate;
            scratch = new float[device.FramesPerBuffer * 2];
            muted = true;
            autoMuted = false;
            runawaySamples = 0;
            outputLevel = 0f;
            device.Start();
        }

        public void Stop()
        {
            if (device != null)
            {
                try { device.Stop(); }
                catch { /* driver may already be stopped */ }
                device.Dispose();
                device = null;
            }
            outputLevel = 0f;
        }

        private void OnBuffer(in AsioProcessBuffers buffers)
        {
            var frames = buffers.Frames;
            var samples = frames * 2;
            var buffer = scratch;

            var inputLeft = buffers.GetInput(0);
            var inputRight = inputChannels == 2 && buffers.InputChannelCount > 1
                ? buffers.GetInput(1)
                : inputLeft;

            for (var i = 0; i < frames; i++)
            {
                buffer[i * 2] = inputLeft[i];
                buffer[i * 2 + 1] = inputRight[i];
            }

            var chain = effects;
            for (var e = 0; e < chain.Length; e++)
                chain[e].Process(buffer.AsSpan(0, samples));

            var peak = 0f;
            if (muted)
            {
                Array.Clear(buffer, 0, samples);
            }
            else
            {
                for (var i = 0; i < samples; i++)
                {
                    var a = buffer[i] < 0f ? -buffer[i] : buffer[i];
                    if (a > peak)
                        peak = a;
                }
            }
            outputLevel = peak;

            var outputLeftSpan = buffers.GetOutput(0);
            for (var i = 0; i < frames; i++)
                outputLeftSpan[i] = buffer[i * 2];
            if (buffers.OutputChannelCount > 1)
            {
                var outputRightSpan = buffers.GetOutput(1);
                for (var i = 0; i < frames; i++)
                    outputRightSpan[i] = buffer[i * 2 + 1];
            }

            // Feedback protection: ~1 s of near-full-scale output → auto-mute.
            if (!muted && peak > 0.98f)
            {
                runawaySamples += frames;
                if (runawaySamples > SampleRate)
                {
                    muted = true;
                    autoMuted = true;
                    runawaySamples = 0;
                }
            }
            else
            {
                runawaySamples = 0;
            }
        }

        public void Dispose() => Stop();
    }
}
