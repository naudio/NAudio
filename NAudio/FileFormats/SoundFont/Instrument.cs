// ©Mark Heath 2006 (mark@wordandspirit.co.uk)
// You are free to use this code for your own projects.
// Please consider giving credit somewhere in your app to this code if you use it
// Please do not redistribute this code without my permission
// Please get in touch and let me know of any bugs you find, enhancements you would like,
// and apps you have written
using System;

namespace NAudio.SoundFont 
{
	/// <summary>
	/// SoundFont instrument
	/// </summary>
	public class Instrument 
	{
		private string name;
		internal ushort startInstrumentZoneIndex;
		internal ushort endInstrumentZoneIndex;
		private Zone[] zones;
		
		/// <summary>
		/// instrument name
		/// </summary>
		public string Name 
		{
			get 
			{
				return name;
			}
			set 
			{
				// TODO: validate
				name = value;
			}
		}

		/// <summary>
		/// Zones
		/// </summary>
		public Zone[] Zones
		{
			get
			{
				return zones;
			}
			set
			{
				zones = value;
			}
		}

		/// <summary>
		/// <see cref="Object.ToString"/>
		/// </summary>
		public override string ToString() 
		{
			return this.name;
		}
	}
}