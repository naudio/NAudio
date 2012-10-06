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
	/// Builds a SoundFont version
	/// </summary>
	class SFVersionBuilder : StructureBuilder 
	{
		/// <summary>
		/// Reads a SoundFont Version structure
		/// </summary>
		public override object Read(BinaryReader br) 
		{
			SFVersion v = new SFVersion();
			v.Major = br.ReadInt16();
			v.Minor = br.ReadInt16();
			data.Add(v);
			return v;
		}

		/// <summary>
		/// Writes a SoundFont Version structure
		/// </summary>
		public override void Write(BinaryWriter bw,object o) 
		{
			SFVersion v = (SFVersion) o;
			bw.Write(v.Major);
			bw.Write(v.Minor);
		}

		/// <summary>
		/// Gets the length of this structure
		/// </summary>
		public override int Length 
		{
			get 
			{
				return 4;
			}
		}
	}
}