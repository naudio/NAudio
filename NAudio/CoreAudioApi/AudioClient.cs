using System;
using System.Threading;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Windows Vista CoreAudio AudioClient
    /// </summary>
    public class AudioClient : IDisposable
    {
        IAudioClient audioClientInterface;
        WaveFormat mixFormat;
        AudioRenderClient audioRenderClient;
        AudioCaptureClient audioCaptureClient;

        internal AudioClient(IAudioClient audioClientInterface)
        {
            this.audioClientInterface = audioClientInterface;
        }

        /// <summary>
        /// Mix Format,
        /// Can be called before initialize
        /// </summary>
        public WaveFormat MixFormat
        {
            get
            {
                if(mixFormat == null)
                {
                    IntPtr waveFormatPointer;
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetMixFormat(out waveFormatPointer));
                    //WaveFormatExtensible waveFormat = new WaveFormatExtensible(44100,32,2);
                    //Marshal.PtrToStructure(waveFormatPointer, waveFormat);
                    WaveFormat waveFormat = WaveFormat.MarshalFromPtr(waveFormatPointer);
                    Marshal.FreeCoTaskMem(waveFormatPointer);
                    mixFormat = waveFormat;
                    return waveFormat;
                }
                else
                {
                    return mixFormat;
                }
            }
        }

        /// <summary>
        /// Initialize the Audio Client
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
            int hresult = audioClientInterface.Initialize(shareMode, streamFlags, bufferDuration, periodicity, waveFormat, ref audioSessionGuid);
            Marshal.ThrowExceptionForHR(hresult);
            // may have changed the mix format so reset it
            mixFormat = null;
        }

        /// <summary>
        /// Gets the buffer size (must initialize first)
        /// </summary>
        public int BufferSize
        {
            get
            {
                uint bufferSize;                
                Marshal.ThrowExceptionForHR(audioClientInterface.GetBufferSize(out bufferSize));
                return (int) bufferSize;
            }
        }

        /// <summary>
        /// Gets the stream latency (must initialize first)
        /// </summary>
        public long StreamLatency
        {
            get
            {
                return audioClientInterface.GetStreamLatency();
            }
        }

        /// <summary>
        /// Gets the current padding (must initialize first)
        /// </summary>
        public int CurrentPadding
        {
            get
            {
                int currentPadding;
                Marshal.ThrowExceptionForHR(audioClientInterface.GetCurrentPadding(out currentPadding));
                return currentPadding;
            }
        }

        /// <summary>
        /// Gets the default device period (can be called before initialize)
        /// </summary>
        public long DefaultDevicePeriod
        {
            get
            {
                long defaultDevicePeriod;
                long minimumDevicePeriod;
                Marshal.ThrowExceptionForHR(audioClientInterface.GetDevicePeriod(out defaultDevicePeriod, out minimumDevicePeriod));
                return defaultDevicePeriod;
            }
        }

        /// <summary>
        /// Gets the minimum device period (can be called before initialize)
        /// </summary>
        public long MinimumDevicePeriod
        {
            get
            {
                long defaultDevicePeriod;
                long minimumDevicePeriod;
                Marshal.ThrowExceptionForHR(audioClientInterface.GetDevicePeriod(out defaultDevicePeriod, out minimumDevicePeriod));
                return minimumDevicePeriod;
            }
        }

        // TODO: GetService:
        // IID_IAudioCaptureClient
        // IID_IAudioClock
        // IID_IAudioSessionControl
        // IID_IAudioStreamVolume
        // IID_IChannelAudioVolume
        // IID_ISimpleAudioVolume

        
        /// <summary>
        /// Gets the AudioRenderClient service
        /// </summary>
        public AudioRenderClient AudioRenderClient
        {
            get
            {
                if (audioRenderClient == null)
                {
                    object audioRenderClientInterface;
                    Guid audioRenderClientGuid = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(ref audioRenderClientGuid, out audioRenderClientInterface));
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
                    object audioCaptureClientInterface;
                    Guid audioCaptureClientGuid = new Guid("c8adbd64-e71e-48a0-a4de-185c395cd317");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(ref audioCaptureClientGuid, out audioCaptureClientInterface));
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
        /// <returns>
        /// 	<c>true</c> if [is format supported] [the specified share mode]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsFormatSupported(AudioClientShareMode shareMode,
            WaveFormat desiredFormat)
        {
            WaveFormatExtensible closestMatchFormat;
            return IsFormatSupported(shareMode, desiredFormat, out closestMatchFormat);
        }



        /// <summary>
        /// Determines if the specified output format is supported in shared mode
        /// </summary>
        /// <param name="shareMode">Share Mode</param>
        /// <param name="desiredFormat">Desired Format</param>
        /// <param name="closestMatchFormat">Output The closest match format.</param>
        /// <returns>
        /// 	<c>true</c> if [is format supported] [the specified share mode]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsFormatSupported(AudioClientShareMode shareMode, WaveFormat desiredFormat, out WaveFormatExtensible closestMatchFormat)
        {
            int hresult = audioClientInterface.IsFormatSupported(shareMode, desiredFormat, out closestMatchFormat);
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
            else if (hresult == (int)AudioClientErrors.UnsupportedFormat)
            {
                return false;
            }
            else
            {
                Marshal.ThrowExceptionForHR(hresult);
            }
            // shouldn't get here
            throw new NotSupportedException("Unknown hresult " + hresult.ToString());
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
        public void SetEventHandle(EventWaitHandle eventWaitHandle) 
        {
            audioClientInterface.SetEventHandle(eventWaitHandle.SafeWaitHandle.DangerousGetHandle());
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
                Marshal.ReleaseComObject(audioClientInterface);
                audioClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
