using System;
using System.IO;
using System.Text;

namespace NAudio.SoundFont
{
    /// <summary>
    /// Instrument Builder
    /// </summary>
    internal class InstrumentBuilder : StructureBuilder<Instrument>
    {
        private Instrument lastInstrument = null;

        public override Instrument Read(BinaryReader br)
        {
            Instrument i = new Instrument();
            string s = Encoding.UTF8.GetString(br.ReadBytes(20), 0, 20);
            if (s.IndexOf('\0') >= 0)
            {
                s = s.Substring(0, s.IndexOf('\0'));
            }
            i.Name = s;
            i.startInstrumentZoneIndex = br.ReadUInt16();
            if (lastInstrument != null)
            {
                lastInstrument.endInstrumentZoneIndex = (ushort)(i.startInstrumentZoneIndex - 1);
            }
            data.Add(i);
            lastInstrument = i;
            return i;
        }

        public override void Write(BinaryWriter bw, Instrument instrument)
        {
        }

        public override int Length => 22;

        public void LoadZones(Zone[] zones)
        {
            // don't do the last preset, which is simply EOP
            for (int instrument = 0; instrument < data.Count - 1; instrument++)
            {
                Instrument i = data[instrument];
                i.Zones = new Zone[i.endInstrumentZoneIndex - i.startInstrumentZoneIndex + 1];
                Array.Copy(zones, i.startInstrumentZoneIndex, i.Zones, 0, i.Zones.Length);
            }
            // we can get rid of the EOP record now
            data.RemoveAt(data.Count - 1);
        }

        public Instrument[] Instruments => data.ToArray();
    }
}