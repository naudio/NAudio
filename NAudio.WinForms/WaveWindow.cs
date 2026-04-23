using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Dispatches waveIn/waveOut window messages into a <see cref="WaveInterop.WaveCallback"/> delegate.
    /// Shared between <see cref="WaveWindow"/> and <see cref="WaveWindowNative"/>.
    /// </summary>
    internal static class WaveWindowMessages
    {
        public static bool TryDispatch(ref Message m, WaveInterop.WaveCallback callback)
        {
            var message = (WaveInterop.WaveMessage)m.Msg;
            switch (message)
            {
                case WaveInterop.WaveMessage.WaveOutDone:
                case WaveInterop.WaveMessage.WaveInData:
                    var waveHeader = Marshal.PtrToStructure<WaveHeader>(m.LParam);
                    callback(m.WParam, message, IntPtr.Zero, waveHeader, IntPtr.Zero);
                    return true;
                case WaveInterop.WaveMessage.WaveOutOpen:
                case WaveInterop.WaveMessage.WaveOutClose:
                case WaveInterop.WaveMessage.WaveInOpen:
                case WaveInterop.WaveMessage.WaveInClose:
                    callback(m.WParam, message, IntPtr.Zero, null, IntPtr.Zero);
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// A hidden Form whose HWND is used as the callback window for a waveIn/waveOut device.
    /// </summary>
    internal sealed class WaveWindow : Form
    {
        private readonly WaveInterop.WaveCallback waveCallback;

        public WaveWindow(WaveInterop.WaveCallback waveCallback)
        {
            this.waveCallback = waveCallback;
        }

        protected override void WndProc(ref Message m)
        {
            if (!WaveWindowMessages.TryDispatch(ref m, waveCallback))
            {
                base.WndProc(ref m);
            }
        }
    }

    /// <summary>
    /// Subclasses an existing window so that waveIn/waveOut messages posted to its HWND can be
    /// intercepted and forwarded to a <see cref="WaveInterop.WaveCallback"/>.
    /// </summary>
    internal sealed class WaveWindowNative : NativeWindow
    {
        private readonly WaveInterop.WaveCallback waveCallback;

        public WaveWindowNative(WaveInterop.WaveCallback waveCallback)
        {
            this.waveCallback = waveCallback;
        }

        protected override void WndProc(ref Message m)
        {
            if (!WaveWindowMessages.TryDispatch(ref m, waveCallback))
            {
                base.WndProc(ref m);
            }
        }
    }

    /// <summary>
    /// Manages the lifetime of the callback window used by <see cref="WaveOutWindow"/> and
    /// <see cref="WaveInWindow"/>. Either creates its own hidden window or subclasses an
    /// existing one supplied by the caller.
    /// </summary>
    internal sealed class WaveCallbackHost : IDisposable
    {
        private WaveWindow ownedWindow;
        private WaveWindowNative subclassedWindow;

        /// <summary>
        /// The HWND to pass to waveOutOpen / waveInOpen with CALLBACK_WINDOW.
        /// </summary>
        public IntPtr Handle { get; }

        /// <summary>
        /// True if this host owns and subclasses an application-supplied window handle.
        /// </summary>
        public bool OwnsWindow => ownedWindow != null;

        /// <summary>Creates a new hidden window to receive callbacks.</summary>
        public WaveCallbackHost(WaveInterop.WaveCallback callback)
        {
            ownedWindow = new WaveWindow(callback);
            ownedWindow.CreateControl();
            Handle = ownedWindow.Handle;
        }

        /// <summary>Subclasses an existing window handle to receive callbacks.</summary>
        public WaveCallbackHost(WaveInterop.WaveCallback callback, IntPtr existingWindowHandle)
        {
            if (existingWindowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Window handle cannot be zero", nameof(existingWindowHandle));
            }
            subclassedWindow = new WaveWindowNative(callback);
            subclassedWindow.AssignHandle(existingWindowHandle);
            Handle = existingWindowHandle;
        }

        public void Dispose()
        {
            if (ownedWindow != null)
            {
                ownedWindow.Close();
                ownedWindow.Dispose();
                ownedWindow = null;
            }
            if (subclassedWindow != null)
            {
                subclassedWindow.ReleaseHandle();
                subclassedWindow = null;
            }
        }
    }
}
