// created on 10/12/2002 at 20:37
using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.Mixer 
{
	/// <summary>
	/// Represents a mixer line (source or destination)
	/// </summary>
	public class MixerLine 
	{
		private MixerInterop.MIXERLINE mixerLine;
        private IntPtr mixerHandle;
		private int nDestination;
        private int nSource;
		
		/// <summary>
		/// Creates a new mixer destination
		/// </summary>
        /// <param name="mixerHandle">Mixer Handle</param>
		/// <param name="nDestination">Destination ID</param>
        public MixerLine(IntPtr mixerHandle, int nDestination) 
		{
			mixerLine = new MixerInterop.MIXERLINE();			
			mixerLine.cbStruct = Marshal.SizeOf(mixerLine);
			mixerLine.dwDestination = nDestination;
            MmException.Try(MixerInterop.mixerGetLineInfo(mixerHandle, ref mixerLine, MixerInterop.MIXER_GETLINEINFOF_DESTINATION), "mixerGetLineInfo");
            this.mixerHandle = mixerHandle;
			this.nDestination = nDestination;            
		}

        /// <summary>
		/// Creates a new Mixer Source
		/// </summary>
        /// <param name="mixerHandle">Mixer Handle</param>
		/// <param name="nDestination">Destination ID</param>
		/// <param name="nSource">Source ID</param>
        public MixerLine(IntPtr mixerHandle, int nDestination, int nSource) 
		{
			mixerLine = new MixerInterop.MIXERLINE();
			mixerLine.cbStruct = Marshal.SizeOf(mixerLine);
			mixerLine.dwDestination = nDestination;
			mixerLine.dwSource = nSource;
            MmException.Try(MixerInterop.mixerGetLineInfo(mixerHandle, ref mixerLine, MixerInterop.MIXER_GETLINEINFOF_SOURCE), "mixerGetLineInfo");
            this.mixerHandle = mixerHandle;
			this.nDestination = nDestination;
			this.nSource = nSource;
		}
		
		/// <summary>
		/// Mixer Line Name
		/// </summary>
		public String Name 
		{
			get 
			{
				return mixerLine.szName;
			}
		}
		
		/// <summary>
		/// Mixer Line short name
		/// </summary>
		public String ShortName 
		{
			get 
			{
				return mixerLine.szShortName;
			}
		}

        /// <summary>
        /// The line ID
        /// </summary>
        public int LineId
        {
            get
            {
                return mixerLine.dwLineID;
            }
        }

		/// <summary>
		/// Mixer destination type description
		/// </summary>
		public String TypeDescription 
		{
			get 
			{
                switch (mixerLine.dwComponentType)
                {
                    // destinations
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_DST_UNDEFINED:
                        return "Undefined";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_DST_DIGITAL:
                        return "Digital";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_DST_LINE:
                        return "Line Level";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_DST_MONITOR:
                        return "Monitor";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_DST_SPEAKERS:
                        return "Speakers";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_DST_HEADPHONES:
                        return "Headphones";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_DST_TELEPHONE:
                        return "Telephone";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_DST_WAVEIN:
                        return "Wave Input";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_DST_VOICEIN:
                        return "Voice Recognition";
                    // sources
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_UNDEFINED:
                        return "Undefined";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_DIGITAL:
                        return "Digital";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_LINE:
                        return "Line Level";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_MICROPHONE:
                        return "Microphone";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_SYNTHESIZER:
                        return "Synthesizer";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_COMPACTDISC:
                        return "Compact Disk";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_TELEPHONE:
                        return "Telephone";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_PCSPEAKER:
                        return "PC Speaker";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_WAVEOUT:
                        return "Wave Out";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_AUXILIARY:
                        return "Auxiliary";
                    case MixerInterop.MIXERLINE_COMPONENTTYPE.MIXERLINE_COMPONENTTYPE_SRC_ANALOG:
                        return "Analog";
                    default:
                        return "Invalid";
                }
			}				
		}
		
		/// <summary>
		/// Number of channels
		/// </summary>
		public int Channels 
		{
			get 
			{
				return mixerLine.cChannels;
			}
		}
		
		/// <summary>
		/// Number of sources
		/// </summary>
		public int SourceCount 
		{
			get 
			{
				return mixerLine.cConnections;
			}
		}
		
		/// <summary>
		/// Number of controls
		/// </summary>
		public int ControlsCount 
		{
			get 
			{
				return mixerLine.cControls;
			}
		}
		
		/// <summary>
		/// Gets the specified source
		/// </summary>
		public MixerLine GetSource(int nSource) 
		{
			if(nSource < 0 || nSource >= SourceCount) 
			{
				throw new ArgumentOutOfRangeException("nSource");
			}
            return new MixerLine(mixerHandle, nDestination, nSource);			
		}

		/// <summary>
		/// Gets the specified control
		/// </summary>
		public MixerControl GetControl(int controlIndex) 
		{
			if(controlIndex < 0 || controlIndex >= ControlsCount) 
			{
                throw new ArgumentOutOfRangeException("controlIndex");
			}
            return MixerControl.GetMixerControl(mixerHandle, mixerLine.dwLineID, controlIndex+1, Channels);
		}

        /// <summary>
        /// Describes this Mixer Line (for diagnostic purposes)
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0} {1} ({2} controls, ID={3})", 
                Name, TypeDescription, ControlsCount, mixerLine.dwLineID);
        }
	}
}

