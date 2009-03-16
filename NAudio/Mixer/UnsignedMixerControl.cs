// created on 13/12/2002 at 22:04
using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.Mixer
{
	/// <summary>
	/// Represents an unsigned mixer control
	/// </summary>
	public class UnsignedMixerControl : MixerControl 
	{
		private MixerInterop.MIXERCONTROLDETAILS_UNSIGNED unsignedDetails;
		
		internal UnsignedMixerControl(MixerInterop.MIXERCONTROL mixerControl,IntPtr mixerHandle,int nChannels) 
		{
			this.mixerControl = mixerControl;
            this.mixerHandle = mixerHandle;
			this.nChannels = nChannels;
			this.mixerControlDetails = new MixerInterop.MIXERCONTROLDETAILS();
			GetControlDetails();
		}

		/// <summary>
		/// Gets the details for this control
		/// </summary>
		protected override void GetDetails(IntPtr pDetails) 
		{
			unsignedDetails = (MixerInterop.MIXERCONTROLDETAILS_UNSIGNED) Marshal.PtrToStructure(mixerControlDetails.paDetails,typeof(MixerInterop.MIXERCONTROLDETAILS_UNSIGNED));
		}

		/// <summary>
		/// The control value
		/// </summary>
		public uint Value 
		{
			get 
			{
				GetControlDetails();
				return unsignedDetails.dwValue;
			}
			set 
			{
				unsignedDetails.dwValue = value;
				// TODO: pin
                MmException.Try(MixerInterop.mixerSetControlDetails(mixerHandle, ref mixerControlDetails, MixerFlags.Value | MixerFlags.MixerHandle), "mixerSetControlDetails");
			}
		}
		
		/// <summary>
		/// The control's minimum value
		/// </summary>
		public UInt32 MinValue 
		{
			get 
			{
				return (uint) mixerControl.Bounds.minimum;
			}
		}

		/// <summary>
		/// The control's maximum value
		/// </summary>
		public UInt32 MaxValue 
		{
			get 
			{
				return (uint) mixerControl.Bounds.maximum;
			}
		}

        /// <summary>
        /// Value of the control represented as a percentage
        /// </summary>
        public double Percent
        {
            get
            {
                return 100.0 * (Value - MinValue) / (double)(MaxValue - MinValue);
            }
        }

        /// <summary>
        /// String Representation for debugging purposes
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0} {1}%", base.ToString(), Percent);
        }
	}
}
