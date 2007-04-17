// created on 09/12/2002 at 21:03
using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

// TODO: add function help from MSDN
// TODO: Create enums for flags parameters
namespace NAudio.Mixer
{
    [Flags]
    enum MixerFlags
    {

        #region Objects
        /// <summary>
        /// MIXER_OBJECTF_HANDLE 	= 0x80000000;
        /// </summary>
        Handle = unchecked ( (int) 0x80000000 ),
        /// <summary>
        /// MIXER_OBJECTF_MIXER 	= 0x00000000;
        /// </summary>
        Mixer = 0,
        /// <summary>
        /// MIXER_OBJECTF_HMIXER
        /// </summary>
		MixerHandle = Mixer | Handle,
        /// <summary>
        /// MIXER_OBJECTF_WAVEOUT
        /// </summary>
        WaveOut = 0x10000000,
        /// <summary>
        /// MIXER_OBJECTF_HWAVEOUT
        /// </summary>
        WaveOutHandle = WaveOut | Handle,
        /// <summary>
        /// MIXER_OBJECTF_WAVEIN
        /// </summary>
        WaveIn = 0x20000000,
        /// <summary>
        /// MIXER_OBJECTF_HWAVEIN
        /// </summary>
        WaveInHandle = WaveIn | Handle,
        /// <summary>
        /// MIXER_OBJECTF_MIDIOUT
        /// </summary>
        MidiOut = 0x30000000,
        /// <summary>
        /// MIXER_OBJECTF_HMIDIOUT
        /// </summary>
        MidiOutHandle = MidiOut | Handle,
        /// <summary>
        /// MIXER_OBJECTF_MIDIIN
        /// </summary>
        MidiIn = 0x40000000,
        /// <summary>
        /// MIXER_OBJECTF_HMIDIIN
        /// </summary>
        MidiInHandle = MidiIn | Handle,
        /// <summary>
        /// MIXER_OBJECTF_AUX
        /// </summary>
		Aux = 0x50000000,
        #endregion

        #region Get/Set control details
        /// <summary>
        /// MIXER_GETCONTROLDETAILSF_VALUE      	= 0x00000000;
        /// MIXER_SETCONTROLDETAILSF_VALUE      	= 0x00000000;
        /// </summary>
        Value = 0,
        /// <summary>
        /// MIXER_GETCONTROLDETAILSF_LISTTEXT   	= 0x00000001;
        /// MIXER_SETCONTROLDETAILSF_LISTTEXT   	= 0x00000001;
        /// </summary>
        ListText = 1,
        /// <summary>
        /// MIXER_GETCONTROLDETAILSF_QUERYMASK  	= 0x0000000F;
        /// MIXER_SETCONTROLDETAILSF_QUERYMASK  	= 0x0000000F;
        /// MIXER_GETLINECONTROLSF_QUERYMASK    	= 0x0000000F;
        /// </summary>
        QueryMask = 0xF,
        #endregion

        #region get line controls
        /// <summary>
        /// MIXER_GETLINECONTROLSF_ALL          	= 0x00000000;
        /// </summary>
        All = 0,
        /// <summary>
        /// MIXER_GETLINECONTROLSF_ONEBYID      	= 0x00000001;
        /// </summary>
		OneById = 1,
        /// <summary>
        /// MIXER_GETLINECONTROLSF_ONEBYTYPE    	= 0x00000002;
        /// </summary>
		OneByType = 2,		
        #endregion

    }


