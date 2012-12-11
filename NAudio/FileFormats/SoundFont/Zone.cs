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
	/// A SoundFont zone
	/// </summary>
	public class Zone 
	{
		internal ushort generatorIndex;
		internal ushort modulatorIndex;
		internal ushort generatorCount;
		internal ushort modulatorCount;
		private Modulator[] modulators;
		private Generator[] generators;

		/// <summary>
		/// <see cref="Object.ToString"/>
		/// </summary>
		public override string ToString()
		{
			return String.Format("Zone {0} Gens:{1} {2} Mods:{3}",generatorCount,generatorIndex,
				modulatorCount,modulatorIndex);
		}

		/// <summary>
		/// Modulators for this Zone
		/// </summary>
		public Modulator[] Modulators
		{
			get
			{
				return modulators;
			}
			set
			{
				modulators = value;
			}
		}

		/// <summary>
		/// Generators for this Zone
		/// </summary>
		public Generator[] Generators
		{
			get
			{
				return generators;
			}
			set
			{
				generators = value;
			}
		}

	}
}