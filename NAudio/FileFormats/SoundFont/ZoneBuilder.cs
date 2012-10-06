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
	class ZoneBuilder : StructureBuilder 
	{
		private Zone lastZone = null;

		public override object Read(BinaryReader br) 
        {
			Zone z = new Zone();
			z.generatorIndex = br.ReadUInt16();
			z.modulatorIndex = br.ReadUInt16();
			if(lastZone != null)
			{
				lastZone.generatorCount = (ushort) (z.generatorIndex - lastZone.generatorIndex);
				lastZone.modulatorCount = (ushort) (z.modulatorIndex - lastZone.modulatorIndex);
			}
			data.Add(z);
			lastZone = z;
			return z;
		}

		public override void Write(BinaryWriter bw,object o) 
        {			
			Zone z = (Zone) o;
			//bw.Write(p.---);
		}

		public void Load(Modulator[] modulators, Generator[] generators)
		{
			// don't do the last zone, which is simply EOZ
			for(int zone = 0; zone < data.Count - 1; zone++)
			{
				Zone z = (Zone) data[zone];
				z.Generators = new Generator[z.generatorCount];
				Array.Copy(generators,z.generatorIndex,z.Generators,0,z.generatorCount);
				z.Modulators = new Modulator[z.modulatorCount];
				Array.Copy(modulators,z.modulatorIndex,z.Modulators,0,z.modulatorCount);
			}
			// we can get rid of the EOP record now
			data.RemoveAt(data.Count - 1);
		}

		public Zone[] Zones
		{
			get
			{
				return (Zone[]) data.ToArray(typeof(Zone));
			}
		}

		public override int Length {
			get {
				return 4;
			}
		}
	}
}