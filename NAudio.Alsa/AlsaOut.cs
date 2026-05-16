using System;
using System.Threading;
using NAudio.Wave.SampleProviders;

namespace NAudio.Wave.Alsa
{
    /// <summary>
    /// An <see cref="IWavePlayer"/> that plays audio through ALSA on Linux.
    /// </summary>
    /// <remarks>
    /// Audio is taken through a <see cref="SampleChannel"/> (giving software
    /// volume and mono→stereo handling) and converted to the first device-
    /// supported format among IEEE float, 16-bit and 24-bit PCM. A dedicated
    /// background thread performs blocking <c>snd_pcm_writei</c> calls and
    /// recovers from xruns; it is always joined before the device handle is
    /// closed.
    /// </remarks>
    public sealed class AlsaOut : IWavePlayer
    {
        private static readonly PCMFormat[] PreferredFormats =
        {
            PCMFormat.SND_PCM_FORMAT_FLOAT_LE,
            PCMFormat.SND_PCM_FORMAT_S16_LE,
            PCMFormat.SND_PCM_FORMAT_S24_3LE,
        };

        private readonly object sync = new();
        private readonly ManualResetEventSlim resumeGate = new(initialState: true);
        private readonly AlsaPcm pcm;
        private SampleChannel channel;
        private IWaveProvider source;
        private int frameBytes;
        private float volume = 1.0f;
        private volatile PlaybackState playbackState = PlaybackState.Stopped;
        private bool pausedViaHardware;

        /// <summary>Creates an <see cref="AlsaOut"/> on the default PCM device.</summary>
        public AlsaOut()
            : this("default")
        {
        }

        /// <summary>
        /// Creates an <see cref="AlsaOut"/> on a specific PCM device.
        /// </summary>
        /// <param name="device">An ALSA PCM name, e.g. <c>"default"</c> or <c>"hw:0"</c>.</param>
        public AlsaOut(string device)
        {
            pcm = new AlsaPcm(device, PCMStream.SND_PCM_STREAM_PLAYBACK);
        }

        /// <inheritdoc />
        public PlaybackState PlaybackState => playbackState;

        /// <inheritdoc />
        public WaveFormat OutputWaveFormat { get; private set; }

        /// <inheritdoc />
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <inheritdoc />
        public float Volume
        {
            get => channel?.Volume ?? volume;
            set
            {
                volume = value;
                if (channel != null)
                {
                    channel.Volume = value;
                }
            }
        }

        /// <inheritdoc />
        public void Init(IWaveProvider waveProvider)
        {
            ArgumentNullException.ThrowIfNull(waveProvider);
            pcm.ThrowIfDisposed();
            lock (sync)
            {
                if (source != null)
                {
                    throw new InvalidOperationException("Already initialised");
                }

                channel = new SampleChannel(waveProvider, forceStereo: false) { Volume = volume };
                int channels = channel.WaveFormat.Channels;
                int sampleRate = channel.WaveFormat.SampleRate;

                var format = Negotiate();
                switch (format)
                {
                    case PCMFormat.SND_PCM_FORMAT_FLOAT_LE:
                        source = new SampleToWaveProvider(channel);
                        OutputWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
                        break;
                    case PCMFormat.SND_PCM_FORMAT_S24_3LE:
                        source = new SampleToWaveProvider24(channel);
                        OutputWaveFormat = new WaveFormat(sampleRate, 24, channels);
                        break;
                    default:
                        source = new SampleToWaveProvider16(channel);
                        OutputWaveFormat = new WaveFormat(sampleRate, 16, channels);
                        break;
                }

                frameBytes = AlsaFormat.FrameBytes(OutputWaveFormat);
                pcm.ConfigureHardware(format, channels, sampleRate);
                pcm.ConfigureSoftware((AlsaPcm.Periods - 1) * AlsaPcm.PeriodFrames);
                pcm.Prepare();
            }
        }

        /// <inheritdoc />
        public void Play()
        {
            pcm.ThrowIfDisposed();
            lock (sync)
            {
                if (source == null)
                {
                    throw new InvalidOperationException("Call Init before Play");
                }

                switch (playbackState)
                {
                    case PlaybackState.Playing:
                        return;
                    case PlaybackState.Paused:
                        if (pausedViaHardware)
                        {
                            AlsaInterop.PcmPause(pcm.Pcm, 0);
                        }
                        else
                        {
                            pcm.Prepare();
                        }

                        playbackState = PlaybackState.Playing;
                        resumeGate.Set();
                        return;
                    default:
                        playbackState = PlaybackState.Playing;
                        resumeGate.Set();
                        pcm.StartWorker("ALSA Playback", PlaybackLoop);
                        return;
                }
            }
        }

        /// <inheritdoc />
        public void Pause()
        {
            lock (sync)
            {
                if (playbackState != PlaybackState.Playing)
                {
                    return;
                }

                playbackState = PlaybackState.Paused;
                resumeGate.Reset();
                int paused = AlsaInterop.PcmPause(pcm.Pcm, 1);
                pausedViaHardware = paused >= 0;
                if (!pausedViaHardware)
                {
                    AlsaInterop.PcmDrop(pcm.Pcm);
                }
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            lock (sync)
            {
                if (playbackState == PlaybackState.Stopped)
                {
                    return;
                }

                playbackState = PlaybackState.Stopped;
                resumeGate.Set();
            }

            pcm.StopWorker();
        }

        private PCMFormat Negotiate()
        {
            foreach (var candidate in PreferredFormats)
            {
                if (pcm.SupportsFormat(candidate))
                {
                    return candidate;
                }
            }

            throw new NotSupportedException(
                "ALSA device supports none of IEEE float, 16-bit or 24-bit PCM");
        }

        private void PlaybackLoop()
        {
            var buffer = new byte[AlsaPcm.PeriodFrames * frameBytes];
            Exception error = null;
            try
            {
                while (pcm.Running)
                {
                    if (playbackState == PlaybackState.Paused)
                    {
                        resumeGate.Wait();
                        continue;
                    }

                    int read = source.Read(buffer);
                    if (read == 0)
                    {
                        break;
                    }

                    int offset = 0;
                    int frames = read / frameBytes;
                    while (frames > 0 && pcm.Running)
                    {
                        nint written = AlsaInterop.PcmWriteI(
                            pcm.Pcm, buffer.AsSpan(offset, frames * frameBytes), (nuint)frames);
                        if (written < 0)
                        {
                            if (!pcm.Running)
                            {
                                break;
                            }

                            int recovered = AlsaInterop.PcmRecover(pcm.Pcm, (int)written, 1);
                            if (recovered < 0)
                            {
                                throw new AlsaException("snd_pcm_writei", (int)written);
                            }

                            continue;
                        }

                        offset += (int)written * frameBytes;
                        frames -= (int)written;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            playbackState = PlaybackState.Stopped;
            PlaybackStopped?.Invoke(this, new StoppedEventArgs(error));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (sync)
            {
                playbackState = PlaybackState.Stopped;
                resumeGate.Set();
            }

            pcm.Dispose();
            resumeGate.Dispose();
        }
    }
}
