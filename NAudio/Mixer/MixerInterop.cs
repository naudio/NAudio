// created on 09/12/2002 at 21:03
using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

// TODO: add function help from MSDN
// TODO: Create enums for flags parameters
namespace NAudio.Mixer
{



	class MixerInterop 
	{
        public const UInt32 MIXERCONTROL_CONTROLF_UNIFORM   		= 0x00000001;
		public const UInt32 MIXERCONTROL_CONTROLF_MULTIPLE  		= 0x00000002;
		public const UInt32 MIXERCONTROL_CONTROLF_DISABLED  		= 0x80000000;
		
		public const Int32 MAXPNAMELEN = 32;
		public const Int32 MIXER_SHORT_NAME_CHARS = 16;
		public const Int32 MIXER_LONG_NAME_CHARS = 64;

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
		public static extern Int32 mixerGetNumDevs();
		
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerOpen(out IntPtr hMixer, int uMxId, IntPtr dwCallback, IntPtr dwInstance, UInt32 dwOpenFlags);
		
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerClose(IntPtr hMixer);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetControlDetails(IntPtr hMixer, ref MIXERCONTROLDETAILS mixerControlDetails, MixerFlags dwDetailsFlags);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetDevCaps(IntPtr nMixerID, ref MIXERCAPS mixerCaps, Int32 mixerCapsSize);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetID(IntPtr hMixer, out Int32 mixerID, MixerFlags dwMixerIDFlags);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetLineControls(IntPtr hMixer, ref MIXERLINECONTROLS mixerLineControls, MixerFlags dwControlFlags);
		
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetLineInfo(IntPtr hMixer, ref MIXERLINE mixerLine, MixerFlags dwInfoFlags);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerMessage(IntPtr hMixer, UInt32 nMessage, UInt32 dwParam1, UInt32 dwParam2);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerSetControlDetails(IntPtr hMixer, ref MIXERCONTROLDETAILS mixerControlDetails, MixerFlags dwDetailsFlags);

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		public struct MIXERCONTROLDETAILS 
		{
			public Int32 cbStruct; // size of the MIXERCONTROLDETAILS structure
			public Int32 dwControlID; 
			public Int32 cChannels; // Number of channels on which to get or set control properties
			public UInt32 cMultipleItems; // Union with HWND hwndOwner
			public Int32 cbDetails; // Size of the paDetails Member
			public IntPtr paDetails; // LPVOID
		}
		
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MIXERCAPS 
		{ 
			public UInt16 wMid; 
			public UInt16 wPid; 
			public UInt32 vDriverVersion; // MMVERSION - major high byte, minor low byte
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAXPNAMELEN)] 
			public String szPname;
			public UInt32 fdwSupport; 
			public UInt32 cDestinations; 
		} 
		
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MIXERLINECONTROLS 
		{
			public Int32 cbStruct; // size of the MIXERLINECONTROLS structure
			public Int32 dwLineID; // Line identifier for which controls are being queried
			public Int32 dwControlID; // union with UInt32 dwControlType
			public Int32 cControls; 
			public Int32 cbmxctrl; 
			public IntPtr pamxctrl; // see MSDN "Structs Sample"
		}

        /// <summary>
        /// Mixer Line Flags
        /// </summary>
        [Flags]
        public enum MIXERLINE_LINEF
        {
            /// <summary>
            /// Audio line is active. An active line indicates that a signal is probably passing 
            /// through the line.
            /// </summary>
            MIXERLINE_LINEF_ACTIVE = 1,

            /// <summary>
            /// Audio line is disconnected. A disconnected line's associated controls can still be 
            /// modified, but the changes have no effect until the line is connected.
            /// </summary>
            MIXERLINE_LINEF_DISCONNECTED = 0x8000,

            /// <summary>
            /// Audio line is an audio source line associated with a single audio destination line. 
            /// If this flag is not set, this line is an audio destination line associated with zero 
            /// or more audio source lines.
            /// </summary>
            MIXERLINE_LINEF_SOURCE = (unchecked ((int)0x80000000))
        }

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MIXERLINE 
		{
			public Int32 cbStruct; 
			public Int32 dwDestination; 
			public Int32 dwSource; 
			public Int32 dwLineID;
            public MIXERLINE_LINEF fdwLine; 
			public UInt32 dwUser;
            public MixerLineComponentType dwComponentType; 
			public Int32 cChannels; 
			public Int32 cConnections; 
			public Int32 cControls; 
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MIXER_SHORT_NAME_CHARS)] 
			public String szShortName; 
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MIXER_LONG_NAME_CHARS)] 
			public String szName; 
			// start of target struct 'Target'
				public UInt32 dwType; 
				public UInt32 dwDeviceID; 
				public UInt16 wMid; 
				public UInt16 wPid; 
				public UInt32 vDriverVersion; // MMVERSION - major high byte, minor low byte
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAXPNAMELEN)] 
				public String szPname; 
			// end of target struct
		}

        /// <summary>
        /// BOUNDS structure
        /// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct Bounds 
		{
			/// <summary>
			/// dwMinimum / lMinimum / reserved 0
			/// </summary>
			public int minimum;
            /// <summary>
            /// dwMaximum / lMaximum / reserved 1
            /// </summary>
            public int maximum;
            /// <summary>
            /// reserved 2
            /// </summary>
            public int reserved2;
            /// <summary>
            /// reserved 3
            /// </summary>
            public int reserved3;
            /// <summary>
            /// reserved 4
            /// </summary>
            public int reserved4;
            /// <summary>
            /// reserved 5
            /// </summary>
            public int reserved5;
		}

        /// <summary>
        /// METRICS structure
        /// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct Metrics 
		{
			/// <summary>
            /// cSteps / reserved[0]
			/// </summary>
            public int step;
            /// <summary>
            /// cbCustomData / reserved[1], number of bytes for control details
            /// </summary>
            public int customData;
            /// <summary>
            /// reserved 2
            /// </summary>
            public int reserved2;
            /// <summary>
            /// reserved 3
            /// </summary>
            public int reserved3;
            /// <summary>
            /// reserved 4
            /// </summary>
            public int reserved4;
            /// <summary>
            /// reserved 5
            /// </summary>
            public int reserved5;
		}

        /// <summary>
        /// MIXERCONTROL struct
        /// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MIXERCONTROL 
		{ 
			public UInt32 cbStruct;
			public Int32 dwControlID; 
			public MixerControlType dwControlType; 
			public UInt32 fdwControl; 
			public UInt32 cMultipleItems; 
 			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MIXER_SHORT_NAME_CHARS)] 
			public String szShortName; 
 			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MIXER_LONG_NAME_CHARS)] 
			public String szName; 
			public Bounds Bounds;
			public Metrics Metrics;
		}
		
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MIXERCONTROLDETAILS_BOOLEAN 
		{ 
			public Int32 fValue;
		}
		
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MIXERCONTROLDETAILS_SIGNED 
		{ 
			public Int32 lValue;
		}
		
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MIXERCONTROLDETAILS_LISTTEXT 
		{ 
    		public UInt32 dwParam1; 
    		public UInt32 dwParam2; 
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MIXER_LONG_NAME_CHARS)]
    		public String szName; 
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MIXERCONTROLDETAILS_UNSIGNED 
		{ 
			public UInt32 dwValue;
		}
	}
}
