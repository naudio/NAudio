using System;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Windows CoreAudio AudioClient
    /// </summary>
    public class AudioClient : IDisposable
    {
        private IAudioClient audioClientInterface;
        private WaveFormat mixFormat;
        private AudioRenderClient audioRenderClient;
        private AudioCaptureClient audioCaptureClient;
        private AudioClockClient audioClockClient;
        private AudioStreamVolume audioStreamVolume;
        private AudioClientShareMode shareMode;

        public AudioClient(IAudioClient audioClientInterface)
        {
            this.audioClientInterface = audioClientInterface;
        }

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
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetMixFormat(out var waveFormatPointer));
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
            int hresult = audioClientInterface.Initialize(shareMode, streamFlags, bufferDuration, periodicity, waveFormat, ref audioSessionGuid);
            Marshal.ThrowExceptionForHR(hresult);
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
                Marshal.ThrowExceptionForHR(audioClientInterface.GetBufferSize(out uint bufferSize));
                return (int) bufferSize;
            }
        }

        /// <summary>
        /// Retrieves the maximum latency for the current stream and can be called any time after the stream has been initialized.
        /// </summary>
        public long StreamLatency => audioClientInterface.GetStreamLatency();

        /// <summary>
        /// Retrieves the number of frames of padding in the endpoint buffer (must initialize first)
        /// </summary>
        public int CurrentPadding
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioClientInterface.GetCurrentPadding(out var currentPadding));
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
                Marshal.ThrowExceptionForHR(audioClientInterface.GetDevicePeriod(out var defaultDevicePeriod, out _));
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
                Marshal.ThrowExceptionForHR(audioClientInterface.GetDevicePeriod(out _, out var minimumDevicePeriod));
                return minimumDevicePeriod;
            }
        }

        // TODO: GetService:
        // IID_IAudioSessionControl
        // IID_IChannelAudioVolume
        // IID_ISimpleAudioVolume

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
                    var audioStreamVolumeGuid = new Guid("93014887-242D-4068-8A15-CF5E93B90FE3");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioStreamVolumeGuid, out var audioStreamVolumeInterface));
                    audioStreamVolume = new AudioStreamVolume((IAudioStreamVolume)audioStreamVolumeInterface);
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
                    var audioClockClientGuid = new Guid("CD63314F-3FBA-4a1b-812C-EF96358728E7");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioClockClientGuid, out var audioClockClientInterface));
                    audioClockClient = new AudioClockClient((IAudioClock)audioClockClientInterface);
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
                    var audioRenderClientGuid = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioRenderClientGuid, out var audioRenderClientInterface));
                    audioRenderClient = new AudioRenderClient((IAudioRenderClient)audioRenderClientInterface);
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
                    var audioCaptureClientGuid = new Guid("c8adbd64-e71e-48a0-a4de-185c395cd317");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioCaptureClientGuid, out var audioCaptureClientInterface));
                    audioCaptureClient = new AudioCaptureClient((IAudioCaptureClient)audioCaptureClientInterface);
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

        private IntPtr GetPointerToPointer()
        {
            return Marshal.AllocHGlobal(Marshal.SizeOf<IntPtr>());
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
            IntPtr pointerToPtr = GetPointerToPointer(); // IntPtr.Zero; // Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatExtensible>());
            closestMatchFormat = null;
            int hresult = audioClientInterface.IsFormatSupported(shareMode, desiredFormat, pointerToPtr);

            var closestMatchPtr = Marshal.PtrToStructure<IntPtr>(pointerToPtr);

            if (closestMatchPtr != IntPtr.Zero)
            {
                closestMatchFormat = Marshal.PtrToStructure<WaveFormatExtensible>(closestMatchPtr);
                Marshal.FreeCoTaskMem(closestMatchPtr);
            }
            Marshal.FreeHGlobal(pointerToPtr);
            // S_OK is 0, S_FALSE = 1
            if (hresult == 0)
            {

                // directly supported
                return true;
            }
            if (hresult == 1)
            {
                return false;
            }
            if (hresult == (int)AudioClientErrors.UnsupportedFormat)
            {
                // documentation is confusing as to what this flag means
                // https://docs.microsoft.com/en-us/windows/desktop/api/audioclient/nf-audioclient-iaudioclient-isformatsupported
                // "Succeeded but the specified format is not supported in exclusive mode."
                return false; // shareMode != AudioClientShareMode.Exclusive;
            }
            Marshal.ThrowExceptionForHR(hresult);
            // shouldn't get here
            throw new NotSupportedException("Unknown hresult " + hresult);
        }

        /// <summary>
        /// Starts the audio stream
        /// </summary>
        public void Start()
        {
            audioClientInterface.Start();
        }

        /// <summary>
        /// Stops the audio stream.
        /// </summary>
        public void Stop()
        {
            audioClientInterface.Stop();
        }

        /// <summary>
        /// Set the Event Handle for buffer synchro.
        /// </summary>
        /// <param name="eventWaitHandle">The Wait Handle to setup</param>
        public void SetEventHandle(IntPtr eventWaitHandle)
        {
            audioClientInterface.SetEventHandle(eventWaitHandle);
        }

        /// <summary>
        /// Resets the audio stream
        /// Reset is a control method that the client calls to reset a stopped audio stream. 
        /// Resetting the stream flushes all pending data and resets the audio clock stream 
        /// position to 0. This method fails if it is called on a stream that is not stopped
        /// </summary>
        public void Reset()
        {
            audioClientInterface.Reset();
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
                Marshal.ReleaseComObject(audioClientInterface);
                audioClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
