using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    class WaveInWindow : Form
    {
        private WaveInterop.WaveInCallback waveInCallback;

        public WaveInWindow(WaveInterop.WaveInCallback waveInCallback)
        {
            this.waveInCallback = waveInCallback;
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == (int)WaveInterop.WaveInMessage.Data)
            {
                IntPtr hOutputDevice = m.WParam;
                WaveHeader waveHeader = new WaveHeader();
                Marshal.PtrToStructure(m.LParam, waveHeader);
                waveInCallback(hOutputDevice, WaveInterop.WaveInMessage.Data, 0, waveHeader, 0);
            }
            else if (m.Msg == (int)WaveInterop.WaveInMessage.Open)
            {
                waveInCallback(m.WParam, WaveInterop.WaveInMessage.Open, 0, null, 0);
            }
            else if (m.Msg == (int)WaveInterop.WaveInMessage.Close)
            {
                waveInCallback(m.WParam, WaveInterop.WaveInMessage.Close, 0, null, 0);
            }
            base.WndProc(ref m);
        }
    }
}
