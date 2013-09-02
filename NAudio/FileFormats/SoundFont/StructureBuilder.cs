// ©Mark Heath 2006 (mark@wordandspirit.co.uk)
// You are free to use this code for your own projects.
// Please consider giving credit somewhere in your app to this code if you use it
// Please do not redistribute this code without my permission
// Please get in touch and let me know of any bugs you find, enhancements you would like,
// and apps you have written
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace NAudio.SoundFont 
{

	/// <summary>
	/// base class for structures that can read themselves
	/// </summary>
	internal abstract class StructureBuilder<T>
	{
		protected List<T> data;

		public StructureBuilder()
		{
			Reset();
		}

		public abstract T Read(BinaryReader br);
		public abstract void Write(BinaryWriter bw,T o);
		public abstract int Length { get; }
		
		public void Reset()
		{
			data = new List<T>();
		}
		
		public T[] Data 
		{ 
			get
			{
				return data.ToArray();
			}
		}
	}

}