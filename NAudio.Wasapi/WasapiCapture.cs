using System;
using System.Threading;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Threading.Tasks;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

// for consistency this should be in NAudio.Wave namespace, but left as it is for backwards compatibility
// ReSharper disable once CheckNamespace
namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Capture using Wasapi
    /// See http://msdn.microsoft.com/en-us/library/dd370800%28VS.85%29.aspx
    /// </summary>
    public class WasapiCapture : IWaveIn
    {
        private const long ReftimesPerSec = 10000000;
        private const long ReftimesPerMillisec = 10000;
        private volatile CaptureState captureState;
        private byte[] recordBuffer;
        private Thread captureThread;
        private AudioClient audioClient;
        private int bytesPerFrame;
        private WaveFormat waveFormat;
        private bool initialized;
        private readonly SynchronizationContext syncContext;
        private readonly bool isUsingEventSync;
        private EventWaitHandle frameEventWaitHandle;
        private readonly int audioBufferMillisecondsLength;
        private AudioClientStreamFlags audioClientStreamFlags;

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        public WasapiCapture() : 
            this(GetDefaultCaptureDevice())
        {
        }

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        /// <param name="captureDevice">Capture device to use</param>
        public WasapiCapture(MMDevice captureDevice)
            : this(captureDevice, false)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiCapture"/> class.
        /// </summary>
        /// <param name="captureDevice">The capture device.</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        public WasapiCapture(MMDevice captureDevice, bool useEventSync) 
            : this(captureDevice, useEventSync, 100)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiCapture" /> class.
        /// </summary>
        /// <param name="captureDevice">The capture device.</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        /// <param name="audioBufferMillisecondsLength">Length of the audio buffer in milliseconds. A lower value means lower latency but increased CPU usage.</param>
        public WasapiCapture(MMDevice captureDevice, bool useEventSync, int audioBufferMillisecondsLength)
            : this(captureDevice.AudioClient, useEventSync, audioBufferMillisecondsLength)
        {
        }


        private WasapiCapture(AudioClient audioClient, bool useEventSync, int audioBufferMillisecondsLength)
        {
            syncContext = SynchronizationContext.Current;
            this.audioClient = audioClient;
            ShareMode = AudioClientShareMode.Shared;
            isUsingEventSync = useEventSync;
            this.audioBufferMillisecondsLength = audioBufferMillisecondsLength;
            // enable auto-convert PCM
            this.audioClientStreamFlags = AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality;
            waveFormat = audioClient.MixFormat;
        }

        public static async Task<WasapiCapture> CreateForProcessCaptureAsync(int processId, bool includeProcessTree)
        {
            // https://github.com/microsoft/Windows-classic-samples/blob/main/Samples/ApplicationLoopback/cpp/LoopbackCapture.cpp
            var activationParams = new AudioClientActivationParams()
            {
                ActivationType = AudioClientActivationType.ProcessLoopback,
                ProcessLoopbackParams = new AudioClientProcessLoopbackParams()
                {
                    ProcessLoopbackMode = includeProcessTree ? ProcessLoopbackMode.IncludeTargetProcessTree : 
                        ProcessLoopbackMode.ExcludeTargetProcessTree,
                    TargetProcessId = (uint)processId
                }
            };
            var blobData = Marshal.AllocHGlobal(Marshal.SizeOf(activationParams));
            try
            {
                Marshal.StructureToPtr(activationParams, blobData, false);
                var activateParams = new PropVariant()
                {
                    vt = (short)VarEnum.VT_BLOB,
                    blobVal = new Blob()
                    {
                        Length = Marshal.SizeOf(activationParams),
                        Data = blobData
                    }
                };
                WasapiCapture capture = null;
                var icbh = new ActivateAudioInterfaceCompletionHandler1(ac2 => { 
                    var audioClient = new AudioClient(ac2);
                    capture = new WasapiCapture(audioClient, true, 100);
                    capture.audioClientStreamFlags |= AudioClientStreamFlags.Loopback;
                    capture.WaveFormat = new WaveFormat(); // ask for capture at 44.1, stereo 16 bit
                });
                var IID_IAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
                var propVariant = Marshal.AllocHGlobal(Marshal.SizeOf(activateParams));
                try
                {
                    Marshal.StructureToPtr(activateParams, propVariant, false);
                    NativeMethods.ActivateAudioInterfaceAsync(VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK, IID_IAudioClient, propVariant,
                        icbh, out var activationOperation);
                    var audioClientInterface = await icbh;
                    return capture;
                }
                finally
                {
                    Marshal.FreeHGlobal(propVariant);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(blobData);
            }
        }
        
        private const string VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK = "VAD\\Process_Loopback";


        /// <summary>
        /// Share Mode - set before calling StartRecording
        /// </summary>
        public AudioClientShareMode ShareMode { get; set; }

        /// <summary>
        /// Current Capturing State
        /// </summary>
        public CaptureState CaptureState {  get { return captureState; } }

        /// <summary>
        /// Capturing wave format
        /// </summary>
        public virtual WaveFormat WaveFormat 
        {
            get
            {
                // for convenience, return a WAVEFORMATEX, instead of the real
                // WAVEFORMATEXTENSIBLE being used
                return waveFormat.AsStandardWaveFormat();
            }
            set { waveFormat = value; }
        }

        /// <summary>
        /// Gets the default audio capture device
        /// </summary>
        /// <returns>The default audio capture device</returns>
        public static MMDevice GetDefaultCaptureDevice()
        {
            var devices = new MMDeviceEnumerator();
            return devices.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        }

        private void InitializeCaptureDevice()
        {
            if (initialized)
                return;

            long requestedDuration = ReftimesPerMillisec * audioBufferMillisecondsLength;

            var streamFlags = GetAudioClientStreamFlags();

            // If using EventSync, setup is specific with shareMode
            if (isUsingEventSync)
            {
                // Init Shared or Exclusive
                if (ShareMode == AudioClientShareMode.Shared)
                {
                    // With EventCallBack and Shared, both latencies must be set to 0
                    audioClient.Initialize(ShareMode, AudioClientStreamFlags.EventCallback | streamFlags, requestedDuration, 0,
                        waveFormat, Guid.Empty);
                }
                else
                {
                    // With EventCallBack and Exclusive, both latencies must equals
                    audioClient.Initialize(ShareMode, AudioClientStreamFlags.EventCallback | streamFlags, requestedDuration, requestedDuration,
                                        waveFormat, Guid.Empty);
                }

                // Create the Wait Event Handle
                frameEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                audioClient.SetEventHandle(frameEventWaitHandle.SafeWaitHandle.DangerousGetHandle());
            }
            else
            {
                // Normal setup for both sharedMode
                audioClient.Initialize(ShareMode,
                streamFlags,
                requestedDuration,
                0,
                waveFormat,
                Guid.Empty);
            }

            int bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = waveFormat.Channels * waveFormat.BitsPerSample / 8;
            recordBuffer = new byte[bufferFrameCount * bytesPerFrame];
            
            //Debug.WriteLine(string.Format("record buffer size = {0}", this.recordBuffer.Length));

            initialized = true;
        }

        /// <summary>
        /// To allow overrides to specify different flags (e.g. loopback)
        /// </summary>
        protected virtual AudioClientStreamFlags GetAudioClientStreamFlags()
        {
            return audioClientStreamFlags;
        }

        /// <summary>
        /// Start Capturing
        /// </summary>
        public void StartRecording()
        {
            if (captureState != CaptureState.Stopped)
            {
                throw new InvalidOperationException("Previous recording still in progress");
            }
            captureState = CaptureState.Starting;
            InitializeCaptureDevice();
            captureThread = new Thread(() => CaptureThread(audioClient));
            captureThread.Start();
        }

        /// <summary>
        /// Stop Capturing (requests a stop, wait for RecordingStopped event to know it has finished)
        /// </summary>
        public void StopRecording()
        {
            if (captureState != CaptureState.Stopped)
                captureState = CaptureState.Stopping;
        }

        private void CaptureThread(AudioClient client)
        {
            Exception exception = null;
            try
            {
                DoRecording(client);
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                client.Stop();
                // don't dispose - the AudioClient only gets disposed when WasapiCapture is disposed
            }
            captureThread = null;
            captureState = CaptureState.Stopped;
            RaiseRecordingStopped(exception);
        }

        private void DoRecording(AudioClient client)
        {
            //Debug.WriteLine(String.Format("Client buffer frame count: {0}", client.BufferSize));
            int bufferFrameCount = client.BufferSize;

            // Calculate the actual duration of the allocated buffer.
            long actualDuration = (long)((double)ReftimesPerSec *
                             bufferFrameCount / waveFormat.SampleRate);
            int sleepMilliseconds = (int)(actualDuration / ReftimesPerMillisec / 2);
            int waitMilliseconds = (int)(3 * actualDuration / ReftimesPerMillisec);

            var capture = client.AudioCaptureClient;
            client.Start();
            // avoid race condition where we stop immediately after starting
            if (captureState == CaptureState.Starting)
            {
                captureState = CaptureState.Capturing;
            }
            while (captureState == CaptureState.Capturing)
            {
                if (isUsingEventSync)
                {
                    frameEventWaitHandle.WaitOne(waitMilliseconds, false);
                }
                else
                {
                    Thread.Sleep(sleepMilliseconds);
                }
                if (captureState != CaptureState.Capturing)
                    break;

                // If still recording
                ReadNextPacket(capture);
            }
        }

        private void RaiseRecordingStopped(Exception e)
        {
            var handler = RecordingStopped;
            if (handler == null) return;
            if (syncContext == null)
            {
                handler(this, new StoppedEventArgs(e));
            }
            else
            {
                syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
            }
        }

        private void ReadNextPacket(AudioCaptureClient capture)
        {
            int packetSize = capture.GetNextPacketSize();
            int recordBufferOffset = 0;
            //Debug.WriteLine(string.Format("packet size: {0} samples", packetSize / 4));

            while (packetSize != 0)
            {
                IntPtr buffer = capture.GetBuffer(out int framesAvailable, out AudioClientBufferFlags flags);

                int bytesAvailable = framesAvailable * bytesPerFrame;

                // apparently it is sometimes possible to read more frames than we were expecting?
                // fix suggested by Michael Feld:
                int spaceRemaining = Math.Max(0, recordBuffer.Length - recordBufferOffset);
                if (spaceRemaining < bytesAvailable && recordBufferOffset > 0)
                {
                    DataAvailable?.Invoke(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
                    recordBufferOffset = 0;
                }

                // if not silence...
                if ((flags & AudioClientBufferFlags.Silent) != AudioClientBufferFlags.Silent)
                {
                    Marshal.Copy(buffer, recordBuffer, recordBufferOffset, bytesAvailable);
                }
                else
                {
                    Array.Clear(recordBuffer, recordBufferOffset, bytesAvailable);
                }
                recordBufferOffset += bytesAvailable;
                capture.ReleaseBuffer(framesAvailable);
                packetSize = capture.GetNextPacketSize();
            }
            DataAvailable?.Invoke(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            StopRecording();
            if (captureThread != null)
            {
                captureThread.Join();
                captureThread = null;
            }
            if (audioClient != null)
            {
                audioClient.Dispose();
                audioClient = null;
            }
        }
    }
}
