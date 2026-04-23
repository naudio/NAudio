using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.Wasapi
{
    /// <summary>
    /// Modern WASAPI audio recorder with zero-copy buffer access, MMCSS thread priority,
    /// process-specific loopback capture, and IAsyncEnumerable support.
    /// Created via <see cref="WasapiRecorderBuilder"/>.
    /// </summary>
    public class WasapiRecorder : IDisposable, IAsyncDisposable
    {
        private const long ReftimesPerMillisec = 10000;

        private readonly AudioClientShareMode shareMode;
        private readonly bool isUsingEventSync;
        private readonly bool useLoopback;
        private readonly int bufferMilliseconds;
        private readonly string mmcssTaskName;
        private readonly SynchronizationContext syncContext;

        private AudioClient audioClient;
        private int bytesPerFrame;
        private WaveFormat waveFormat;
        private volatile CaptureState captureState;
        private Thread captureThread;
        private EventWaitHandle frameEvent;

        /// <summary>
        /// Fired when captured audio data is available. The buffer span is only valid
        /// for the duration of the callback — copy it if you need to keep it.
        /// </summary>
        public event CaptureDataAvailableHandler DataAvailable;

        /// <summary>
        /// Fired when recording stops, either by request or due to an error.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// The capture format.
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Current capture state.
        /// </summary>
        public CaptureState CaptureState => captureState;

        internal WasapiRecorder(MMDevice device, AudioClientShareMode shareMode, bool useEventSync,
            int bufferMilliseconds, WaveFormat requestedFormat, string mmcssTaskName, bool useLoopback = false)
        {
            syncContext = SynchronizationContext.Current;
            this.shareMode = shareMode;
            isUsingEventSync = useEventSync;
            this.useLoopback = useLoopback;
            this.bufferMilliseconds = bufferMilliseconds;
            this.mmcssTaskName = mmcssTaskName;

            audioClient = device.CreateAudioClient();
            waveFormat = requestedFormat ?? audioClient.MixFormat;
        }

        // Private constructor for process loopback (audioClient created externally via ActivateAudioInterfaceAsync)
        private WasapiRecorder(AudioClient audioClient, bool useEventSync,
            int bufferMilliseconds, WaveFormat requestedFormat, string mmcssTaskName)
        {
            syncContext = SynchronizationContext.Current;
            shareMode = AudioClientShareMode.Shared;
            isUsingEventSync = useEventSync;
            this.bufferMilliseconds = bufferMilliseconds;
            this.mmcssTaskName = mmcssTaskName;
            this.audioClient = audioClient;
            waveFormat = requestedFormat ?? audioClient.MixFormat;
        }

        internal static WasapiRecorder CreateProcessLoopback(uint processId, ProcessLoopbackMode mode,
            bool useEventSync, int bufferMilliseconds, WaveFormat requestedFormat, string mmcssTaskName)
        {
            // Process loopback uses ActivateAudioInterfaceAsync with special activation params.
            // For now, create a placeholder that uses the default render device in loopback mode.
            // Full implementation requires marshaling AudioClientActivationParams to ActivateAudioInterfaceAsync.
            // TODO: implement full process loopback activation via ActivateAudioInterfaceAsync
            throw new NotImplementedException(
                "Process-specific loopback capture requires ActivateAudioInterfaceAsync with " +
                "AUDIOCLIENT_ACTIVATION_PARAMS, which is not yet fully implemented. " +
                "Use WasapiLoopbackCapture for system-wide loopback in the meantime.");
        }

        /// <summary>
        /// Start recording.
        /// </summary>
        public void StartRecording()
        {
            if (captureState != CaptureState.Stopped)
                throw new InvalidOperationException("Already recording");

            captureState = CaptureState.Starting;
            InitializeAudioClient();
            captureThread = new Thread(CaptureThread) { IsBackground = true };
            captureThread.Start();
        }

        /// <summary>
        /// Stop recording.
        /// </summary>
        public void StopRecording()
        {
            if (captureState != CaptureState.Stopped)
                captureState = CaptureState.Stopping;
        }

        /// <summary>
        /// Capture audio as an async enumerable. Each yielded <see cref="AudioBuffer"/> contains
        /// a copy of the captured data (safe to store/process asynchronously).
        /// Use the <see cref="DataAvailable"/> event for zero-copy processing instead.
        /// </summary>
        public async IAsyncEnumerable<AudioBuffer> CaptureAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (captureState != CaptureState.Stopped)
                throw new InvalidOperationException("Already recording");

            captureState = CaptureState.Starting;
            InitializeAudioClient();

            IntPtr mmcssHandle = IntPtr.Zero;
            try
            {
                if (mmcssTaskName != null)
                {
                    uint taskIndex = 0;
                    mmcssHandle = NativeMethods.AvSetMmThreadCharacteristics(mmcssTaskName, ref taskIndex);
                }

                bytesPerFrame = waveFormat.BlockAlign;
                var capture = audioClient.AudioCaptureClient;
                audioClient.Start();
                captureState = CaptureState.Capturing;

                while (!cancellationToken.IsCancellationRequested && captureState == CaptureState.Capturing)
                {
                    // Wait for data
                    if (isUsingEventSync && frameEvent != null)
                    {
                        await Task.Run(() => frameEvent.WaitOne(3 * bufferMilliseconds), cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(bufferMilliseconds / 2, cancellationToken);
                    }

                    // Read all available packets using IntPtr API (ref struct can't cross yield)
                    int packetSize = capture.GetNextPacketSize();
                    while (packetSize > 0)
                    {
                        var bufferPtr = capture.GetBuffer(out var framesRead, out var flags,
                            out var devicePosition, out var qpcPosition);
                        try
                        {
                            if ((flags & AudioClientBufferFlags.Silent) == 0 && framesRead > 0)
                            {
                                int byteCount = framesRead * bytesPerFrame;
                                var data = new byte[byteCount];
                                Marshal.Copy(bufferPtr, data, 0, byteCount);
                                yield return new AudioBuffer(data, flags, devicePosition, qpcPosition);
                            }
                        }
                        finally
                        {
                            capture.ReleaseBuffer(framesRead);
                        }

                        packetSize = capture.GetNextPacketSize();
                    }
                }
            }
            finally
            {
                audioClient.Stop();
                audioClient.Reset();
                captureState = CaptureState.Stopped;
                if (mmcssHandle != IntPtr.Zero)
                    NativeMethods.AvRevertMmThreadCharacteristics(mmcssHandle);
            }
        }

        private void InitializeAudioClient()
        {
            long bufferDuration = bufferMilliseconds * ReftimesPerMillisec;

            var flags = shareMode == AudioClientShareMode.Shared
                ? AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality
                : AudioClientStreamFlags.None;

            if (useLoopback)
                flags |= AudioClientStreamFlags.Loopback;

            if (isUsingEventSync)
                flags |= AudioClientStreamFlags.EventCallback;

            audioClient.Initialize(shareMode, flags, bufferDuration, 0, waveFormat, Guid.Empty);

            if (isUsingEventSync)
            {
                frameEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
                audioClient.SetEventHandle(frameEvent.SafeWaitHandle.DangerousGetHandle());
            }
        }

        private void CaptureThread()
        {
            IntPtr mmcssHandle = IntPtr.Zero;
            Exception exception = null;
            try
            {
                if (mmcssTaskName != null)
                {
                    uint taskIndex = 0;
                    mmcssHandle = NativeMethods.AvSetMmThreadCharacteristics(mmcssTaskName, ref taskIndex);
                }

                bytesPerFrame = waveFormat.BlockAlign;
                var capture = audioClient.AudioCaptureClient;

                audioClient.Start();
                captureState = CaptureState.Capturing;

                int waitMilliseconds = isUsingEventSync
                    ? 3 * bufferMilliseconds
                    : bufferMilliseconds / 2;

                while (captureState == CaptureState.Capturing)
                {
                    if (isUsingEventSync && frameEvent != null)
                        frameEvent.WaitOne(waitMilliseconds, false);
                    else
                        Thread.Sleep(waitMilliseconds);

                    if (captureState != CaptureState.Capturing)
                        break;

                    ReadAvailablePackets(capture);
                }
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                audioClient.Stop();
                audioClient.Reset();
                captureState = CaptureState.Stopped;
                if (mmcssHandle != IntPtr.Zero)
                    NativeMethods.AvRevertMmThreadCharacteristics(mmcssHandle);
                RaiseRecordingStopped(exception);
            }
        }

        private void ReadAvailablePackets(AudioCaptureClient capture)
        {
            int packetSize = capture.GetNextPacketSize();
            while (packetSize > 0)
            {
                using var lease = capture.GetBufferLease(bytesPerFrame);
                DataAvailable?.Invoke(lease.Buffer, lease.Flags);
                packetSize = capture.GetNextPacketSize();
            }
        }

        private void RaiseRecordingStopped(Exception e)
        {
            var handler = RecordingStopped;
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
            StopRecording();
            captureThread?.Join();
            audioClient?.Dispose();
            audioClient = null;
            frameEvent?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Async dispose.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            StopRecording();
            if (captureThread != null)
                await Task.Run(() => captureThread.Join());
            audioClient?.Dispose();
            audioClient = null;
            frameEvent?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Delegate for zero-copy capture data events. The buffer is a <see cref="ReadOnlySpan{T}"/>
    /// directly over the WASAPI buffer — only valid for the duration of the callback.
    /// </summary>
    /// <param name="buffer">The captured audio data. Copy it if you need to keep it.</param>
    /// <param name="flags">Buffer flags from WASAPI (e.g. Silent).</param>
    public delegate void CaptureDataAvailableHandler(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags);

    /// <summary>
    /// A captured audio buffer suitable for async consumption (heap-allocated copy).
    /// </summary>
    public readonly struct AudioBuffer
    {
        /// <summary>
        /// The captured audio data.
        /// </summary>
        public ReadOnlyMemory<byte> Data { get; }

        /// <summary>
        /// Buffer flags from WASAPI (e.g. Silent).
        /// </summary>
        public AudioClientBufferFlags Flags { get; }

        /// <summary>
        /// Device position at time of capture.
        /// </summary>
        public long DevicePosition { get; }

        /// <summary>
        /// QPC position at time of capture (100-nanosecond units).
        /// </summary>
        public long QPCPosition { get; }

        internal AudioBuffer(byte[] data, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
        {
            Data = data;
            Flags = flags;
            DevicePosition = devicePosition;
            QPCPosition = qpcPosition;
        }
    }
}
