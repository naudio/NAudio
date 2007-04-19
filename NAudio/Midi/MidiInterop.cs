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

        public delegate void MidiInCallback(IntPtr midiInHandle, MidiInMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2);
        public delegate void MidiOutCallback(IntPtr midiInHandle, MidiOutMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2);


		[DllImport("winmm.dll")]
		public static extern MmResult midiConnect(IntPtr hMidiIn,IntPtr hMidiOut, int pReserved);

		[DllImport("winmm.dll")]
        public static extern MmResult midiDisconnect(IntPtr hMidiIn, int hmo, int pReserved);

		[DllImport("winmm.dll")]
        public static extern MmResult midiInAddBuffer(IntPtr hMidiIn, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiInHdr, int uSize);

		[DllImport("winmm.dll")]
        public static extern MmResult midiInClose(IntPtr hMidiIn);

		[DllImport("winmm.dll", CharSet = CharSet.Auto)]
		public static extern MmResult midiInGetDevCaps(int deviceId, out MidiInCapabilities capabilities, int size);

		[DllImport("winmm.dll")]
		public static extern MmResult midiInGetErrorText(int err,string lpText,int uSize);

		[DllImport("winmm.dll")]
        public static extern MmResult midiInGetID(IntPtr hMidiIn, int lpuDeviceId);

		[DllImport("winmm.dll")]
		public static extern int midiInGetNumDevs();

		[DllImport("winmm.dll")]
        public static extern MmResult midiInMessage(IntPtr hMidiIn, int msg, int dw1, int dw2);

		[DllImport("winmm.dll", EntryPoint="midiInOpen")]
        public static extern MmResult midiInOpen(out IntPtr hMidiIn, int uDeviceID, MidiInCallback callback, int dwInstance, int dwFlags);

        [DllImport("winmm.dll", EntryPoint="midiInOpen")]
        public static extern MmResult midiInOpenWindow(out IntPtr hMidiIn, int uDeviceID, IntPtr callbackWindowHandle, int dwInstance, int dwFlags);

		[DllImport("winmm.dll")]
        public static extern MmResult midiInPrepareHeader(IntPtr hMidiIn, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiInHdr, int uSize);

		[DllImport("winmm.dll")]
        public static extern MmResult midiInReset(IntPtr hMidiIn);

		[DllImport("winmm.dll")]
        public static extern MmResult midiInStart(IntPtr hMidiIn);

		[DllImport("winmm.dll")]
        public static extern MmResult midiInStop(IntPtr hMidiIn);

		[DllImport("winmm.dll")]
        public static extern MmResult midiInUnprepareHeader(IntPtr hMidiIn, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiInHdr, int uSize);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutCacheDrumPatches(IntPtr hMidiOut, int uPatch, int lpKeyArray, int uFlags);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutCachePatches(IntPtr hMidiOut, int uBank, int lpPatchArray, int uFlags);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutClose(IntPtr hMidiOut);

		[DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult midiOutGetDevCaps(int deviceNumber, out MidiOutCapabilities caps, int uSize);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutGetErrorText(IntPtr err, string lpText, int uSize);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutGetID(IntPtr hMidiOut, int lpuDeviceID);

		[DllImport("winmm.dll")]
		public static extern int midiOutGetNumDevs();

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutGetVolume(IntPtr uDeviceID, ref int lpdwVolume);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutLongMsg(IntPtr hMidiOut, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiOutHdr, int uSize);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutMessage(IntPtr hMidiOut, int msg, int dw1, int dw2);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutOpen(out IntPtr lphMidiOut, int uDeviceID, MidiOutCallback dwCallback, int dwInstance, int dwFlags);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutPrepareHeader(IntPtr hMidiOut, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiOutHdr, int uSize);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutReset(IntPtr hMidiOut);

		[DllImport("winmm.dll")]
		public static extern MmResult midiOutSetVolume(IntPtr hMidiOut,int dwVolume);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutShortMsg(IntPtr hMidiOut, int dwMsg);

		[DllImport("winmm.dll")]
        public static extern MmResult midiOutUnprepareHeader(IntPtr hMidiOut, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR lpMidiOutHdr, int uSize);

		[DllImport("winmm.dll")]
		public static extern MmResult midiStreamClose(IntPtr hMidiStream);

		[DllImport("winmm.dll")]
        public static extern MmResult midiStreamOpen(out IntPtr hMidiStream, int puDeviceID, int cMidi, int dwCallback, int dwInstance, int fdwOpen);

		[DllImport("winmm.dll")]
        public static extern MmResult midiStreamOut(IntPtr hMidiStream, [MarshalAs(UnmanagedType.Struct)] ref MIDIHDR pmh, int cbmh);

		[DllImport("winmm.dll")]
        public static extern MmResult midiStreamPause(IntPtr hMidiStream);

		[DllImport("winmm.dll")]
        public static extern MmResult midiStreamPosition(IntPtr hMidiStream, [MarshalAs(UnmanagedType.Struct)] ref MMTIME lpmmt, int cbmmt);

		[DllImport("winmm.dll")]
        public static extern MmResult midiStreamProperty(IntPtr hMidiStream, byte lppropdata, int dwProperty);

		[DllImport("winmm.dll")]
        public static extern MmResult midiStreamRestart(IntPtr hMidiStream);

		[DllImport("winmm.dll")]
        public static extern MmResult midiStreamStop(IntPtr hMidiStream);
		
		// TODO: this is general MM interop
		public const int CALLBACK_FUNCTION = 0x30000;
		public const int CALLBACK_NULL = 0;
		
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
			public int  dwDeltaTime;
			public int  dwStreamID;
			public int  dwEvent;
			[MarshalAs(UnmanagedType.ByValArray,SizeConst=1)]
			public int  dwParms;
		}

		[StructLayout(LayoutKind.Sequential)] 
		public struct MIDIHDR 
		{
			public string  lpData;
			public int  dwBufferLength;
			public int  dwBytesRecorded;
			public int  dwUser;
			public int  dwFlags;
			public int  lpNext;
			public int  Reserved;
		}

		[StructLayout(LayoutKind.Sequential)] 
		public struct MIDIPROPTEMPO 
		{
			public int  cbStruct;
			public int  dwTempo;
		}

		
	}
}
