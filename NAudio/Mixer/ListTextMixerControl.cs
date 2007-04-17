// created on 13/12/2002 at 22:06
using System;
using System.Runtime.InteropServices;

namespace NAudio.Mixer
{
	/// <summary>
	/// List text mixer control
	/// </summary>
	public class ListTextMixerControl : MixerControl 
	{
		internal ListTextMixerControl(MixerInterop.MIXERCONTROL mixerControl,int nMixer,int nChannels) 
		{
			this.mixerControl = mixerControl;
			this.nMixer = nMixer;
			this.nChannels = nChannels;
			this.mixerControlDetails = new MixerInterop.MIXERCONTROLDETAILS();
			
			GetControlDetails();

		}

		/// <summary>
		/// Get the details for this control
		/// </summary>
		/// <param name="pDetails">Memory location to read to</param>
		protected override void GetDetails(IntPtr pDetails) 
		{
		}

		// TODO: provide a way of getting / setting data
	}
}
