using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    class WaveWindowNative : NativeWindow
    {
        private WaveInterop.WaveCallback waveCallback;

        public WaveWindowNative(WaveInterop.WaveCallback waveCallback)
        {
            this.waveCallback = waveCallback;
        }

        protected override void WndProc(ref Message m)
        {
            WaveInterop.WaveMessage message = (WaveInterop.WaveMessage)m.Msg;
            
            switch(message)
            {
                case WaveInterop.WaveMessage.WaveOutDone:
                case WaveInterop.WaveMessage.WaveInData:
                    IntPtr hOutputDevice = m.WParam;
                    WaveHeader waveHeader = new WaveHeader();
                    Marshal.PtrToStructure(m.LParam, waveHeader);
                    waveCallback(hOutputDevice, message, IntPtr.Zero, waveHeader, IntPtr.Zero);
                    break;
                case WaveInterop.WaveMessage.WaveOutOpen:
                case WaveInterop.WaveMessage.WaveOutClose:
                case WaveInterop.WaveMessage.WaveInClose:
                case WaveInterop.WaveMessage.WaveInOpen:
                    waveCallback(m.WParam, message, IntPtr.Zero, null, IntPtr.Zero);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }

    class WaveWindow : Form
    {
        private WaveInterop.WaveCallback waveCallback;

        public WaveWindow(WaveInterop.WaveCallback waveCallback)
        {
            this.waveCallback = waveCallback;
        }

        protected override void WndProc(ref Message m)
        {
            WaveInterop.WaveMessage message = (WaveInterop.WaveMessage)m.Msg;
            
            switch(message)
            {
                case WaveInterop.WaveMessage.WaveOutDone:
                case WaveInterop.WaveMessage.WaveInData:
                    IntPtr hOutputDevice = m.WParam;
                    WaveHeader waveHeader = new WaveHeader();
                    Marshal.PtrToStructure(m.LParam, waveHeader);
                    waveCallback(hOutputDevice, message, IntPtr.Zero, waveHeader, IntPtr.Zero);
                    break;
                case WaveInterop.WaveMessage.WaveOutOpen:
                case WaveInterop.WaveMessage.WaveOutClose:
                case WaveInterop.WaveMessage.WaveInClose:
                case WaveInterop.WaveMessage.WaveInOpen:
                    waveCallback(m.WParam, message, IntPtr.Zero, null, IntPtr.Zero);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }
}
