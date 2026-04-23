using System;
using System.Threading;
using System.Runtime.InteropServices;
using NAudio.Utils;
using NAudio.Wave;

// for consistency this should be in NAudio.Wave namespace, but left as it is for backwards compatibility
// ReSharper disable once CheckNamespace
namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Capture using Wasapi
    /// See http://msdn.microsoft.com/en-us/library/dd370800%28VS.85%29.aspx
    /// </summary>
    [Obsolete("Use WasapiRecorderBuilder to create a WasapiRecorder instead. WasapiRecorder provides zero-copy buffers, MMCSS thread priority, IAsyncEnumerable capture, and process-specific loopback.")]
    public class WasapiCapture : IWaveIn
    {
        private const long ReftimesPerSec = 10000000;
        private const long ReftimesPerMillisec = 10000;
        private volatile CaptureState captureState;
        private byte[] silenceBuffer;
        private Thread captureThread;
        private AudioClient audioClient;
        private int bytesPerFrame;
        private WaveFormat waveFormat;
        private bool initialized;
        private readonly SynchronizationContext syncContext;
        private readonly bool isUsingEventSync;
        private EventWaitHandle frameEventWaitHandle;
        private readonly int audioBufferMillisecondsLength;

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
        {
            syncContext = SynchronizationContext.Current;
            audioClient = captureDevice.CreateAudioClient();
            ShareMode = AudioClientShareMode.Shared;
            isUsingEventSync = useEventSync;
            this.audioBufferMillisecondsLength = audioBufferMillisecondsLength;

            waveFormat = audioClient.MixFormat;

        }

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

            if ((ShareMode == AudioClientShareMode.Exclusive) && !audioClient.IsFormatSupported(ShareMode, waveFormat))
            {
                throw new ArgumentException("Unsupported Wave Format");
            }

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
            // Silent packets are materialised into this buffer (zero-filled); grown on demand.
            // Non-silent packets bypass this entirely and are handed to consumers as a
            // ReadOnlyMemory<byte> wrapping the native WASAPI buffer.
            silenceBuffer = new byte[bufferFrameCount * bytesPerFrame];

            initialized = true;
        }

        /// <summary>
        /// To allow overrides to specify different flags (e.g. loopback)
        /// </summary>
        protected virtual AudioClientStreamFlags GetAudioClientStreamFlags()
        {
            if (ShareMode == AudioClientShareMode.Shared)
            {
                // enable auto-convert PCM
                return AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality;
            }
            else
            {
                return 0;
            }
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
            captureThread = new Thread(() => CaptureThread(audioClient))
            {
                IsBackground = true,
            };
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
            // Fires one DataAvailable event per WASAPI packet. Non-silent packets are handed out
            // as a ReadOnlyMemory<byte> wrapping the native buffer pointer — no Marshal.Copy into
            // a managed array. The consumer must finish processing before the handler returns;
            // after that the native buffer is released and the Memory is invalid (same aliasing
            // contract as before).
            int packetSize = capture.GetNextPacketSize();
            while (packetSize != 0)
            {
                IntPtr buffer = capture.GetBuffer(out int framesAvailable, out AudioClientBufferFlags flags);
                int bytesAvailable = framesAvailable * bytesPerFrame;

                try
                {
                    if ((flags & AudioClientBufferFlags.Silent) == AudioClientBufferFlags.Silent)
                    {
                        // Materialise silence in a managed buffer. The zero-fill matters because the
                        // buffer is reused across packets and a consumer could have written into
                        // Buffer (it's a byte[]); clear before reuse to guarantee silence semantics.
                        silenceBuffer = BufferHelpers.Ensure(silenceBuffer, bytesAvailable);
                        Array.Clear(silenceBuffer, 0, bytesAvailable);
                        DataAvailable?.Invoke(this, new WaveInEventArgs(silenceBuffer, bytesAvailable));
                    }
                    else
                    {
                        using var memoryManager = new NativeAudioBufferMemoryManager(buffer, bytesAvailable);
                        DataAvailable?.Invoke(this, new WaveInEventArgs(memoryManager.Memory));
                    }
                }
                finally
                {
                    capture.ReleaseBuffer(framesAvailable);
                }

                packetSize = capture.GetNextPacketSize();
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
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
