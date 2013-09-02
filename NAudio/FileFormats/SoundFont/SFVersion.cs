// ©Mark Heath 2006 (mark@wordandspirit.co.uk)
// You are free to use this code for your own projects.
// Please consider giving credit somewhere in your app to this code if you use it
// Please do not redistribute this code without my permission
// Please get in touch and let me know of any bugs you find, enhancements you would like,
// and apps you have written
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