	class MixerInterop 
	{
	


		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_FIRST       = 0x00000000;
		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_UNDEFINED   = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 0);
		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_DIGITAL     = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 1);
		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_LINE        = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 2);
		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_MONITOR     = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 3);
		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_SPEAKERS    = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 4);
		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_HEADPHONES  = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 5);
		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_TELEPHONE   = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 6);
		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_WAVEIN      = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 7);
		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_VOICEIN     = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 8);
		public const UInt32 MIXERLINE_COMPONENTTYPE_DST_LAST        = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 8);
		
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_FIRST       = 0x00001000;
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_UNDEFINED   = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 0);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_DIGITAL     = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 1);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_LINE        = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 2);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_MICROPHONE  = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 3);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_SYNTHESIZER = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 4);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_COMPACTDISC = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 5);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_TELEPHONE   = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 6);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_PCSPEAKER   = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 7);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_WAVEOUT     = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 8);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_AUXILIARY   = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 9);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_ANALOG      = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 10);
		public const UInt32 MIXERLINE_COMPONENTTYPE_SRC_LAST        = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 10);
		



		public const UInt32 MIXER_GETLINEINFOF_DESTINATION      	= 0x00000000;
		public const UInt32 MIXER_GETLINEINFOF_SOURCE           	= 0x00000001;
		public const UInt32 MIXER_GETLINEINFOF_LINEID           	= 0x00000002;
		public const UInt32 MIXER_GETLINEINFOF_COMPONENTTYPE    	= 0x00000003;
		public const UInt32 MIXER_GETLINEINFOF_TARGETTYPE       	= 0x00000004;
		public const UInt32 MIXER_GETLINEINFOF_QUERYMASK        	= 0x0000000F;



		public const UInt32 MIXERCONTROL_CONTROLF_UNIFORM   		= 0x00000001;
		public const UInt32 MIXERCONTROL_CONTROLF_MULTIPLE  		= 0x00000002;
		public const UInt32 MIXERCONTROL_CONTROLF_DISABLED  		= 0x80000000;


		
		public const Int32 MAXPNAMELEN = 32;
		public const Int32 MIXER_SHORT_NAME_CHARS = 16;
		public const Int32 MIXER_LONG_NAME_CHARS = 64;

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
		public static extern Int32 mixerGetNumDevs();
		
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
		public static extern MmResult mixerOpen(ref Int32 hMixer, UInt32 dwCallback, UInt32 dwInstance, UInt32 dwOpenFlags);
		
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
		public static extern MmResult mixerClose(UInt32 hMixer);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetControlDetails(Int32 hMixer, ref MIXERCONTROLDETAILS mixerControlDetails, MixerFlags dwDetailsFlags);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
		public static extern MmResult mixerGetDevCaps(Int32 nMixerID, ref MIXERCAPS mixerCaps, Int32 mixerCapsSize);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
		public static extern MmResult mixerGetID(Int32 hMixer, ref UInt32 mixerID, UInt32 dwMixerIDFlags);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
		public static extern MmResult mixerGetLineControls(Int32 hMixer, ref MIXERLINECONTROLS mixerLineControls, MixerFlags dwControlFlags);
		
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
		public static extern MmResult mixerGetLineInfo(Int32 hMixer, ref MIXERLINE mixerLine, UInt32 dwInfoFlags);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
		public static extern MmResult mixerMessage(Int32 hMixer, UInt32 nMessage, UInt32 dwParam1, UInt32 dwParam2);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
		public static extern MmResult mixerSetControlDetails(Int32 hMixer, ref MIXERCONTROLDETAILS mixerControlDetails, MixerFlags dwDetailsFlags);

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		public struct MIXERCONTROLDETAILS 
		{
			public Int32 cbStruct; // size of the MIXERCONTROLDETAILS structure
			public UInt32 dwControlID; 
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
			public UInt32 cControls; 
			public Int32 cbmxctrl; 
			public IntPtr pamxctrl; // see MSDN "Structs Sample"
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public struct MIXERLINE 
		{
			public Int32 cbStruct; 
			public Int32 dwDestination; 
			public Int32 dwSource; 
			public UInt32 dwLineID; 
			public UInt32 fdwLine; 
			public UInt32 dwUser; 
			public UInt32 dwComponentType; 
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
			public UInt32 dwControlID; 
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
