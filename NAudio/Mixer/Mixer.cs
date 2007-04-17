using System;
using System.Runtime.InteropServices;

namespace NAudio.Mixer
{
	/// <summary>Represents a Windows mixer device</summary>
	public class Mixer 
	{
		private MixerInterop.MIXERCAPS caps;
		private int nMixer;
		
		/// <summary>The number of mixer devices available</summary>	
		public static int NumberOfDevices 
		{
			get 
			{
				return MixerInterop.mixerGetNumDevs();
			}
		}
		
		/// <summary>Connects to the specified mixer</summary>
		/// <param name="mixerID">The ID of the mixer to use. 
		/// This should be between zero and NumberOfDevices - 1</param>
		public Mixer(int mixerID) 
		{
			if(mixerID < 0 || mixerID >= NumberOfDevices) 
			{
				throw new ArgumentOutOfRangeException("mixerID");
			}
			caps = new MixerInterop.MIXERCAPS();
			MmException.Try(MixerInterop.mixerGetDevCaps(mixerID,ref caps,Marshal.SizeOf(caps)),"mixerGetDevCaps");
			nMixer = mixerID;
			
			// TODO: optionally support really opening the mixer device
		}

		/// <summary>The number of destinations this mixer supports</summary>
		public int DestinationCount 
		{
			get 
			{
				return (int) caps.cDestinations;
			}
		}
		
		/// <summary>The name of this mixer device</summary>
		public String Name 
		{
			get 
			{
				return caps.szPname;
			}
		}
		
		/// <summary>The manufacturer code for this mixer device</summary>
		public Manufacturers Manufacturer 
		{
			get 
			{
				return (Manufacturers) caps.wMid;
			}
		}

		/// <summary>The product identifier code for this mixer device</summary>
		public int ProductID 
		{
			get 
			{
				return caps.wPid;
			}
		}
		
		/// <summary>Retrieve the specified MixerDestination object</summary>
		/// <param name="destination">The ID of the destination to use.
		/// Should be between 0 and DestinationCount - 1</param>
		public MixerDestination GetDestination(int destination) 
		{
			if(destination < 0 || destination >= DestinationCount) 
			{
				throw new ArgumentOutOfRangeException("destination");
			}
			return new MixerDestination(nMixer,destination);
		}
	}
}
