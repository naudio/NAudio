using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.System.Threading;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Dsp;
using NAudio.Wave;
using Windows.Media.Devices;
using NAudio.Utils;
using NAudio.Wave.SampleProviders;

namespace NAudio.Win8.Wave.WaveOutputs
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
    public class WasapiOutRT : IWavePlayer
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
                cbSize = (uint) MarshalHelpers.SizeOf<AudioClientProperties>(),
                bIsOffload = Convert.ToInt32(useHardwareOffload),
                eCategory = category,
                Options = options
            };
        }

        private async Task Activate()
        {
            var icbh = new ActivateAudioInterfaceCompletionHandler(
                ac2 =>
                {
                    
                    if (this.audioClientProperties != null)
                    {
                        IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(this.audioClientProperties.Value));
                        Marshal.StructureToPtr(this.audioClientProperties.Value, p, false);
                        ac2.SetClientProperties(p);
                        Marshal.FreeHGlobal(p);
                        // TODO: consider whether we can marshal this without the need for AllocHGlobal
                    }

                    /*var wfx = new WaveFormat(44100, 16, 2);
                int hr = ac2.Initialize(AudioClientShareMode.Shared,
                               AudioClientStreamFlags.EventCallback | AudioClientStreamFlags.NoPersist,
                               10000000, 0, wfx, IntPtr.Zero);*/
                });
            var IID_IAudioClient2 = new Guid("726778CD-F60A-4eda-82DE-E47610CD78AA");
            IActivateAudioInterfaceAsyncOperation activationOperation;
            NativeMethods.ActivateAudioInterfaceAsync(device, IID_IAudioClient2, IntPtr.Zero, icbh, out activationOperation);
            var audioClient2 = await icbh;
            this.audioClient = new AudioClient((IAudioClient)audioClient2);
        }

        private static string GetDefaultAudioEndpoint()
        {
            // can't use the MMDeviceEnumerator in WinRT

            return MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
        }

        private async void PlayThread()
        {
            await Activate();
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
        public async Task Init(IWaveProvider provider)
        {
            Init(() => provider);
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
            if (!audioClient.IsFormatSupported(shareMode, outputFormat, out closestSampleRateFormat))
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

                // just check that we can make it.
                //using (new MediaFoundationResampler(waveProvider, outputFormat))
                {
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
                // With EventCallBack and Shared, 
                audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback, latencyRefTimes, 0,
                                       outputFormat, Guid.Empty);

                // Get back the effective latency from AudioClient. On Windows 10 it can be 0
                if (audioClient.StreamLatency > 0)
                    latencyMilliseconds = (int) (audioClient.StreamLatency/10000);
            }
            else
            {
                // With EventCallBack and Exclusive, both latencies must equals
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

        /// <summary>
        /// Enables Windows Store apps to access preexisting Component Object Model (COM) interfaces in the WASAPI family.
        /// </summary>
        /// <param name="deviceInterfacePath">A device interface ID for an audio device. This is normally retrieved from a DeviceInformation object or one of the methods of the MediaDevice class.</param>
        /// <param name="riid">The IID of a COM interface in the WASAPI family, such as IAudioClient.</param>
        /// <param name="activationParams">Interface-specific activation parameters. For more information, see the pActivationParams parameter in IMMDevice::Activate. </param>
        /// <param name="completionHandler"></param>
        /// <param name="activationOperation"></param>
        [DllImport("Mmdevapi.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void ActivateAudioInterfaceAsync(
            [In, MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [In] IntPtr activationParams, // n.b. is actually a pointer to a PropVariant, but we never need to pass anything but null
            [In] IActivateAudioInterfaceCompletionHandler completionHandler,
            out IActivateAudioInterfaceAsyncOperation activationOperation);
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

    internal class ActivateAudioInterfaceCompletionHandler :
        IActivateAudioInterfaceCompletionHandler, IAgileObject
    {
        private Action<IAudioClient2> initializeAction;
        private TaskCompletionSource<IAudioClient2> tcs = new TaskCompletionSource<IAudioClient2>();

        public ActivateAudioInterfaceCompletionHandler(
            Action<IAudioClient2> initializeAction)
        {
            this.initializeAction = initializeAction;
        }

        public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
        {
            // First get the activation results, and see if anything bad happened then
            int hr = 0;
            object unk = null;
            activateOperation.GetActivateResult(out hr, out unk);
            if (hr != 0)
            {
                tcs.TrySetException(Marshal.GetExceptionForHR(hr, new IntPtr(-1)));
                return;
            }

            var pAudioClient = (IAudioClient2) unk;

            // Next try to call the client's (synchronous, blocking) initialization method.
            try
            {
                initializeAction(pAudioClient);
                tcs.SetResult(pAudioClient);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }


        }


        public TaskAwaiter<IAudioClient2> GetAwaiter()
        {
            return tcs.Task.GetAwaiter();
        }
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("41D949AB-9862-444A-80F6-C261334DA5EB")]
    interface IActivateAudioInterfaceCompletionHandler
    {
        //virtual HRESULT STDMETHODCALLTYPE ActivateCompleted(/*[in]*/ _In_  
        //   IActivateAudioInterfaceAsyncOperation *activateOperation) = 0;
        void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation);
    }


    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D")]
    interface IActivateAudioInterfaceAsyncOperation
    {
        //virtual HRESULT STDMETHODCALLTYPE GetActivateResult(/*[out]*/ _Out_  
        //  HRESULT *activateResult, /*[out]*/ _Outptr_result_maybenull_  IUnknown **activatedInterface) = 0;
        void GetActivateResult([Out] out int activateResult,
                               [Out, MarshalAs(UnmanagedType.IUnknown)] out object activateInterface);
    }


    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("726778CD-F60A-4eda-82DE-E47610CD78AA")]
    interface IAudioClient2
    {
        [PreserveSig]
        int Initialize(AudioClientShareMode shareMode,
                       AudioClientStreamFlags streamFlags,
                       long hnsBufferDuration, // REFERENCE_TIME
                       long hnsPeriodicity, // REFERENCE_TIME
                       [In] WaveFormat pFormat,
                       [In] IntPtr audioSessionGuid);

        // ref Guid AudioSessionGuid

        /// <summary>
        /// The GetBufferSize method retrieves the size (maximum capacity) of the endpoint buffer.
        /// </summary>
        int GetBufferSize(out uint bufferSize);

        [return: MarshalAs(UnmanagedType.I8)]
        long GetStreamLatency();

        int GetCurrentPadding(out int currentPadding);

        [PreserveSig]
        int IsFormatSupported(
            AudioClientShareMode shareMode,
            [In] WaveFormat pFormat,
            out IntPtr closestMatchFormat);

        int GetMixFormat(out IntPtr deviceFormatPointer);

        // REFERENCE_TIME is 64 bit int        
        int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);

        int Start();

        int Stop();

        int Reset();

        int SetEventHandle(IntPtr eventHandle);

        /// <summary>
        /// The GetService method accesses additional services from the audio client object.
        /// </summary>
        /// <param name="interfaceId">The interface ID for the requested service.</param>
        /// <param name="interfacePointer">Pointer to a pointer variable into which the method writes the address of an instance of the requested interface. </param>
        [PreserveSig]
        int GetService([In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId,
                       [Out, MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);

        //virtual HRESULT STDMETHODCALLTYPE IsOffloadCapable(/*[in]*/ _In_  
        //   AUDIO_STREAM_CATEGORY Category, /*[in]*/ _Out_  BOOL *pbOffloadCapable) = 0;
        void IsOffloadCapable(int category, out bool pbOffloadCapable);
        //virtual HRESULT STDMETHODCALLTYPE SetClientProperties(/*[in]*/ _In_  
        //  const AudioClientProperties *pProperties) = 0;
        void SetClientProperties([In] IntPtr pProperties);
        // TODO: try this: void SetClientProperties([In, MarshalAs(UnmanagedType.LPStruct)] AudioClientProperties pProperties);
        //virtual HRESULT STDMETHODCALLTYPE GetBufferSizeLimits(/*[in]*/ _In_  
        //   const WAVEFORMATEX *pFormat, /*[in]*/ _In_  BOOL bEventDriven, /*[in]*/ 
        //  _Out_  REFERENCE_TIME *phnsMinBufferDuration, /*[in]*/ _Out_  
        //  REFERENCE_TIME *phnsMaxBufferDuration) = 0;
        void GetBufferSizeLimits(IntPtr pFormat, bool bEventDriven,
                                 out long phnsMinBufferDuration, out long phnsMaxBufferDuration);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90")]
    interface IAgileObject
    {

    }


}
