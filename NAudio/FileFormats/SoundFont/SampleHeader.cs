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
	/// A SoundFont Sample Header
	/// </summary>
	public class SampleHeader 
	{
		/// <summary>
		/// The sample name
		/// </summary>
		public string SampleName;
		/// <summary>
		/// Start offset
		/// </summary>
		public uint Start;
		/// <summary>
		/// End offset
		/// </summary>
		public uint End;
		/// <summary>
		/// Start loop point
		/// </summary>
		public uint StartLoop;
		/// <summary>
		/// End loop point
		/// </summary>
		public uint EndLoop;
		/// <summary>
		/// Sample Rate
		/// </summary>
		public uint SampleRate;
		/// <summary>
		/// Original pitch
		/// </summary>
		public byte OriginalPitch;
		/// <summary>
		/// Pitch correction
		/// </summary>
		public sbyte PitchCorrection;
		/// <summary>
		/// Sample Link
		/// </summary>
		public ushort SampleLink;
		/// <summary>
		/// SoundFont Sample Link Type
		/// </summary>
		public SFSampleLink SFSampleLink;

		/// <summary>
		/// <see cref="Object.ToString"/>
		/// </summary>
		public override string ToString()
		{
			return SampleName;
		}
		
	}
}

