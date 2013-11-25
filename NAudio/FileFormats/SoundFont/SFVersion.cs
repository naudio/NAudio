using System;
using System.IO;

namespace NAudio.SoundFont 
{
	/// <summary>
	/// SoundFont Version Structure
	/// </summary>
	public class SFVersion 
	{
		private short major;
		private short minor;

		/// <summary>
		/// Major Version
		/// </summary>
		public short Major 
		{
			get 
			{
				return major;
			}
			set 
			{
				major = value;			
			}
		}

		/// <summary>
		/// Minor Version
		/// </summary>
		public short Minor 
		{
			get 
			{
				return minor;
			}
			set 
			{
				minor = value;			
			}
		}
	}
}