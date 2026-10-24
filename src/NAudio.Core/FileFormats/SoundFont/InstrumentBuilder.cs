using System;
using System.IO;
using System.Text;

namespace NAudio.SoundFont;

/// <summary>
/// Instrument Builder
/// </summary>
internal class InstrumentBuilder : StructureBuilder<Instrument>
{
    private Instrument? lastInstrument = null;

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
        if (data.Count == 0)
            throw new InvalidDataException("SoundFont has no instrument headers (missing EOI record)");
        // don't do the last preset, which is simply EOP
        for (int instrument = 0; instrument < data.Count - 1; instrument++)
        {
            Instrument i = data[instrument];
            // measured from the next header's start as an int: the ushort end index wraps
            // when start indices decrease, which only a corrupt file does
            int start = i.startInstrumentZoneIndex;
            int count = data[instrument + 1].startInstrumentZoneIndex - start;
            ZoneBuilder.CheckRange(start, count, zones.Length, "instrument zone");
            i.Zones = new Zone[count];
            Array.Copy(zones, start, i.Zones, 0, count);
        }
        // we can get rid of the EOP record now
        data.RemoveAt(data.Count - 1);
    }

    public Instrument[] Instruments => data.ToArray();
}
