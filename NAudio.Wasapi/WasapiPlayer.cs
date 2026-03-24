using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.Wave
{
    /// <summary>
    /// Modern WASAPI audio player with zero-copy buffer access, MMCSS thread priority,
    /// and IAudioClient3 low-latency support. Created via <see cref="WasapiPlayerBuilder"/>.
    /// </summary>
    public class WasapiPlayer : IWavePlayer, IAsyncDisposable
    {
        private readonly MMDevice mmDevice;
        private readonly AudioClientShareMode shareMode;
        private readonly bool isUsingEventSync;
        private readonly AudioStreamCategory? audioCategory;
        private readonly string mmcssTaskName;
        private readonly bool preferLowLatency;
        private readonly SynchronizationContext syncContext;
        private readonly EventWaitHandle stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        private AudioClient audioClient;
        private AudioRenderClient renderClient;
        private int latencyMilliseconds;
        private int bufferFrameCount;
        private int bytesPerFrame;
        private EventWaitHandle frameEvent;
        private Thread playThread;
        private volatile PlaybackState playbackState;
        private IAudioSource audioSource;

        /// <summary>
        /// Raised when playback stops, either because the source ended or an error occurred.
        /// If a <see cref="SynchronizationContext"/> was captured at construction, this event
        /// is raised on that context (e.g. the UI thread).
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// Current playback state.
        /// </summary>
        public PlaybackState PlaybackState => playbackState;

        /// <summary>
        /// The output format being sent to the audio device.
        /// </summary>
        public WaveFormat OutputWaveFormat { get; private set; }

        /// <summary>
        /// Gets or sets the playback volume (0.0 to 1.0) via the device endpoint volume.
        /// </summary>
        public float Volume
        {
            get => mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
            set
            {
                if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), "Volume must be between 0.0 and 1.0");
                mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value;
            }
        }

        internal WasapiPlayer(MMDevice device, AudioClientShareMode shareMode, bool useEventSync,
            int latencyMilliseconds, AudioStreamCategory? audioCategory, string mmcssTaskName, bool preferLowLatency)
        {
            mmDevice = device;
            this.shareMode = shareMode;
            isUsingEventSync = useEventSync;
            this.latencyMilliseconds = latencyMilliseconds;
            this.audioCategory = audioCategory;
            this.mmcssTaskName = mmcssTaskName;
            this.preferLowLatency = preferLowLatency;
            syncContext = SynchronizationContext.Current;

            audioClient = device.CreateAudioClient();
            OutputWaveFormat = audioClient.MixFormat;
        }

        /// <summary>
        /// The device's preferred mix format (shared mode). This is always supported in shared mode.
        /// </summary>
        public WaveFormat DeviceMixFormat => audioClient.MixFormat;

        /// <summary>
        /// Checks whether the specified format is supported by the device in the current share mode.
        /// </summary>
        /// <param name="format">The format to check.</param>
        /// <param name="closestMatch">In shared mode, the closest supported format if the exact format isn't supported. Always null in exclusive mode.</param>
        /// <returns>True if the format is supported.</returns>
        public bool IsFormatSupported(WaveFormat format, out WaveFormatExtensible closestMatch)
        {
            return audioClient.IsFormatSupported(shareMode, format, out closestMatch);
        }

        /// <summary>
        /// Checks whether the specified format is supported by the device in the current share mode.
        /// </summary>
        public bool IsFormatSupported(WaveFormat format)
        {
            return audioClient.IsFormatSupported(shareMode, format);
        }

        /// <summary>
        /// Initialize for playing the specified audio source (zero-copy path).
        /// In exclusive mode, the source format must be natively supported by the device —
        /// use <see cref="IsFormatSupported(WaveFormat)"/> to check before calling Init.
        /// </summary>
        public void Init(IAudioSource source)
        {
            audioSource = source;
            InitializeAudioClient(source.WaveFormat);
        }


        private void InitializeAudioClient(WaveFormat sourceFormat)
        {
            long latencyRefTimes = latencyMilliseconds * 10000L;
            OutputWaveFormat = sourceFormat;

            // Set audio category via IAudioClient2 if requested
            if (audioCategory.HasValue && audioClient.SupportsAudioClient2)
            {
                var props = new AudioClientProperties
                {
                    cbSize = (uint)Marshal.SizeOf<AudioClientProperties>(),
                    bIsOffload = 0,
                    eCategory = audioCategory.Value,
                    Options = AudioClientStreamOptions.None
                };
                var propsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<AudioClientProperties>());
                try
                {
                    Marshal.StructureToPtr(props, propsPtr, false);
                    // Call SetClientProperties on the underlying IAudioClient2
                    // For now, use the v1 path since SetClientProperties is not yet exposed on the wrapper
                }
                finally
                {
                    Marshal.FreeHGlobal(propsPtr);
                }
            }

            var flags = shareMode == AudioClientShareMode.Shared
                ? AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality
                : AudioClientStreamFlags.None;

            // Try IAudioClient3 low-latency path for shared mode
            if (preferLowLatency && shareMode == AudioClientShareMode.Shared && audioClient.SupportsAudioClient3)
            {
                if (TryInitializeLowLatency(flags))
                    return;
            }

            // Standard initialization
            if (isUsingEventSync)
            {
                InitializeWithEventSync(flags, latencyRefTimes);
            }
            else
            {
                audioClient.Initialize(shareMode, flags, latencyRefTimes, 0, OutputWaveFormat, Guid.Empty);
            }

            renderClient = audioClient.AudioRenderClient;
        }

        private bool TryInitializeLowLatency(AudioClientStreamFlags flags)
        {
            try
            {
                var periodInfo = audioClient.GetSharedModeEnginePeriod(OutputWaveFormat);
                var periodInFrames = Math.Max(periodInfo.MinPeriodInFrames, periodInfo.FundamentalPeriodInFrames);
                audioClient.InitializeSharedAudioStream(
                    flags | AudioClientStreamFlags.EventCallback,
                    periodInFrames, OutputWaveFormat, Guid.Empty);

                latencyMilliseconds = (int)(periodInFrames * 1000 / OutputWaveFormat.SampleRate);
                if (latencyMilliseconds < 1) latencyMilliseconds = 1;

                frameEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
                audioClient.SetEventHandle(frameEvent.SafeWaitHandle.DangerousGetHandle());
                renderClient = audioClient.AudioRenderClient;
                return true;
            }
            catch
            {
                // Fall back to standard initialization
                audioClient.Dispose();
                audioClient = mmDevice.CreateAudioClient();
                return false;
            }
        }

        private void InitializeWithEventSync(AudioClientStreamFlags flags, long latencyRefTimes)
        {
            if (shareMode == AudioClientShareMode.Shared)
            {
                audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback | flags,
                    latencyRefTimes, 0, OutputWaveFormat, Guid.Empty);

                var streamLatency = audioClient.StreamLatency;
                if (streamLatency != 0)
                    latencyMilliseconds = (int)(streamLatency / 10000);
            }
            else
            {
                try
                {
                    audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback | flags,
                        latencyRefTimes, latencyRefTimes, OutputWaveFormat, Guid.Empty);
                }
                catch (COMException ex) when (ex.ErrorCode == AudioClientErrorCode.BufferSizeNotAligned)
                {
                    long newLatencyRefTimes = (long)(10000000.0 / OutputWaveFormat.SampleRate * audioClient.BufferSize + 0.5);
                    audioClient.Dispose();
                    audioClient = mmDevice.CreateAudioClient();
                    audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback | flags,
                        newLatencyRefTimes, newLatencyRefTimes, OutputWaveFormat, Guid.Empty);
                }
            }

            frameEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            audioClient.SetEventHandle(frameEvent.SafeWaitHandle.DangerousGetHandle());
            renderClient = audioClient.AudioRenderClient;
        }

        /// <summary>
        /// Begin playback.
        /// </summary>
        public void Play()
        {
            if (playbackState != PlaybackState.Playing)
            {
                if (playbackState == PlaybackState.Stopped)
                {
                    stopEvent.Reset();
                    playThread = new Thread(PlayThread) { IsBackground = true };
                    playbackState = PlaybackState.Playing;
                    playThread.Start();
                }
                else
                {
                    playbackState = PlaybackState.Playing;
                }
            }
        }

        /// <summary>
        /// Stop playback and flush buffers.
        /// </summary>
        public void Stop()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped;
                stopEvent.Set();
                playThread?.Join();
                playThread = null;
            }
        }

        /// <summary>
        /// Pause playback without flushing buffers.
        /// </summary>
        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
                playbackState = PlaybackState.Paused;
        }

        private void PlayThread()
        {
            IntPtr mmcssHandle = IntPtr.Zero;
            Exception exception = null;
            try
            {
                // Elevate thread priority via MMCSS
                if (mmcssTaskName != null)
                {
                    uint taskIndex = 0;
                    mmcssHandle = NativeMethods.AvSetMmThreadCharacteristics(mmcssTaskName, ref taskIndex);
                }

                bufferFrameCount = audioClient.BufferSize;
                bytesPerFrame = OutputWaveFormat.BlockAlign;

                // Fill the initial buffer
                if (FillBuffer(bufferFrameCount))
                    return;

                var waitHandles = (isUsingEventSync || frameEvent != null)
                    ? new WaitHandle[] { frameEvent, stopEvent }
                    : null;

                audioClient.Start();

                var reachedEndOfStream = false;
                while (playbackState != PlaybackState.Stopped)
                {
                    if (waitHandles != null)
                    {
                        WaitHandle.WaitAny(waitHandles, 3 * latencyMilliseconds, false);
                    }
                    else
                    {
                        stopEvent.WaitOne(latencyMilliseconds / 2, false);
                    }

                    if (playbackState == PlaybackState.Playing)
                    {
                        if (reachedEndOfStream)
                        {
                            if (shareMode == AudioClientShareMode.Exclusive || audioClient.CurrentPadding == 0)
                                break;
                            continue;
                        }

                        int numFramesPadding = (isUsingEventSync && shareMode == AudioClientShareMode.Exclusive)
                            ? 0
                            : audioClient.CurrentPadding;
                        int numFramesAvailable = bufferFrameCount - numFramesPadding;
                        if (numFramesAvailable > 10)
                        {
                            if (FillBuffer(numFramesAvailable))
                                reachedEndOfStream = true;
                        }
                    }
                }

                audioClient.Stop();
                playbackState = PlaybackState.Stopped;
                audioClient.Reset();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                if (mmcssHandle != IntPtr.Zero)
                    NativeMethods.AvRevertMmThreadCharacteristics(mmcssHandle);
                RaisePlaybackStopped(exception);
            }
        }

        /// <summary>
        /// Fills the WASAPI render buffer directly from the audio source using Span (zero-copy).
        /// Returns true if the source has ended.
        /// </summary>
        private bool FillBuffer(int frameCount)
        {
            using var lease = renderClient.GetBufferLease(frameCount, bytesPerFrame);
            int bytesRead = audioSource.Read(lease.Buffer);
            if (bytesRead == 0)
            {
                lease.Release(0, AudioClientBufferFlags.Silent);
                return true;
            }

            int framesRead = bytesRead / bytesPerFrame;
            if (isUsingEventSync && shareMode == AudioClientShareMode.Exclusive)
            {
                // In exclusive event mode, must release the full frame count
                if (bytesRead < frameCount * bytesPerFrame)
                    lease.Buffer.Slice(bytesRead).Clear();
                lease.Release(frameCount);
            }
            else
            {
                lease.Release(framesRead);
            }
            return false;
        }

        private void RaisePlaybackStopped(Exception e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                if (syncContext != null)
                    syncContext.Post(_ => handler(this, new StoppedEventArgs(e)), null);
                else
                    handler(this, new StoppedEventArgs(e));
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Stop();
            audioClient?.Dispose();
            audioClient = null;
            renderClient = null;
            stopEvent.Dispose();
            frameEvent?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Async dispose — stops playback and releases resources.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped;
                stopEvent.Set();
                if (playThread != null)
                    await Task.Run(() => playThread.Join());
                playThread = null;
            }
            audioClient?.Dispose();
            audioClient = null;
            renderClient = null;
            stopEvent.Dispose();
            frameEvent?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
