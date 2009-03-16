// created on 13/12/2002 at 22:01
using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.Mixer 
{
	/// <summary>
	/// Represents a signed mixer control
	/// </summary>
	public class SignedMixerControl : MixerControl 
	{
		private MixerInterop.MIXERCONTROLDETAILS_SIGNED signedDetails;
	
		internal SignedMixerControl(MixerInterop.MIXERCONTROL mixerControl,IntPtr mixerHandle,int nChannels) 
		{
			this.mixerControl = mixerControl;
            this.mixerHandle = mixerHandle;
			this.nChannels = nChannels;
			this.mixerControlDetails = new MixerInterop.MIXERCONTROLDETAILS();
			GetControlDetails();
		}
		
		/// <summary>
		/// Gets details for this contrl
		/// </summary>
		protected override void GetDetails(IntPtr pDetails) 
		{
			signedDetails = (MixerInterop.MIXERCONTROLDETAILS_SIGNED) Marshal.PtrToStructure(mixerControlDetails.paDetails,typeof(MixerInterop.MIXERCONTROLDETAILS_SIGNED));
		}
		
		/// <summary>
		/// The value of the control
		/// </summary>
		public int Value 
		{
			get 
			{
				GetControlDetails();				
				return signedDetails.lValue;
			}
			set 
			{
				//GetControlDetails();
				signedDetails.lValue = value;
				// TODO: pin memory
                MmException.Try(MixerInterop.mixerSetControlDetails(mixerHandle, ref mixerControlDetails, MixerFlags.Value | MixerFlags.MixerHandle), "mixerSetControlDetails");
			}
		}
		
		/// <summary>
		/// Minimum value for this control
		/// </summary>
		public int MinValue 
		{
			get 
			{
				return mixerControl.Bounds.minimum;
			}
		}

		/// <summary>
		/// Maximum value for this control
		/// </summary>
		public int MaxValue 
		{
			get 
			{
				return mixerControl.Bounds.maximum;
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
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1}%", base.ToString(), Percent);
        }
	}
}
