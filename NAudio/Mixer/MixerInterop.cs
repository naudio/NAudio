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
        public static extern MmResult mixerOpen(out IntPtr hMixer, int uMxId, IntPtr dwCallback, IntPtr dwInstance, UInt32 dwOpenFlags);
		
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerClose(IntPtr hMixer);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetControlDetails(IntPtr hMixer, ref MIXERCONTROLDETAILS mixerControlDetails, MixerFlags dwDetailsFlags);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetDevCaps(IntPtr nMixerID, ref MIXERCAPS mixerCaps, Int32 mixerCapsSize);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetID(IntPtr hMixer, ref UInt32 mixerID, UInt32 dwMixerIDFlags);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetLineControls(IntPtr hMixer, ref MIXERLINECONTROLS mixerLineControls, MixerFlags dwControlFlags);
		
		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerGetLineInfo(IntPtr hMixer, ref MIXERLINE mixerLine, UInt32 dwInfoFlags);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerMessage(IntPtr hMixer, UInt32 nMessage, UInt32 dwParam1, UInt32 dwParam2);

		[DllImport("winmm.dll", CharSet=CharSet.Ansi)]
        public static extern MmResult mixerSetControlDetails(IntPtr hMixer, ref MIXERCONTROLDETAILS mixerControlDetails, MixerFlags dwDetailsFlags);

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

        /// <summary>
        /// Mixer Line Flags
        /// </summary>
        [Flags]
        public enum MIXERLINE_LINEF
        {
            /// <summary>
            /// Audio line is active. An active line indicates that a signal is probably passing through the line.
            /// </summary>
            MIXERLINE_LINEF_ACTIVE = 1,

            /// <summary>
            /// Audio line is disconnected. A disconnected line's associated controls can still be modified, but the changes have no effect until the line is connected.
            /// </summary>
            MIXERLINE_LINEF_DISCONNECTED = 0x8000,

            /// <summary>
            /// Audio line is an audio source line associated with a single audio destination line. If this flag is not set, this line is an audio destination line associated with zero or more audio source lines.
            /// </summary>
            MIXERLINE_LINEF_SOURCE = (unchecked ((int)0x80000000))
        }

        public enum MIXERLINE_COMPONENTTYPE
        {
            /// <summary>
            /// Audio line is a digital destination (for example, digital input to a DAT or CD audio device).
            /// </summary>
            MIXERLINE_COMPONENTTYPE_DST_DIGITAL = 1,
            /// <summary>
            /// Audio line is an adjustable (gain and/or attenuation) destination intended to drive headphones. Most audio cards use the same audio destination line for speakers and headphones, in which case the mixer device simply uses the MIXERLINE_COMPONENTTYPE_DST_SPEAKERS type.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_DST_HEADPHONES = 5,
            /// <summary>
            /// Audio line is a line level destination (for example, line level input from a CD audio device) that will be the final recording source for the analog-to-digital converter (ADC). Because most audio cards for personal computers provide some sort of gain for the recording audio source line, the mixer device will use the MIXERLINE_COMPONENTTYPE_DST_WAVEIN type.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_DST_LINE = 2,
            /// <summary>
            /// Audio line is a destination used for a monitor.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_DST_MONITOR = 3,
            /// <summary>
            /// Audio line is an adjustable (gain and/or attenuation) destination intended to drive speakers. This is the typical component type for the audio output of audio cards for personal computers.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_DST_SPEAKERS = 4,
            /// <summary>
            /// Audio line is a destination that will be routed to a telephone line.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_DST_TELEPHONE = 6,
            /// <summary>
            /// Audio line is a destination that cannot be defined by one of the standard component types. A mixer device is required to use this component type for line component types that have not been defined by Microsoft Corporation.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_DST_UNDEFINED = 0,
            /// <summary>
            /// Audio line is a destination that will be the final recording source for voice input. This component type is exactly like MIXERLINE_COMPONENTTYPE_DST_WAVEIN but is intended specifically for settings used during voice recording/recognition. Support for this line is optional for a mixer device. Many mixer devices provide only MIXERLINE_COMPONENTTYPE_DST_WAVEIN.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_DST_VOICEIN = 8,
            /// <summary>
            /// Audio line is a destination that will be the final recording source for the waveform-audio input (ADC). This line typically provides some sort of gain or attenuation. This is the typical component type for the recording line of most audio cards for personal computers.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_DST_WAVEIN = 7,
                        
            /// <summary>
            /// Audio line is an analog source (for example, analog output from a video-cassette tape).
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_ANALOG = 0x100A,
            /// <summary>
            /// Audio line is a source originating from the auxiliary audio line. This line type is intended as a source with gain or attenuation that can be routed to the MIXERLINE_COMPONENTTYPE_DST_SPEAKERS destination and/or recorded from the MIXERLINE_COMPONENTTYPE_DST_WAVEIN destination.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_AUXILIARY = 0x1009,
            /// <summary>
            /// Audio line is a source originating from the output of an internal audio CD. This component type is provided for audio cards that provide an audio source line intended to be connected to an audio CD (or CD-ROM playing an audio CD).
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_COMPACTDISC = 0x1005,
            /// <summary>
            /// Audio line is a digital source (for example, digital output from a DAT or audio CD).
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_DIGITAL = 0x1001,
            /// <summary>
            /// Audio line is a line-level source (for example, line-level input from an external stereo) that can be used as an optional recording source. Because most audio cards for personal computers provide some sort of gain for the recording source line, the mixer device will use the MIXERLINE_COMPONENTTYPE_SRC_AUXILIARY type.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_LINE = 0x1002,
            /// <summary>
            /// Audio line is a microphone recording source. Most audio cards for personal computers provide at least two types of recording sources: an auxiliary audio line and microphone input. A microphone audio line typically provides some sort of gain. Audio cards that use a single input for use with a microphone or auxiliary audio line should use the MIXERLINE_COMPONENTTYPE_SRC_MICROPHONE component type.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_MICROPHONE = 0x1003,
            /// <summary>
            /// Audio line is a source originating from personal computer speaker. Several audio cards for personal computers provide the ability to mix what would typically be played on the internal speaker with the output of an audio card. Some audio cards support the ability to use this output as a recording source.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_PCSPEAKER = 0x1007,
            /// <summary>
            /// Audio line is a source originating from the output of an internal synthesizer. Most audio cards for personal computers provide some sort of MIDI synthesizer (for example, an Adlib®-compatible or OPL/3 FM synthesizer).
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_SYNTHESIZER = 0x1004,
            /// <summary>
            /// Audio line is a source originating from an incoming telephone line.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_TELEPHONE = 0x1006,
            /// <summary>
            /// Audio line is a source that cannot be defined by one of the standard component types. A mixer device is required to use this component type for line component types that have not been defined by Microsoft Corporation.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_UNDEFINED = 0x1000,
            /// <summary>
            /// Audio line is a source originating from the waveform-audio output digital-to-analog converter (DAC). Most audio cards for personal computers provide this component type as a source to the MIXERLINE_COMPONENTTYPE_DST_SPEAKERS destination. Some cards also allow this source to be routed to the MIXERLINE_COMPONENTTYPE_DST_WAVEIN destination.
            /// </summary>
            MIXERLINE_COMPONENTTYPE_SRC_WAVEOUT = 0x1008,
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
            public MIXERLINE_COMPONENTTYPE dwComponentType; 
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
