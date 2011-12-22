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
	internal class GeneratorBuilder : StructureBuilder 
	{
		public override object Read(BinaryReader br) 
		{
			Generator g = new Generator();
			g.GeneratorType = (GeneratorEnum) br.ReadUInt16();
			g.UInt16Amount = br.ReadUInt16();
			data.Add(g);
			return g;
		}

		public override void Write(BinaryWriter bw,object o) 
		{			
			//Zone z = (Zone) o;
			//bw.Write(p.---);
		}

		public override int Length {
			get {
				return 4;
			}
		}

		public Generator[] Generators
		{
			get
			{
				return (Generator[]) data.ToArray(typeof(Generator));
			}
		}

		public void Load(Instrument[] instruments)
		{
			foreach(Generator g in Generators)
			{
				if(g.GeneratorType == GeneratorEnum.Instrument)
				{
					g.Instrument = instruments[g.UInt16Amount];
				}
			}
		}

		public void Load(SampleHeader[] sampleHeaders)
		{
			foreach(Generator g in Generators)
			{
				if(g.GeneratorType == GeneratorEnum.SampleID)
				{
					g.SampleHeader = sampleHeaders[g.UInt16Amount];
				}
			}
		}
	}
}