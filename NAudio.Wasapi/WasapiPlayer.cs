using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.Wave
{
    /// <summary>
    /// Modern WASAPI audio player with zero-copy buffer access, MMCSS thread priority,
    /// and IAudioClient3 low-latency support. Created via <see cref="WasapiPlayerBuilder"/>.
    /// </summary>
    public class WasapiPlayer : IWavePlayer, IWavePosition, IAsyncDisposable
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
        private IWaveProvider waveProvider;

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

        #region Volume

        /// <summary>
        /// Gets or sets the session volume (0.0 to 1.0). This controls your application's
        /// volume as shown in the Windows volume mixer, without affecting other applications.
        /// Delegates to <see cref="SessionVolume"/>.
        /// </summary>
        public float Volume
        {
            get => SessionVolume.Volume;
            set
            {
                if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), "Volume must be between 0.0 and 1.0");
                SessionVolume.Volume = value;
            }
        }

        /// <summary>
        /// Gets or sets the session mute state. This is the mute toggle for your application
        /// in the Windows volume mixer. Delegates to <see cref="SessionVolume"/>.
        /// </summary>
        public bool IsMuted
        {
            get => SessionVolume.Mute;
            set => SessionVolume.Mute = value;
        }

        /// <summary>
        /// Per-session volume and mute control. This is the volume slider shown for your
        /// application in the Windows volume mixer. Use this for simple volume/mute control
        /// that only affects your application.
        /// </summary>
        public SimpleAudioVolume SessionVolume =>
            mmDevice.AudioSessionManager.SimpleAudioVolume;

        /// <summary>
        /// Per-stream, per-channel volume control (0.0 to 1.0 per channel).
        /// Use this for balance, pan, or independent channel level adjustments.
        /// Only available in shared mode.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the player is using exclusive mode, where per-stream volume is not available.
        /// </exception>
        public AudioStreamVolume StreamVolume
        {
            get
            {
                if (shareMode == AudioClientShareMode.Exclusive)
                    throw new InvalidOperationException(
                        "StreamVolume is not available in exclusive mode. " +
                        "Use DeviceVolume for endpoint-level control, or adjust levels in your audio pipeline before playback.");
                return audioClient.AudioStreamVolume;
            }
        }

        /// <summary>
        /// Device endpoint volume — controls the master volume for the audio device,
        /// affecting all applications playing through it. Includes per-channel levels,
        /// mute, volume step control, dB range information, and change notifications.
        /// Use with care: changes are system-wide and visible to the user.
        /// Available in both shared and exclusive modes.
        /// </summary>
        public AudioEndpointVolume DeviceVolume => mmDevice.AudioEndpointVolume;

        #endregion

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
        /// Gets the current position in bytes from the wave output device.
        /// (This is not the same as the position within your reader stream.)
        /// </summary>
        public long GetPosition()
        {
            if (playbackState == PlaybackState.Stopped)
                return 0;

            ulong pos;
            if (playbackState == PlaybackState.Playing)
                pos = audioClient.AudioClockClient.AdjustedPosition;
            else
                audioClient.AudioClockClient.GetPosition(out pos, out _);

            return (long)pos * OutputWaveFormat.AverageBytesPerSecond
                 / (long)audioClient.AudioClockClient.Frequency;
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
        /// Finds a supported exclusive-mode format for this device, trying the preferred format first,
        /// then falling back through standard sample rates, bit depths, and multi-channel speaker
        /// configurations from the Windows Driver Kit (ksmedia.h).
        /// Use this to discover what format to provide to <see cref="Init"/> when using exclusive mode.
        /// </summary>
        /// <param name="preferredFormat">The format you'd ideally like to use.</param>
        /// <returns>A supported <see cref="WaveFormatExtensible"/>, or null if no supported format was found.</returns>
        public WaveFormatExtensible GetSupportedExclusiveFormat(WaveFormat preferredFormat)
        {
            var deviceSampleRate = audioClient.MixFormat.SampleRate;
            var deviceChannels = audioClient.MixFormat.Channels;

            var sampleRatesToTry = new List<int> { preferredFormat.SampleRate };
            if (!sampleRatesToTry.Contains(deviceSampleRate)) sampleRatesToTry.Add(deviceSampleRate);
            if (!sampleRatesToTry.Contains(44100)) sampleRatesToTry.Add(44100);
            if (!sampleRatesToTry.Contains(48000)) sampleRatesToTry.Add(48000);

            var channelCountsToTry = new List<int> { preferredFormat.Channels };
            if (!channelCountsToTry.Contains(deviceChannels)) channelCountsToTry.Add(deviceChannels);
            if (!channelCountsToTry.Contains(2)) channelCountsToTry.Add(2);

            var bitDepthsToTry = new List<int> { preferredFormat.BitsPerSample };
            if (!bitDepthsToTry.Contains(32)) bitDepthsToTry.Add(32);
            if (!bitDepthsToTry.Contains(24)) bitDepthsToTry.Add(24);
            if (!bitDepthsToTry.Contains(16)) bitDepthsToTry.Add(16);

            // Channel mask 0 uses the WaveFormatExtensible default (sequential bit-shift),
            // which covers stereo, 3.0, 3.1, and the obsolete 5.1/7.1 layouts.
            // Additional masks from ksmedia.h cover standard speaker configurations.
            var channelMasksToTry = new List<int> { 0 };
            if (channelCountsToTry.Contains(1)) channelMasksToTry.Add(0x0004); // 1.0: FC        (KSAUDIO_SPEAKER_MONO)
            if (channelCountsToTry.Contains(2)) channelMasksToTry.Add(0x000C); // 1.1: FC|LFE    (KSAUDIO_SPEAKER_1POINT1)
            if (channelCountsToTry.Contains(3)) channelMasksToTry.Add(0x000B); // 2.1: FL|FR|LFE (KSAUDIO_SPEAKER_2POINT1)
            if (channelCountsToTry.Contains(4))
            {
                channelMasksToTry.Add(0x0033); // 4.0: FL|FR|BL|BR (KSAUDIO_SPEAKER_QUAD)
                channelMasksToTry.Add(0x0107); // 4.0: FL|FR|FC|BC (KSAUDIO_SPEAKER_SURROUND)
            }
            if (channelCountsToTry.Contains(5)) channelMasksToTry.Add(0x0607); // 5.0: FL|FR|FC|SL|SR           (KSAUDIO_SPEAKER_5POINT0)
            if (channelCountsToTry.Contains(6)) channelMasksToTry.Add(0x060F); // 5.1: FL|FR|FC|LFE|SL|SR       (KSAUDIO_SPEAKER_5POINT1_SURROUND)
            if (channelCountsToTry.Contains(7)) channelMasksToTry.Add(0x0637); // 7.0: FL|FR|FC|BL|BR|SL|SR     (KSAUDIO_SPEAKER_7POINT0)
            if (channelCountsToTry.Contains(8)) channelMasksToTry.Add(0x063F); // 7.1: FL|FR|FC|LFE|BL|BR|SL|SR (KSAUDIO_SPEAKER_7POINT1_SURROUND)

            foreach (var sampleRate in sampleRatesToTry)
            {
                foreach (var channelCount in channelCountsToTry)
                {
                    foreach (var bitDepth in bitDepthsToTry)
                    {
                        foreach (var channelMask in channelMasksToTry)
                        {
                            var format = new WaveFormatExtensible(sampleRate, bitDepth, channelCount, channelMask);
                            if (audioClient.IsFormatSupported(AudioClientShareMode.Exclusive, format))
                                return format;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Initialize for playing the specified audio source (zero-copy path).
        /// In exclusive mode, the source format must be natively supported by the device —
        /// use <see cref="IsFormatSupported(WaveFormat)"/> or <see cref="GetSupportedExclusiveFormat"/>
        /// to check before calling Init.
        /// </summary>
        public void Init(IWaveProvider source)
        {
            waveProvider = source;
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
            int bytesRead = waveProvider.Read(lease.Buffer);
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
        /// Stops playback (blocking) and releases all resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops playback without blocking the calling thread, then releases all resources.
        /// Prefer this over <see cref="Dispose()"/> in async or UI contexts where blocking is undesirable.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped;
                stopEvent.Set();
                if (playThread != null)
                {
                    await Task.Run(() => playThread.Join());
                }
                playThread = null;
            }
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        private void DisposeCore()
        {
            audioClient?.Dispose();
            audioClient = null;
            renderClient = null;
            stopEvent.Dispose();
            frameEvent?.Dispose();
        }
    }
}
