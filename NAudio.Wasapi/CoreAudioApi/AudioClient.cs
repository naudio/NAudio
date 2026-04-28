using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Windows CoreAudio AudioClient. Wraps IAudioClient, IAudioClient2, and IAudioClient3.
    /// </summary>
    public class AudioClient : IDisposable
    {
        private static readonly Guid ID_AudioStreamVolume = new Guid("93014887-242D-4068-8A15-CF5E93B90FE3");
        private static readonly Guid ID_AudioClockClient = new Guid("CD63314F-3FBA-4a1b-812C-EF96358728E7");
        private static readonly Guid ID_AudioRenderClient = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
        private static readonly Guid ID_AudioCaptureClient = new Guid("c8adbd64-e71e-48a0-a4de-185c395cd317");
        private static readonly Guid IID_IAudioClient2 = new Guid("726778CD-F60A-4eda-82DE-E47610CD78AA");
        private IAudioClient audioClientInterface;
        private readonly IAudioClient2 audioClientInterface2;
        private readonly IAudioClient3 audioClientInterface3;
        private WaveFormat mixFormat;
        private AudioRenderClient audioRenderClient;
        private AudioCaptureClient audioCaptureClient;
        private AudioClockClient audioClockClient;
        private AudioStreamVolume audioStreamVolume;
        private AudioClientShareMode shareMode;

        /// <summary>
        /// Activate Async
        /// </summary>
        public static async Task<AudioClient> ActivateAsync(string deviceInterfacePath, AudioClientProperties? audioClientProperties)
        {
            var icbh = new ActivateAudioInterfaceCompletionHandler(
                ac2 =>
                {
                    if (audioClientProperties != null)
                    {
                        IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(audioClientProperties.Value));
                        try
                        {
                            Marshal.StructureToPtr(audioClientProperties.Value, p, false);
                            ac2.SetClientProperties(p);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(p);
                        }
                    }
                });
            NativeMethods.ActivateAudioInterfaceAsync(deviceInterfacePath, IID_IAudioClient2, IntPtr.Zero, icbh, out _);
            var audioClient2 = await icbh;
            return new AudioClient((IAudioClient)audioClient2);
        }

        /// <summary>
        /// Creates a new AudioClient
        /// </summary>
        internal AudioClient(IAudioClient audioClientInterface)
        {
            this.audioClientInterface = audioClientInterface;
            audioClientInterface2 = audioClientInterface as IAudioClient2;
            audioClientInterface3 = audioClientInterface as IAudioClient3;
        }

        /// <summary>
        /// Whether this audio client supports IAudioClient2 features (Windows 8+)
        /// </summary>
        public bool SupportsAudioClient2 => audioClientInterface2 != null;

        /// <summary>
        /// Whether this audio client supports IAudioClient3 low-latency features (Windows 10 1607+)
        /// </summary>
        public bool SupportsAudioClient3 => audioClientInterface3 != null;

        /// <summary>
        /// Retrieves the stream format that the audio engine uses for its internal processing of shared-mode streams.
        /// Can be called before initialize
        /// </summary>
        public WaveFormat MixFormat
        {
            get
            {
                if (mixFormat == null)
                {
                    CoreAudioException.ThrowIfFailed(audioClientInterface.GetMixFormat(out var waveFormatPointer));
                    var waveFormat = WaveFormat.MarshalFromPtr(waveFormatPointer);
                    Marshal.FreeCoTaskMem(waveFormatPointer);
                    mixFormat = waveFormat;
                }
                return mixFormat;
            }
        }

        /// <summary>
        /// Initializes the Audio Client
        /// </summary>
        /// <param name="shareMode">Share Mode</param>
        /// <param name="streamFlags">Stream Flags</param>
        /// <param name="bufferDuration">Buffer Duration</param>
        /// <param name="periodicity">Periodicity</param>
        /// <param name="waveFormat">Wave Format</param>
        /// <param name="audioSessionGuid">Audio Session GUID (can be null)</param>
        public void Initialize(AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            long bufferDuration,
            long periodicity,
            WaveFormat waveFormat,
            Guid audioSessionGuid)
        {
            this.shareMode = shareMode;
            var formatPtr = WaveFormat.MarshalToPtr(waveFormat);
            try
            {
                CoreAudioException.ThrowIfFailed(
                    audioClientInterface.Initialize(shareMode, streamFlags, bufferDuration, periodicity, formatPtr, in audioSessionGuid));
            }
            finally
            {
                Marshal.FreeHGlobal(formatPtr);
            }
            // may have changed the mix format so reset it
            mixFormat = null;
        }

        /// <summary>
        /// Retrieves the size (maximum capacity) of the audio buffer associated with the endpoint. (must initialize first)
        /// </summary>
        public int BufferSize
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetBufferSize(out uint bufferSize));
                return (int) bufferSize;
            }
        }

        /// <summary>
        /// Retrieves the maximum latency for the current stream and can be called any time after the stream has been initialized.
        /// </summary>
        public long StreamLatency
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetStreamLatency(out var latency));
                return latency;
            }
        }

        /// <summary>
        /// Retrieves the number of frames of padding in the endpoint buffer (must initialize first)
        /// </summary>
        public int CurrentPadding
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetCurrentPadding(out var currentPadding));
                return currentPadding;
            }
        }

        /// <summary>
        /// Retrieves the length of the periodic interval separating successive processing passes by the audio engine on the data in the endpoint buffer.
        /// (can be called before initialize)
        /// </summary>
        public long DefaultDevicePeriod
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetDevicePeriod(out var defaultDevicePeriod, out _));
                return defaultDevicePeriod;
            }
        }

        /// <summary>
        /// Gets the minimum device period
        /// (can be called before initialize)
        /// </summary>
        public long MinimumDevicePeriod
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetDevicePeriod(out _, out var minimumDevicePeriod));
                return minimumDevicePeriod;
            }
        }

        /// <summary>
        /// Returns the AudioStreamVolume service for this AudioClient.
        /// </summary>
        /// <remarks>
        /// This returns the AudioStreamVolume object ONLY for shared audio streams.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// This is thrown when an exclusive audio stream is being used.
        /// </exception>
        public AudioStreamVolume AudioStreamVolume
        {
            get
            {
                if (shareMode == AudioClientShareMode.Exclusive)
                {
                    throw new InvalidOperationException("AudioStreamVolume is ONLY supported for shared audio streams.");
                }
                if (audioStreamVolume == null)
                {
                    CoreAudioException.ThrowIfFailed(audioClientInterface.GetService(ID_AudioStreamVolume, out var ptr));
                    audioStreamVolume = new AudioStreamVolume(ptr);
                }
                return audioStreamVolume;
            }
        }

        /// <summary>
        /// Gets the AudioClockClient service
        /// </summary>
        public AudioClockClient AudioClockClient
        {
            get
            {
                if (audioClockClient == null)
                {
                    CoreAudioException.ThrowIfFailed(audioClientInterface.GetService(ID_AudioClockClient, out var ptr));
                    audioClockClient = new AudioClockClient(ptr);
                }
                return audioClockClient;
            }
        }

        /// <summary>
        /// Gets the AudioRenderClient service
        /// </summary>
        public AudioRenderClient AudioRenderClient
        {
            get
            {
                if (audioRenderClient == null)
                {
                    CoreAudioException.ThrowIfFailed(audioClientInterface.GetService(ID_AudioRenderClient, out var ptr));
                    audioRenderClient = new AudioRenderClient(ptr);
                }
                return audioRenderClient;
            }
        }

        /// <summary>
        /// Gets the AudioCaptureClient service
        /// </summary>
        public AudioCaptureClient AudioCaptureClient
        {
            get
            {
                if (audioCaptureClient == null)
                {
                    CoreAudioException.ThrowIfFailed(audioClientInterface.GetService(ID_AudioCaptureClient, out var ptr));
                    audioCaptureClient = new AudioCaptureClient(ptr);
                }
                return audioCaptureClient;
            }
        }

        /// <summary>
        /// Determines whether if the specified output format is supported
        /// </summary>
        /// <param name="shareMode">The share mode.</param>
        /// <param name="desiredFormat">The desired format.</param>
        /// <returns>True if the format is supported</returns>
        public bool IsFormatSupported(AudioClientShareMode shareMode,
            WaveFormat desiredFormat)
        {
            return IsFormatSupported(shareMode, desiredFormat, out _);
        }

        /// <summary>
        /// Determines if the specified output format is supported in shared mode
        /// </summary>
        /// <param name="shareMode">Share Mode</param>
        /// <param name="desiredFormat">Desired Format</param>
        /// <param name="closestMatchFormat">Output The closest match format.</param>
        /// <returns>True if the format is supported</returns>
        public bool IsFormatSupported(AudioClientShareMode shareMode, WaveFormat desiredFormat, out WaveFormatExtensible closestMatchFormat)
        {
            closestMatchFormat = null;
            var formatPtr = WaveFormat.MarshalToPtr(desiredFormat);
            try
            {
                int hresult = audioClientInterface.IsFormatSupported(shareMode, formatPtr, out var closestMatchPtr);

                if (closestMatchPtr != IntPtr.Zero)
                {
                    closestMatchFormat = Marshal.PtrToStructure<WaveFormatExtensible>(closestMatchPtr);
                    Marshal.FreeCoTaskMem(closestMatchPtr);
                }

                // S_OK is 0, S_FALSE = 1
                if (hresult == 0)
                {
                    return true;
                }
                if (hresult == 1)
                {
                    return false;
                }
                if (hresult == AudioClientErrorCode.UnsupportedFormat)
                {
                    return false;
                }
                CoreAudioException.ThrowIfFailed(hresult);
                throw new NotSupportedException("Unknown hresult " + hresult);
            }
            finally
            {
                Marshal.FreeHGlobal(formatPtr);
            }
        }

        /// <summary>
        /// Starts the audio stream
        /// </summary>
        public void Start()
        {
            CoreAudioException.ThrowIfFailed(audioClientInterface.Start());
        }

        /// <summary>
        /// Stops the audio stream.
        /// </summary>
        public void Stop()
        {
            CoreAudioException.ThrowIfFailed(audioClientInterface.Stop());
        }

        /// <summary>
        /// Set the Event Handle for buffer synchro.
        /// </summary>
        /// <param name="eventWaitHandle">The Wait Handle to setup</param>
        public void SetEventHandle(IntPtr eventWaitHandle)
        {
            CoreAudioException.ThrowIfFailed(audioClientInterface.SetEventHandle(eventWaitHandle));
        }

        /// <summary>
        /// Resets the audio stream
        /// Reset is a control method that the client calls to reset a stopped audio stream.
        /// Resetting the stream flushes all pending data and resets the audio clock stream
        /// position to 0. This method fails if it is called on a stream that is not stopped
        /// </summary>
        public void Reset()
        {
            CoreAudioException.ThrowIfFailed(audioClientInterface.Reset());
        }

        // ---- IAudioClient3 low-latency shared mode ----

        /// <summary>
        /// Returns the range of periodicities supported by the engine for the specified stream format.
        /// Requires Windows 10 1607 or later (IAudioClient3).
        /// </summary>
        public AudioClientPeriodInfo GetSharedModeEnginePeriod(WaveFormat format)
        {
            if (audioClientInterface3 == null)
                throw new PlatformNotSupportedException("IAudioClient3 requires Windows 10 version 1607 or later.");

            var formatPtr = WaveFormat.MarshalToPtr(format);
            try
            {
                CoreAudioException.ThrowIfFailed(
                    audioClientInterface3.GetSharedModeEnginePeriod(formatPtr,
                        out uint defaultPeriod, out uint fundamentalPeriod,
                        out uint minPeriod, out uint maxPeriod));
                return new AudioClientPeriodInfo(defaultPeriod, fundamentalPeriod, minPeriod, maxPeriod);
            }
            finally
            {
                Marshal.FreeHGlobal(formatPtr);
            }
        }

        /// <summary>
        /// Initializes a shared audio stream with the specified periodicity.
        /// Requires Windows 10 1607 or later (IAudioClient3).
        /// </summary>
        public void InitializeSharedAudioStream(
            AudioClientStreamFlags streamFlags,
            uint periodInFrames,
            WaveFormat waveFormat,
            Guid audioSessionGuid)
        {
            if (audioClientInterface3 == null)
                throw new PlatformNotSupportedException("IAudioClient3 requires Windows 10 version 1607 or later.");

            this.shareMode = AudioClientShareMode.Shared;
            var formatPtr = WaveFormat.MarshalToPtr(waveFormat);
            try
            {
                CoreAudioException.ThrowIfFailed(
                    audioClientInterface3.InitializeSharedAudioStream(streamFlags, periodInFrames, formatPtr, in audioSessionGuid));
            }
            finally
            {
                Marshal.FreeHGlobal(formatPtr);
            }
            mixFormat = null;
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioClientInterface != null)
            {
                if (audioClockClient != null)
                {
                    audioClockClient.Dispose();
                    audioClockClient = null;
                }
                if (audioRenderClient != null)
                {
                    audioRenderClient.Dispose();
                    audioRenderClient = null;
                }
                if (audioCaptureClient != null)
                {
                    audioCaptureClient.Dispose();
                    audioCaptureClient = null;
                }
                if (audioStreamVolume != null)
                {
                    audioStreamVolume.Dispose();
                    audioStreamVolume = null;
                }
                if ((object)audioClientInterface is ComObject co)
                {
                    co.FinalRelease();
                }
                audioClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }

    /// <summary>
    /// Information about the audio engine periodicity for shared mode.
    /// </summary>
    public readonly struct AudioClientPeriodInfo
    {
        /// <summary>Default period in frames</summary>
        public uint DefaultPeriodInFrames { get; }
        /// <summary>Fundamental period in frames (all periods must be multiples of this)</summary>
        public uint FundamentalPeriodInFrames { get; }
        /// <summary>Minimum period in frames</summary>
        public uint MinPeriodInFrames { get; }
        /// <summary>Maximum period in frames</summary>
        public uint MaxPeriodInFrames { get; }

        /// <summary>
        /// Creates a new AudioClientPeriodInfo
        /// </summary>
        public AudioClientPeriodInfo(uint defaultPeriod, uint fundamentalPeriod, uint minPeriod, uint maxPeriod)
        {
            DefaultPeriodInFrames = defaultPeriod;
            FundamentalPeriodInFrames = fundamentalPeriod;
            MinPeriodInFrames = minPeriod;
            MaxPeriodInFrames = maxPeriod;
        }
    }
}
