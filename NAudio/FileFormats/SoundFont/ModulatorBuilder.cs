// ©Mark Heath 2006 (mark@wordandspirit.co.uk)
// You are free to use this code for your own projects.
// Please consider giving credit somewhere in your app to this code if you use it
// Please do not redistribute this code without my permission
// Please get in touch and let me know of any bugs you find, enhancements you would like,
// and apps you have written
using System;
using System.IO;

namespace NAudio.SoundFont {
	class ModulatorBuilder : StructureBuilder {
		public override object Read(BinaryReader br) {
			Modulator m = new Modulator();
			m.SourceModulationData = new ModulatorType(br.ReadUInt16());
			m.DestinationGenerator = (GeneratorEnum) br.ReadUInt16();
			m.Amount = br.ReadInt16();
			m.SourceModulationAmount = new ModulatorType(br.ReadUInt16());
			m.SourceTransform = (TransformEnum) br.ReadUInt16();
			data.Add(m);
			return m;
		}

		public override void Write(BinaryWriter bw,object o) {			
			//Zone z = (Zone) o;
			//bw.Write(p.---);
		}

		public override int Length {
			get {
				return 10;
			}
		}

		public Modulator[] Modulators
		{
			get
			{
				return (Modulator[]) data.ToArray(typeof(Modulator));
			}
		}
	}
}