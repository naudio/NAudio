using System;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.Mixer;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Records audio using the winmm <c>waveIn</c> API with CALLBACK_WINDOW, delivering the
    /// <see cref="DataAvailable"/> event on the UI thread's message pump. Useful for apps that
    /// want recording callbacks to arrive on the UI thread without any manual marshaling. For
    /// background-thread capture use <see cref="WaveIn"/> in the NAudio.WinMM package instead.
    /// </summary>
    public class WaveInWindow : IWaveIn
    {
        private readonly SynchronizationContext syncContext;
        private readonly WaveInterop.WaveCallback callback;
        private readonly WaveCallbackHost callbackHost;
        private IntPtr waveInHandle;
        private volatile bool recording;
        private WaveInBuffer[] buffers;
        private int nextBufferIndex;
        private bool isDisposed;

        /// <summary>
        /// Indicates recorded data is available.
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// Creates a <see cref="WaveInWindow"/> that owns a hidden callback window. Must be
        /// constructed on a thread with a running Windows Forms message loop.
        /// </summary>
        public WaveInWindow()
        {
            syncContext = SynchronizationContext.Current
                ?? throw new InvalidOperationException(
                    "WaveInWindow requires a SynchronizationContext. Use WaveIn for background-thread capture.");
            DeviceNumber = 0;
            WaveFormat = new WaveFormat(8000, 16, 1);
            BufferMilliseconds = 100;
            NumberOfBuffers = 3;
            callback = Callback;
            callbackHost = new WaveCallbackHost(callback);
        }

        /// <summary>
        /// Creates a <see cref="WaveInWindow"/> that subclasses an existing window so its HWND
        /// receives waveIn callbacks. The handle must belong to a window whose message loop is
        /// running on the thread that constructs this instance.
        /// </summary>
        public WaveInWindow(IntPtr windowHandle)
        {
            syncContext = SynchronizationContext.Current
                ?? throw new InvalidOperationException(
                    "WaveInWindow requires a SynchronizationContext. Use WaveIn for background-thread capture.");
            DeviceNumber = 0;
            WaveFormat = new WaveFormat(8000, 16, 1);
            BufferMilliseconds = 100;
            NumberOfBuffers = 3;
            callback = Callback;
            callbackHost = new WaveCallbackHost(callback, windowHandle);
        }

        /// <summary>
        /// Returns the number of waveIn devices available on the system.
        /// </summary>
        public static int DeviceCount => WaveInterop.waveInGetNumDevs();

        /// <summary>
        /// Retrieves the capabilities of a waveIn device.
        /// </summary>
        public static WaveInCapabilities GetCapabilities(int devNumber)
        {
            var caps = new WaveInCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveInGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Milliseconds per buffer. Recommended value is 100ms.
        /// </summary>
        public int BufferMilliseconds { get; set; }

        /// <summary>
        /// Number of buffers to use (usually 2 or 3).
        /// </summary>
        public int NumberOfBuffers { get; set; }

        /// <summary>
        /// The device number to use.
        /// </summary>
        public int DeviceNumber { get; set; }

        /// <summary>
        /// Format being recorded.
        /// </summary>
        public WaveFormat WaveFormat { get; set; }

        private void CreateBuffers()
        {
            int bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % WaveFormat.BlockAlign;
            }

            buffers = new WaveInBuffer[NumberOfBuffers];
            for (int n = 0; n < buffers.Length; n++)
            {
                buffers[n] = new WaveInBuffer(waveInHandle, bufferSize);
            }
        }

        private void OpenWaveInDevice()
        {
            CloseWaveInDevice();
            MmResult result = WaveInterop.waveInOpenWindow(
                out waveInHandle,
                (IntPtr)DeviceNumber,
                WaveFormat,
                callbackHost.Handle,
                IntPtr.Zero,
                WaveInterop.WaveInOutOpenFlags.CallbackWindow);
            MmException.Try(result, "waveInOpen");
            CreateBuffers();
        }

        /// <summary>
        /// Start recording.
        /// </summary>
        public void StartRecording()
        {
            if (recording)
            {
                throw new InvalidOperationException("Already recording");
            }
            OpenWaveInDevice();
            nextBufferIndex = 0;
            EnqueueBuffers();
            MmException.Try(WaveInterop.waveInStart(waveInHandle), "waveInStart");
            recording = true;
        }

        private void EnqueueBuffers()
        {
            foreach (var buffer in buffers)
            {
                if (!buffer.InQueue)
                {
                    buffer.Reuse();
                }
            }
        }

        /// <summary>
        /// Stop recording. The driver will return any pending buffers via the window callback
        /// and <see cref="RecordingStopped"/> will be raised on the UI thread once drained.
        /// </summary>
        public void StopRecording()
        {
            if (recording)
            {
                recording = false;
                MmException.Try(WaveInterop.waveInStop(waveInHandle), "waveInStop");
                // Drain any buffers the driver has already filled but not yet delivered,
                // starting from the one we expect next so the order is preserved.
                for (int n = 0; n < buffers.Length; n++)
                {
                    int index = (n + nextBufferIndex) % buffers.Length;
                    var buffer = buffers[index];
                    if (buffer.Done)
                    {
                        RaiseDataAvailable(buffer);
                    }
                }
                RaiseRecordingStopped(null);
            }
        }

        /// <summary>
        /// Current byte position as reported by waveInGetPosition.
        /// </summary>
        public long GetPosition()
        {
            var mmTime = new MmTime { wType = MmTime.TIME_BYTES };
            MmException.Try(WaveInterop.waveInGetPosition(waveInHandle, out mmTime, Marshal.SizeOf(mmTime)), "waveInGetPosition");
            if (mmTime.wType != MmTime.TIME_BYTES)
            {
                throw new InvalidOperationException(
                    $"waveInGetPosition: wType -> Expected {MmTime.TIME_BYTES}, Received {mmTime.wType}");
            }
            return mmTime.cb;
        }

        /// <summary>
        /// Microphone mixer line for this device.
        /// </summary>
        public MixerLine GetMixerLine()
        {
            if (waveInHandle != IntPtr.Zero)
            {
                return new MixerLine(waveInHandle, 0, MixerFlags.WaveInHandle);
            }
            return new MixerLine((IntPtr)DeviceNumber, 0, MixerFlags.WaveIn);
        }

        private void Callback(IntPtr hWaveIn, WaveInterop.WaveMessage message, IntPtr userData, WaveHeader waveHeader, IntPtr reserved)
        {
            if (message != WaveInterop.WaveMessage.WaveInData) return;
            if (!recording) return;
            if (buffers == null) return;

            // Buffers are enqueued in index order and return in the same order — a cyclic index
            // identifies the just-filled buffer without relying on WaveHeader.userData.
            var buffer = buffers[nextBufferIndex];
            nextBufferIndex = (nextBufferIndex + 1) % buffers.Length;

            RaiseDataAvailable(buffer);
            try
            {
                buffer.Reuse();
            }
            catch (Exception e)
            {
                recording = false;
                RaiseRecordingStopped(e);
            }
        }

        private void RaiseDataAvailable(WaveInBuffer buffer)
        {
            DataAvailable?.Invoke(this, new WaveInEventArgs(buffer.Data, buffer.BytesRecorded));
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
                syncContext.Post(_ => handler(this, new StoppedEventArgs(e)), null);
            }
        }

        /// <summary>
        /// Dispose pattern.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            isDisposed = true;

            if (disposing)
            {
                if (recording)
                {
                    StopRecording();
                }
                CloseWaveInDevice();
                callbackHost.Dispose();
            }
        }

        private void CloseWaveInDevice()
        {
            if (waveInHandle == IntPtr.Zero) return;
            WaveInterop.waveInReset(waveInHandle);
            if (buffers != null)
            {
                foreach (var buffer in buffers)
                {
                    buffer.Dispose();
                }
                buffers = null;
            }
            WaveInterop.waveInClose(waveInHandle);
            waveInHandle = IntPtr.Zero;
        }

        /// <summary>
        /// Closes this device.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
