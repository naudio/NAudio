using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using Windows.Media.Devices;
using NAudio.Wave.SampleProviders;

namespace NAudio.Wave
{
    enum WasapiOutState
    {
        Uninitialized,
        Stopped,
        Paused,
        Playing,
        Stopping,
        Disposing,
        Disposed
    }

    /// <summary>
    /// WASAPI Out for Windows RT
    /// </summary>
    public class WasapiOutRT
    {
        private AudioClient audioClient;
        private readonly string device;
        private readonly AudioClientShareMode shareMode;
        private AudioRenderClient renderClient;
        private int latencyMilliseconds;
        private int bufferFrameCount;
        private int bytesPerFrame;
        private byte[] readBuffer;
        private volatile WasapiOutState playbackState;
        private WaveFormat outputFormat;
        private bool resamplerNeeded;
        private IntPtr frameEventWaitHandle;
        private readonly SynchronizationContext syncContext;
        private bool isInitialized;
        private readonly AutoResetEvent playThreadEvent;

        /// <summary>
        /// Playback Stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// WASAPI Out using default audio endpoint
        /// </summary>
        /// <param name="shareMode">ShareMode - shared or exclusive</param>
        /// <param name="latency">Desired latency in milliseconds</param>
        public WasapiOutRT(AudioClientShareMode shareMode, int latency) :
            this(GetDefaultAudioEndpoint(), shareMode, latency)
        {

        }

