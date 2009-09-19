using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// MME Wave function interop
    /// </summary>
    class WaveInterop
    {
        public const int CallbackNull = 0x00000000;    // no callback
        public const int CallbackWindow = 0x00010000;    // dwCallback is a HWND 
        public const int CallbackThread = 0x00020000; // callback is a thread ID 
        public const int CallbackFunction = 0x00030000;    // dwCallback is a FARPROC 
        public const int CallbackEvent = 0x00030000;    // dwCallback is an EVENT handle 

        //public const int TIME_MS = 0x0001;  // time in milliseconds 
        //public const int TIME_SAMPLES = 0x0002;  // number of wave samples 
        //public const int TIME_BYTES = 0x0004;  // current byte offset 


        public enum WaveMessage
        {
            /// <summary>
            /// WIM_OPEN
            /// </summary>
            WaveInOpen = 0x3BE,
            /// <summary>
            /// WIM_CLOSE
            /// </summary>
            WaveInClose = 0x3BF,
            /// <summary>
            /// WIM_DATA
            /// </summary>
            WaveInData = 0x3C0,

            /// <summary>
            /// WOM_CLOSE
            /// </summary>
            WaveOutClose = 0x3BC,
            /// <summary>
            /// WOM_DONE
            /// </summary>
            WaveOutDone = 0x3BD,
            /// <summary>
            /// WOM_OPEN
            /// </summary>
            WaveOutOpen = 0x3BB
        }

        // use the userdata as a reference
        public delegate void WaveCallback(IntPtr hWaveOut, WaveMessage message, Int32 dwUser, WaveHeader wavhdr, int dwReserved);

        [DllImport("winmm.dll")]
        public static extern Int32 mmioStringToFOURCC([MarshalAs(UnmanagedType.LPStr)] String s, int flags);

        [DllImport("winmm.dll")]
        public static extern Int32 waveOutGetNumDevs();
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutPrepareHeader(IntPtr hWaveOut, WaveHeader lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutUnprepareHeader(IntPtr hWaveOut, WaveHeader lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutWrite(IntPtr hWaveOut, WaveHeader lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutOpen(out IntPtr hWaveOut, int uDeviceID, WaveFormat lpFormat, WaveCallback dwCallback, int dwInstance, int dwFlags);
        [DllImport("winmm.dll", EntryPoint = "waveOutOpen")]
        public static extern MmResult waveOutOpenWindow(out IntPtr hWaveOut, int uDeviceID, WaveFormat lpFormat, IntPtr callbackWindowHandle, int dwInstance, int dwFlags);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutReset(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutClose(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutPause(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutRestart(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutGetPosition(IntPtr hWaveOut, out int lpInfo, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutSetVolume(IntPtr hWaveOut, int dwVolume);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutGetVolume(IntPtr hWaveOut, out int dwVolume);
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult waveOutGetDevCaps(int deviceID, out WaveOutCapabilities waveOutCaps, int waveOutCapsSize);

        [DllImport("winmm.dll")]
        public static extern Int32 waveInGetNumDevs();
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult waveInGetDevCaps(int deviceID, out WaveInCapabilities waveInCaps, int waveInCapsSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInAddBuffer(IntPtr hWaveIn, WaveHeader pwh, int cbwh);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInClose(IntPtr hWaveIn);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInOpen(out IntPtr hWaveIn, int uDeviceID, WaveFormat lpFormat, WaveCallback dwCallback, int dwInstance, int dwFlags);
        [DllImport("winmm.dll", EntryPoint = "waveInOpen")]
        public static extern MmResult waveInOpenWindow(out IntPtr hWaveIn, int uDeviceID, WaveFormat lpFormat, IntPtr callbackWindowHandle, int dwInstance, int dwFlags);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInPrepareHeader(IntPtr hWaveIn, WaveHeader lpWaveInHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInUnprepareHeader(IntPtr hWaveIn, WaveHeader lpWaveInHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInReset(IntPtr hWaveIn);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInStart(IntPtr hWaveIn);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInStop(IntPtr hWaveIn);

    }
}
