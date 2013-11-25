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