// ©Mark Heath 2006 (mark@wordandspirit.co.uk)
// You are free to use this code for your own projects.
// Please consider giving credit somewhere in your app to this code if you use it
// Please do not redistribute this code without my permission
// Please get in touch and let me know of any bugs you find, enhancements you would like,
// and apps you have written
using System;
using System.IO;
using System.Text;

namespace NAudio.SoundFont
{
	class SampleHeaderBuilder : StructureBuilder 
	{
		public override object Read(BinaryReader br) 
		{
			SampleHeader sh = new SampleHeader();
			string s = Encoding.ASCII.GetString(br.ReadBytes(20));
			if(s.IndexOf('\0') >= 0) 
			{
				s = s.Substring(0,s.IndexOf('\0'));
			}

			sh.SampleName = s;
			sh.Start = br.ReadUInt32();
			sh.End = br.ReadUInt32();
			sh.StartLoop = br.ReadUInt32();
			sh.EndLoop = br.ReadUInt32();
			sh.SampleRate = br.ReadUInt32();
			sh.OriginalPitch = br.ReadByte();
			sh.PitchCorrection = br.ReadSByte();
			sh.SampleLink = br.ReadUInt16();
			sh.SFSampleLink = (SFSampleLink) br.ReadUInt16();
			data.Add(sh);
			return sh;
		}

		public override void Write(BinaryWriter bw,object o) 
		{			
			SampleHeader sh = (SampleHeader) o;
			//bw.Write(p.---);
		}

		public override int Length 
		{
			get 
			{
				return 46;
			}
		}

		internal void RemoveEOS()
		{
			data.RemoveAt(data.Count-1);
		}

		public SampleHeader[] SampleHeaders
		{
			get
			{
				return (SampleHeader[]) data.ToArray(typeof(SampleHeader));
			}
		}
	}
}