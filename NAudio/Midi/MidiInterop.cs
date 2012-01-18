using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.Midi
{
    internal class MidiInterop
    {

        public enum MidiInMessage
        {
            /// <summary>
            /// MIM_OPEN
            /// </summary>
            Open = 0x3C1,
            /// <summary>
            /// MIM_CLOSE
            /// </summary>
            Close = 0x3C2,
            /// <summary>
            /// MIM_DATA
            /// </summary>
            Data = 0x3C3,
            /// <summary>
            /// MIM_LONGDATA
            /// </summary>
            LongData = 0x3C4,
            /// <summary>
            /// MIM_ERROR
            /// </summary>
            Error = 0x3C5,
            /// <summary>
            /// MIM_LONGERROR
            /// </summary>
            LongError = 0x3C6,
            /// <summary>
            /// MIM_MOREDATA
            /// </summary>
            MoreData = 0x3CC,
        }



        public enum MidiOutMessage
        {
            /// <summary>
            /// MOM_OPEN
            /// </summary>
            Open = 0x3C7,
            /// <summary>
            /// MOM_CLOSE
            /// </summary>
            Close = 0x3C8,
            /// <summary>
            /// MOM_DONE
            /// </summary>
            Done = 0x3C9
        }

        // http://msdn.microsoft.com/en-us/library/dd798460%28VS.85%29.aspx
        public delegate void MidiInCallback(IntPtr midiInHandle, MidiInMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2);

        // http://msdn.microsoft.com/en-us/library/dd798478%28VS.85%29.aspx
        public delegate void MidiOutCallback(IntPtr midiInHandle, MidiOutMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2);

        // http://msdn.microsoft.com/en-us/library/dd798446%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiConnect(IntPtr hMidiIn, IntPtr hMidiOut, IntPtr pReserved);

        // http://msdn.microsoft.com/en-us/library/dd798447%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiDisconnect(IntPtr hMidiIn, IntPtr hMidiOut, IntPtr pReserved);

        // http://msdn.microsoft.com/en-us/library/dd798450%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInAddBuffer(IntPtr hMidiIn, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiInHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798452%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInClose(IntPtr hMidiIn);

        // http://msdn.microsoft.com/en-us/library/dd798453%28VS.85%29.aspx
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult midiInGetDevCaps(IntPtr deviceId, out MidiInCapabilities capabilities, int size);

        // http://msdn.microsoft.com/en-us/library/dd798454%28VS.85%29.aspx
        // TODO: review this, probably doesn't work
        [DllImport("winmm.dll")]
        public static extern MmResult midiInGetErrorText(int err, string lpText, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798455%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInGetID(IntPtr hMidiIn, out int lpuDeviceId);

        // http://msdn.microsoft.com/en-us/library/dd798456%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern int midiInGetNumDevs();

        // http://msdn.microsoft.com/en-us/library/dd798457%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInMessage(IntPtr hMidiIn, int msg, IntPtr dw1, IntPtr dw2);

        // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx
        [DllImport("winmm.dll", EntryPoint = "midiInOpen")]
        public static extern MmResult midiInOpen(out IntPtr hMidiIn, IntPtr uDeviceID, MidiInCallback callback, IntPtr dwInstance, int dwFlags);

        // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx
        [DllImport("winmm.dll", EntryPoint = "midiInOpen")]
        public static extern MmResult midiInOpenWindow(out IntPtr hMidiIn, IntPtr uDeviceID, IntPtr callbackWindowHandle, IntPtr dwInstance, int dwFlags);

        // http://msdn.microsoft.com/en-us/library/dd798459%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInPrepareHeader(IntPtr hMidiIn, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiInHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798461%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInReset(IntPtr hMidiIn);

        // http://msdn.microsoft.com/en-us/library/dd798462%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInStart(IntPtr hMidiIn);

        // http://msdn.microsoft.com/en-us/library/dd798463%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInStop(IntPtr hMidiIn);

        // http://msdn.microsoft.com/en-us/library/dd798464%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInUnprepareHeader(IntPtr hMidiIn, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiInHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798465%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutCacheDrumPatches(IntPtr hMidiOut, int uPatch, IntPtr lpKeyArray, int uFlags);

        // http://msdn.microsoft.com/en-us/library/dd798466%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutCachePatches(IntPtr hMidiOut, int uBank, IntPtr lpPatchArray, int uFlags);

        // http://msdn.microsoft.com/en-us/library/dd798468%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutClose(IntPtr hMidiOut);

        // http://msdn.microsoft.com/en-us/library/dd798469%28VS.85%29.aspx
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult midiOutGetDevCaps(IntPtr deviceNumber, out MidiOutCapabilities caps, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798470%28VS.85%29.aspx
        // TODO: review, probably doesn't work
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutGetErrorText(IntPtr err, string lpText, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798471%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutGetID(IntPtr hMidiOut, out int lpuDeviceID);

        // http://msdn.microsoft.com/en-us/library/dd798472%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern int midiOutGetNumDevs();

        // http://msdn.microsoft.com/en-us/library/dd798473%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutGetVolume(IntPtr uDeviceID, ref int lpdwVolume);

        // http://msdn.microsoft.com/en-us/library/dd798474%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutLongMsg(IntPtr hMidiOut, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiOutHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798475%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutMessage(IntPtr hMidiOut, int msg, IntPtr dw1, IntPtr dw2);

        // http://msdn.microsoft.com/en-us/library/dd798476%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutOpen(out IntPtr lphMidiOut, IntPtr uDeviceID, MidiOutCallback dwCallback, IntPtr dwInstance, int dwFlags);

        // http://msdn.microsoft.com/en-us/library/dd798477%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutPrepareHeader(IntPtr hMidiOut, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiOutHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798479%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutReset(IntPtr hMidiOut);

        // http://msdn.microsoft.com/en-us/library/dd798480%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutSetVolume(IntPtr hMidiOut, int dwVolume);

        // http://msdn.microsoft.com/en-us/library/dd798481%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutShortMsg(IntPtr hMidiOut, int dwMsg);

        // http://msdn.microsoft.com/en-us/library/dd798482%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutUnprepareHeader(IntPtr hMidiOut, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiOutHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798485%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamClose(IntPtr hMidiStream);

        // http://msdn.microsoft.com/en-us/library/dd798486%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamOpen(out IntPtr hMidiStream, IntPtr puDeviceID, int cMidi, IntPtr dwCallback, IntPtr dwInstance, int fdwOpen);

        // http://msdn.microsoft.com/en-us/library/dd798487%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamOut(IntPtr hMidiStream, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR pmh, int cbmh);

        // http://msdn.microsoft.com/en-us/library/dd798488%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamPause(IntPtr hMidiStream);

        // http://msdn.microsoft.com/en-us/library/dd798489%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamPosition(IntPtr hMidiStream, [MarshalAs(UnmanagedType.Struct)] ref MMTIME lpmmt, int cbmmt);

        // http://msdn.microsoft.com/en-us/library/dd798490%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamProperty(IntPtr hMidiStream, IntPtr lppropdata, int dwProperty);

        // http://msdn.microsoft.com/en-us/library/dd798491%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamRestart(IntPtr hMidiStream);

        // http://msdn.microsoft.com/en-us/library/dd798492%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamStop(IntPtr hMidiStream);

        // TODO: this is general MM interop
        public const int CALLBACK_FUNCTION = 0x30000;
        public const int CALLBACK_NULL = 0;

        // http://msdn.microsoft.com/en-us/library/dd757347%28VS.85%29.aspx
        // TODO: not sure this is right
        [StructLayout(LayoutKind.Sequential)]
        public struct MMTIME
        {
            public int wType;
            public int u;
        }

        // TODO: check for ANSI strings in these structs
        // TODO: check for WORD params
        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIEVENT
        {
            public int dwDeltaTime;
            public int dwStreamID;
            public int dwEvent;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public int dwParms;
        }

        // http://msdn.microsoft.com/en-us/library/dd798449%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIHDR
        {
            public IntPtr lpData; // LPSTR
            public int dwBufferLength; // DWORD
            public int dwBytesRecorded; // DWORD
            public IntPtr dwUser; // DWORD_PTR
            public int dwFlags; // DWORD
            public IntPtr lpNext; // struct mididhdr_tag *
            public IntPtr reserved; // DWORD_PTR
            public int dwOffset; // DWORD
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] 
            public IntPtr[] dwReserved; // DWORD_PTR dwReserved[4]
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIPROPTEMPO
        {
            public int cbStruct;
            public int dwTempo;
        }
    }
}
