using System;

namespace NAudio.Wave.Alsa
{
    /// <summary>
    /// An <see cref="IWaveIn"/> that records audio through ALSA on Linux.
    /// </summary>
    /// <remarks>
    /// A dedicated background thread performs blocking <c>snd_pcm_readi</c>
    /// calls, recovers from overruns, and raises <see cref="DataAvailable"/>
    /// for each captured period. The thread is always joined before the
    /// device handle is closed.
    /// </remarks>
    public sealed class AlsaIn : IWaveIn, IWaveLatency
    {
        private readonly AlsaPcm pcm;
        private WaveFormat waveFormat = new(44100, 16, 2);
        private int frameBytes;

        /// <summary>Creates an <see cref="AlsaIn"/> on the default PCM device.</summary>
        public AlsaIn()
            : this("default")
        {
        }

        /// <summary>
        /// Creates an <see cref="AlsaIn"/> on a specific PCM device.
        /// </summary>
        /// <param name="device">An ALSA PCM name, e.g. <c>"default"</c> or <c>"hw:0"</c>.</param>
        public AlsaIn(string device)
        {
            pcm = new AlsaPcm(device, PCMStream.SND_PCM_STREAM_CAPTURE);
        }

        /// <inheritdoc />
        public WaveFormat WaveFormat
        {
            get => waveFormat;
            set
            {
                if (pcm.Running)
                {
                    throw new InvalidOperationException("Cannot change WaveFormat while recording");
                }

                waveFormat = value;
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The ring buffer is configured at <see cref="StartRecording"/> time as
        /// <c>PeriodFrames × Periods</c>, so the steady-state delay between a sample hitting the
        /// hardware and being delivered to <see cref="DataAvailable"/> subscribers is the full
        /// buffer's worth of frames.
        /// </remarks>
        public TimeSpan AverageLatency =>
            TimeSpan.FromSeconds((long)AlsaPcm.PeriodFrames * AlsaPcm.Periods / (double)waveFormat.SampleRate);

        /// <inheritdoc/>
        /// <remarks>
        /// Uses <c>snd_pcm_delay</c>, which on a capture stream returns the number of frames
        /// already captured and waiting to be read — exactly the age of the oldest unread sample.
        /// Falls back to <see cref="AverageLatency"/> when not running or the call fails.
        /// </remarks>
        public TimeSpan CurrentLatency
        {
            get
            {
                if (!pcm.Running) return AverageLatency;
                if (AlsaInterop.PcmDelay(pcm.Pcm, out nint delayFrames) < 0 || delayFrames < 0)
                    return AverageLatency;
                return TimeSpan.FromSeconds(delayFrames / (double)waveFormat.SampleRate);
            }
        }

        /// <inheritdoc />
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <inheritdoc />
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <inheritdoc />
        public void StartRecording()
        {
            pcm.ThrowIfDisposed();
            if (pcm.Running)
            {
                throw new InvalidOperationException("Already recording");
            }

            var format = AlsaFormat.FromWaveFormat(waveFormat);
            frameBytes = AlsaFormat.FrameBytes(waveFormat);
            pcm.ConfigureHardware(format, waveFormat.Channels, waveFormat.SampleRate);
            pcm.ConfigureSoftware(AlsaPcm.PeriodFrames);
            pcm.Prepare();
            AlsaException.ThrowIfError(AlsaInterop.PcmStart(pcm.Pcm), "snd_pcm_start");
            pcm.StartWorker("ALSA Capture", CaptureLoop);
        }

        /// <inheritdoc />
        public void StopRecording() => pcm.StopWorker();

        private void CaptureLoop()
        {
            var buffer = new byte[AlsaPcm.PeriodFrames * frameBytes];
            Exception error = null;
            try
            {
                while (pcm.Running)
                {
                    nint read = AlsaInterop.PcmReadI(pcm.Pcm, buffer, AlsaPcm.PeriodFrames);
                    if (read < 0)
                    {
                        if (!pcm.Running)
                        {
                            break;
                        }

                        int recovered = AlsaInterop.PcmRecover(pcm.Pcm, (int)read, 1);
                        if (recovered < 0)
                        {
                            throw new AlsaException((int)read, "snd_pcm_readi");
                        }

                        AlsaException.ThrowIfError(AlsaInterop.PcmStart(pcm.Pcm), "snd_pcm_start");
                        continue;
                    }

                    DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, (int)read * frameBytes));
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            RecordingStopped?.Invoke(this, new StoppedEventArgs(error));
        }

        /// <inheritdoc />
        public void Dispose() => pcm.Dispose();
    }
}