        /// <summary>
        /// Creates a new WASAPI Output
        /// </summary>
        /// <param name="device">Device to use</param>
        /// <param name="shareMode"></param>
        /// <param name="latency"></param>
        public WasapiOutRT(string device, AudioClientShareMode shareMode, int latency)
        {
            this.device = device;
            this.shareMode = shareMode;
            this.latencyMilliseconds = latency;
            this.syncContext = SynchronizationContext.Current;
            playThreadEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Properties of the client's audio stream.
        /// Set before calling init
        /// </summary>
        private AudioClientProperties? audioClientProperties = null;

        private Func<IWaveProvider> waveProviderFunc;

        /// <summary>
        /// Sets the parameters that describe the properties of the client's audio stream.
        /// </summary>
        /// <param name="useHardwareOffload">Boolean value to indicate whether or not the audio stream is hardware-offloaded.</param>
        /// <param name="category">An enumeration that is used to specify the category of the audio stream.</param>
        /// <param name="options">A bit-field describing the characteristics of the stream. Supported in Windows 8.1 and later.</param>
        public void SetClientProperties(bool useHardwareOffload, AudioStreamCategory category, AudioClientStreamOptions options)
        {
            audioClientProperties = new AudioClientProperties()
            {
                cbSize = (uint) Marshal.SizeOf<AudioClientProperties>(),
                bIsOffload = Convert.ToInt32(useHardwareOffload),
                eCategory = category,
                Options = options
            };
        }



        private static string GetDefaultAudioEndpoint()
        {
            // can't use the MMDeviceEnumerator in WinRT

            return MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
        }

        private async void PlayThread()
        {
            audioClient = await AudioClient.ActivateAsync(device, audioClientProperties);
            var playbackProvider = Init();
            bool isClientRunning = false;
            try
            {
                if (this.resamplerNeeded)
                {
                    var resampler = new WdlResamplingSampleProvider(playbackProvider.ToSampleProvider(), outputFormat.SampleRate);
                    playbackProvider = new SampleToWaveProvider(resampler);
                }

                // fill a whole buffer
                bufferFrameCount = audioClient.BufferSize;
                bytesPerFrame = outputFormat.Channels*outputFormat.BitsPerSample/8;
                readBuffer = new byte[bufferFrameCount*bytesPerFrame];
                FillBuffer(playbackProvider, bufferFrameCount);
                int timeout = 3 * latencyMilliseconds;
                
                while (playbackState != WasapiOutState.Disposed)
                {
                    if (playbackState != WasapiOutState.Playing)
                    {
                        playThreadEvent.WaitOne(500);
                    }
                    
                    // If still playing and notification is ok
                    if (playbackState == WasapiOutState.Playing)
                    {
                        if (!isClientRunning)
                        {
                            audioClient.Start();
                            isClientRunning = true;
                        }
                        // If using Event Sync, Wait for notification from AudioClient or Sleep half latency
                        var r = NativeMethods.WaitForSingleObjectEx(frameEventWaitHandle, timeout, true);
                        if (r != 0) throw new InvalidOperationException("Timed out waiting for event");
                        // See how much buffer space is available.
                        int numFramesPadding = 0;
                        // In exclusive mode, always ask the max = bufferFrameCount = audioClient.BufferSize
                        numFramesPadding = (shareMode == AudioClientShareMode.Shared) ? audioClient.CurrentPadding : 0;

                        int numFramesAvailable = bufferFrameCount - numFramesPadding;
                        if (numFramesAvailable > 0)
                        {
                            FillBuffer(playbackProvider, numFramesAvailable);
                        }
                    }

                    if (playbackState == WasapiOutState.Stopping)
                    {
                        // play the buffer out
                        while (audioClient.CurrentPadding > 0)
                        {
                            await Task.Delay(latencyMilliseconds / 2);
                        }
                        audioClient.Stop();
                        isClientRunning = false;
                        audioClient.Reset();
                        playbackState = WasapiOutState.Stopped;
                        RaisePlaybackStopped(null);
                    }
                    if (playbackState == WasapiOutState.Disposing)
                    {
                        audioClient.Stop();
                        isClientRunning = false;
                        audioClient.Reset();
                        playbackState = WasapiOutState.Disposed;
                        var disposablePlaybackProvider = playbackProvider as IDisposable;
                        if (disposablePlaybackProvider!=null)
                            disposablePlaybackProvider.Dispose(); // do everything on this thread, even dispose in case it is Media Foundation
                        RaisePlaybackStopped(null);

                    }

                }
            }
            catch (Exception e)
            {
                RaisePlaybackStopped(e);
            }
            finally
            {
                audioClient.Dispose();
                audioClient = null;
                renderClient = null;
                NativeMethods.CloseHandle(frameEventWaitHandle);

            }
        }

        private void RaisePlaybackStopped(Exception e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                if (this.syncContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }

        private void FillBuffer(IWaveProvider playbackProvider, int frameCount)
        {
            IntPtr buffer = renderClient.GetBuffer(frameCount);
            int readLength = frameCount*bytesPerFrame;
            int read = playbackProvider.Read(readBuffer, 0, readLength);
            if (read == 0)
            {
                playbackState = WasapiOutState.Stopping;
            }
            Marshal.Copy(readBuffer, 0, buffer, read);
            int actualFrameCount = read/bytesPerFrame;
            /*if (actualFrameCount != frameCount)
            {
                Debug.WriteLine(String.Format("WASAPI wanted {0} frames, supplied {1}", frameCount, actualFrameCount ));
            }*/
            renderClient.ReleaseBuffer(actualFrameCount, AudioClientBufferFlags.None);
        }

        #region IWavePlayer Members

        /// <summary>
        /// Begin Playback
        /// </summary>
        public void Play()
        {
            if (playbackState != WasapiOutState.Playing)
            {
                playbackState = WasapiOutState.Playing;
                playThreadEvent.Set();
            }
        }

        /// <summary>
        /// Stop playback and flush buffers
        /// </summary>
        public void Stop()
        {
            if (playbackState == WasapiOutState.Playing || playbackState == WasapiOutState.Paused)
            {
                playbackState = WasapiOutState.Stopping;
                playThreadEvent.Set();
            }
        }

        /// <summary>
        /// Stop playback without flushing buffers
        /// </summary>
        public void Pause()
        {
            if (playbackState == WasapiOutState.Playing)
            {
                playbackState = WasapiOutState.Paused;
                playThreadEvent.Set();
            }
        }

        /// <summary>
        /// Old init implementation. Use the func one
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        [Obsolete]
        public Task Init(IWaveProvider provider)
        {
            Init(() => provider);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes with a function to create the provider that is made on the playback thread
        /// </summary>
        /// <param name="waveProviderFunc">Creates the wave provider</param>
        public void Init(Func<IWaveProvider> waveProviderFunc)
        {
            if (isInitialized) throw new InvalidOperationException("Already Initialized");
            isInitialized = true;
            this.waveProviderFunc = waveProviderFunc;
            Task.Factory.StartNew(() =>
            {
                PlayThread();
            });
        }


        /// <summary>
        /// Initialize for playing the specified wave stream
        /// </summary>
        private IWaveProvider Init()
        {
            var waveProvider = waveProviderFunc();
            long latencyRefTimes = latencyMilliseconds*10000;
            outputFormat = waveProvider.WaveFormat;
            // first attempt uses the WaveFormat from the WaveStream
            WaveFormatExtensible closestSampleRateFormat;
            if (shareMode == AudioClientShareMode.Exclusive && !audioClient.IsFormatSupported(shareMode, outputFormat, out closestSampleRateFormat))
            {
                // Use closesSampleRateFormat (in sharedMode, it equals usualy to the audioClient.MixFormat)
                // See documentation : http://msdn.microsoft.com/en-us/library/ms678737(VS.85).aspx 
                // They say : "In shared mode, the audio engine always supports the mix format"
                // The MixFormat is more likely to be a WaveFormatExtensible.
                if (closestSampleRateFormat == null)
                {
                    WaveFormat correctSampleRateFormat = audioClient.MixFormat;
                    /*WaveFormat.CreateIeeeFloatWaveFormat(
                    audioClient.MixFormat.SampleRate,
                    audioClient.MixFormat.Channels);*/

                    if (!audioClient.IsFormatSupported(shareMode, correctSampleRateFormat))
                    {
                        // Iterate from Worst to Best Format
                        WaveFormatExtensible[] bestToWorstFormats =
                            {
                                new WaveFormatExtensible(
                                    outputFormat.SampleRate, 32,
                                    outputFormat.Channels),
                                new WaveFormatExtensible(
                                    outputFormat.SampleRate, 24,
                                    outputFormat.Channels),
                                new WaveFormatExtensible(
                                    outputFormat.SampleRate, 16,
                                    outputFormat.Channels),
                            };

                        // Check from best Format to worst format ( Float32, Int24, Int16 )
                        for (int i = 0; i < bestToWorstFormats.Length; i++)
                        {
                            correctSampleRateFormat = bestToWorstFormats[i];
                            if (audioClient.IsFormatSupported(shareMode, correctSampleRateFormat))
                            {
                                break;
                            }
                            correctSampleRateFormat = null;
                        }

                        // If still null, then test on the PCM16, 2 channels
                        if (correctSampleRateFormat == null)
                        {
                            // Last Last Last Chance (Thanks WASAPI)
                            correctSampleRateFormat = new WaveFormatExtensible(outputFormat.SampleRate, 16, 2);
                            if (!audioClient.IsFormatSupported(shareMode, correctSampleRateFormat))
                            {
                                throw new NotSupportedException("Can't find a supported format to use");
                            }
                        }
                    }
                    outputFormat = correctSampleRateFormat;
                }
                else
                {
                    outputFormat = closestSampleRateFormat;
                }

                this.resamplerNeeded = true;
            }
            else
            {
                resamplerNeeded = false;
            }

            
            // Init Shared or Exclusive
            if (shareMode == AudioClientShareMode.Shared)
            {
                audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback |
                    AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality, latencyRefTimes, 0,
                                       outputFormat, Guid.Empty);

                // Get back the effective latency from AudioClient. On Windows 10 it can be 0
                if (audioClient.StreamLatency > 0)
                    latencyMilliseconds = (int) (audioClient.StreamLatency/10000);
            }
            else
            {
                // With EventCallBack and Exclusive, both latencies must equal
                audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback, latencyRefTimes, latencyRefTimes,
                                       outputFormat, Guid.Empty);
            }

            // Create the Wait Event Handle
            frameEventWaitHandle = NativeMethods.CreateEventExW(IntPtr.Zero, IntPtr.Zero, 0, EventAccess.EVENT_ALL_ACCESS);
            audioClient.SetEventHandle(frameEventWaitHandle);

            // Get the RenderClient
            renderClient = audioClient.AudioRenderClient;
            return waveProvider;
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState
        {
            get
            {
                switch (playbackState)
                {
                    case WasapiOutState.Playing:
                        return PlaybackState.Playing;
                    case WasapiOutState.Paused:
                        return PlaybackState.Paused;
                    default:
                        return PlaybackState.Stopped;
                }
            }
        }
       
        #endregion

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioClient != null)
            {
                playbackState = WasapiOutState.Disposing;
                playThreadEvent.Set();
            }
        }
    }

    /// <summary>
    /// Some useful native methods for Windows 8/10 support ( https://msdn.microsoft.com/en-us/library/windows/desktop/hh802935(v=vs.85).aspx )
    /// </summary>
    class NativeMethods
    {
        [DllImport("api-ms-win-core-synch-l1-2-0.dll", CharSet = CharSet.Unicode, ExactSpelling = false,
            PreserveSig = true, SetLastError = true)]
        internal static extern IntPtr CreateEventExW(IntPtr lpEventAttributes, IntPtr lpName, int dwFlags,
                                                    EventAccess dwDesiredAccess);


        [DllImport("api-ms-win-core-handle-l1-1-0.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("api-ms-win-core-synch-l1-2-0.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern int WaitForSingleObjectEx(IntPtr hEvent, int milliseconds, bool bAlertable);

    }

    // trying some ideas from Lucian Wischik (ljw1004):
    // http://www.codeproject.com/Articles/460145/Recording-and-playing-PCM-audio-on-Windows-8-VB

    [Flags]
    internal enum EventAccess
    {
        STANDARD_RIGHTS_REQUIRED = 0xF0000,
        SYNCHRONIZE = 0x100000,
        EVENT_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x3
    }




}
