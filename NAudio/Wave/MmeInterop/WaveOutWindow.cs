using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    class WaveOutWindowNative : System.Windows.Forms.NativeWindow
    {
        private WaveInterop.WaveOutCallback waveOutCallback;

        public WaveOutWindowNative(WaveInterop.WaveOutCallback waveOutCallback)
        {
            this.waveOutCallback = waveOutCallback;
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == (int)WaveInterop.WaveOutMessage.Done)
            {
                IntPtr hOutputDevice = m.WParam;
                WaveHeader waveHeader = new WaveHeader();
                Marshal.PtrToStructure(m.LParam, waveHeader);

                waveOutCallback(hOutputDevice, WaveInterop.WaveOutMessage.Done, 0, waveHeader, 0);
            }
            else if (m.Msg == (int)WaveInterop.WaveOutMessage.Open)
            {
                waveOutCallback(m.WParam, WaveInterop.WaveOutMessage.Open, 0, null, 0);
            }
            else if (m.Msg == (int)WaveInterop.WaveOutMessage.Close)
            {
                waveOutCallback(m.WParam, WaveInterop.WaveOutMessage.Close, 0, null, 0);
            }
            else
            {
                base.WndProc(ref m);
            }
        }
    }

    class WaveOutWindow : Form
    {
        private WaveInterop.WaveOutCallback waveOutCallback;

        public WaveOutWindow(WaveInterop.WaveOutCallback waveOutCallback)
        {
            this.waveOutCallback = waveOutCallback;
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == (int)WaveInterop.WaveOutMessage.Done)
            {
                IntPtr hOutputDevice = m.WParam;
                WaveHeader waveHeader = new WaveHeader();
                Marshal.PtrToStructure(m.LParam, waveHeader);

                waveOutCallback(hOutputDevice, WaveInterop.WaveOutMessage.Done, 0, waveHeader, 0);
            }
            else if (m.Msg == (int)WaveInterop.WaveOutMessage.Open)
            {
                waveOutCallback(m.WParam, WaveInterop.WaveOutMessage.Open, 0, null, 0);
            }
            else if (m.Msg == (int)WaveInterop.WaveOutMessage.Close)
            {
                waveOutCallback(m.WParam, WaveInterop.WaveOutMessage.Close, 0, null, 0);
            }
            else
            {
                base.WndProc(ref m);
            }
        }
    }
}
