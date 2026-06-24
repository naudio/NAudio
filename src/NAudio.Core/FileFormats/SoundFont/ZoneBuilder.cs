using System;
using System.IO;

namespace NAudio.SoundFont
{
    class ZoneBuilder : StructureBuilder<Zone>
    {
        private Zone lastZone = null;

        public override Zone Read(BinaryReader br)
        {
            Zone z = new Zone();
            z.generatorIndex = br.ReadUInt16();
            z.modulatorIndex = br.ReadUInt16();
            if (lastZone != null)
            {
                lastZone.generatorCount = (ushort)(z.generatorIndex - lastZone.generatorIndex);
                lastZone.modulatorCount = (ushort)(z.modulatorIndex - lastZone.modulatorIndex);
            }
            data.Add(z);
            lastZone = z;
            return z;
        }

        public override void Write(BinaryWriter bw, Zone zone)
        {
            //bw.Write(p.---);
        }

        public void Load(Modulator[] modulators, Generator[] generators)
        {
            // don't do the last zone, which is simply EOZ
            for (int zone = 0; zone < data.Count - 1; zone++)
            {
                Zone z = (Zone)data[zone];
                z.Generators = new Generator[z.generatorCount];
                Array.Copy(generators, z.generatorIndex, z.Generators, 0, z.generatorCount);
                z.Modulators = new Modulator[z.modulatorCount];
                Array.Copy(modulators, z.modulatorIndex, z.Modulators, 0, z.modulatorCount);
            }
            // we can get rid of the EOP record now
            data.RemoveAt(data.Count - 1);
        }

        public Zone[] Zones => data.ToArray();

        public override int Length => 4;
    }
}