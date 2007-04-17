// created on 10/12/2002 at 20:37
using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.Mixer 
{
	/// <summary>
	/// Represents a mixer destination
	/// </summary>
	public class MixerDestination 
	{
		private MixerInterop.MIXERLINE mixerLine;
		private int nMixer;
		private int nDestination;
		
		/// <summary>
		/// Creates a new mixer destination
		/// </summary>
		/// <param name="nMixer">Mixer ID</param>
		/// <param name="nDestination">Destination ID</param>
		public MixerDestination(int nMixer, int nDestination) 
		{
			mixerLine = new MixerInterop.MIXERLINE();			
			mixerLine.cbStruct = Marshal.SizeOf(mixerLine);
			mixerLine.dwDestination = nDestination;
			MmException.Try(MixerInterop.mixerGetLineInfo(nMixer, ref mixerLine, MixerInterop.MIXER_GETLINEINFOF_DESTINATION),"mixerGetLineInfo");
			this.nMixer = nMixer;
			this.nDestination = nDestination;
		}
		
		/// <summary>
		/// Mixer destination name
		/// </summary>
		public String Name 
		{
			get 
			{
				return mixerLine.szName;
			}
		}
		
		/// <summary>
		/// Mixer destination short name
		/// </summary>
		public String ShortName 
		{
			get 
			{
				return mixerLine.szShortName;
			}
		}

		/// <summary>
		/// Mixer destination type description
		/// </summary>
		public String TypeDescription 
		{
			get 
			{
				switch(mixerLine.dwComponentType) 
				{
				case MixerInterop.MIXERLINE_COMPONENTTYPE_DST_UNDEFINED:
					return "Undefined";
				case MixerInterop.MIXERLINE_COMPONENTTYPE_DST_DIGITAL:
					return "Digital";
				case MixerInterop.MIXERLINE_COMPONENTTYPE_DST_LINE:
					return "Line Level";
				case MixerInterop.MIXERLINE_COMPONENTTYPE_DST_MONITOR:
					return "Monitor";
				case MixerInterop.MIXERLINE_COMPONENTTYPE_DST_SPEAKERS:
					return "Speakers";
				case MixerInterop.MIXERLINE_COMPONENTTYPE_DST_HEADPHONES:
					return "Headphones";
				case MixerInterop.MIXERLINE_COMPONENTTYPE_DST_TELEPHONE:
					return "Telephone";
				case MixerInterop.MIXERLINE_COMPONENTTYPE_DST_WAVEIN:
					return "Wave Input";
				case MixerInterop.MIXERLINE_COMPONENTTYPE_DST_VOICEIN:
					return "Voice Recognition";
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
		public MixerSource GetSource(int nSource) 
		{
			if(nSource < 0 || nSource >= SourceCount) 
			{
				throw new ArgumentOutOfRangeException("nSource");
			}
			return new MixerSource(nMixer,nDestination,nSource);			
		}

		/// <summary>
		/// Gets the specified control
		/// </summary>
		public MixerControl GetControl(int nControl) 
		{
			if(nControl < 0 || nControl >= SourceCount) 
			{
				throw new ArgumentOutOfRangeException("nControl");
			}			
			return MixerControl.GetMixerControl(nMixer,(int) mixerLine.dwLineID,nControl,Channels);
		}
	}
}

