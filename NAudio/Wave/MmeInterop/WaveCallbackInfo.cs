using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// Wave Callback Info
    /// </summary>
    public class WaveCallbackInfo
    {
        /// <summary>
        /// Callback Strategy
        /// </summary>
        public WaveCallbackStrategy Strategy { get; private set; }
        /// <summary>
        /// Window Handle (if applicable)
        /// </summary>
        public IntPtr Handle { get; private set; }

        private WaveWindow waveOutWindow;
        private WaveWindowNative waveOutWindowNative;

        /// <summary>
        /// Sets up a new WaveCallbackInfo for function callbacks
        /// </summary>
        public static WaveCallbackInfo FunctionCallback()
        {
            return new WaveCallbackInfo(WaveCallbackStrategy.FunctionCallback, IntPtr.Zero);
        }

        /// <summary>
        /// Sets up a new WaveCallbackInfo to use a New Window
        /// IMPORTANT: only use this on the GUI thread
        /// </summary>
        public static WaveCallbackInfo NewWindow()
        {
            return new WaveCallbackInfo(WaveCallbackStrategy.NewWindow, IntPtr.Zero);
        }

        /// <summary>
        /// Sets up a new WaveCallbackInfo to use an existing window
        /// IMPORTANT: only use this on the GUI thread
        /// </summary>
        public static WaveCallbackInfo ExistingWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Handle cannot be zero");
            }
            return new WaveCallbackInfo(WaveCallbackStrategy.ExistingWindow, handle);
        }

        private WaveCallbackInfo(WaveCallbackStrategy strategy, IntPtr handle)
        {
            this.Strategy = strategy;
            this.Handle = handle;
        }

        internal void Connect(WaveInterop.WaveCallback callback)
        {
            if (Strategy == WaveCallbackStrategy.NewWindow)
            {
                waveOutWindow = new WaveWindow(callback);
                waveOutWindow.CreateControl();
                this.Handle = waveOutWindow.Handle;
            }
            else if (Strategy == WaveCallbackStrategy.ExistingWindow)
            {
                waveOutWindowNative = new WaveWindowNative(callback);
                waveOutWindowNative.AssignHandle(this.Handle);
            }
        }

        internal MmResult WaveOutOpen(out IntPtr waveOutHandle, int deviceNumber, WaveFormat waveFormat, WaveInterop.WaveCallback callback)
        {
            MmResult result;
            if (Strategy == WaveCallbackStrategy.FunctionCallback)
            {
                result = WaveInterop.waveOutOpen(out waveOutHandle, deviceNumber, waveFormat, callback, 0, WaveInterop.CallbackFunction);
            }
            else
            {
                result = WaveInterop.waveOutOpenWindow(out waveOutHandle, deviceNumber, waveFormat, this.Handle, 0, WaveInterop.CallbackWindow);
            }
            return result;
        }

        internal MmResult WaveInOpen(out IntPtr waveInHandle, int deviceNumber, WaveFormat waveFormat, WaveInterop.WaveCallback callback)
        {
            MmResult result;
            if (Strategy == WaveCallbackStrategy.FunctionCallback)
            {        
                result = WaveInterop.waveInOpen(out waveInHandle, deviceNumber, waveFormat, callback, 0, WaveInterop.CallbackFunction);
            }
            else
            {
                result = WaveInterop.waveInOpenWindow(out waveInHandle, deviceNumber, waveFormat, this.Handle, 0, WaveInterop.CallbackWindow);
            }
            return result;
        }

        internal void Disconnect()
        {
            if (waveOutWindow != null)
            {
                waveOutWindow.Close();
                waveOutWindow = null;
            }
            if (waveOutWindowNative != null)
            {
                waveOutWindowNative.ReleaseHandle();
                waveOutWindowNative = null;
            }
        }
    }
}
