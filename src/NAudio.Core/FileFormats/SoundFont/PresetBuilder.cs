using System;
using System.IO;
using System.Text;

namespace NAudio.SoundFont;

internal class PresetBuilder : StructureBuilder<Preset>
{
    private Preset? lastPreset = null;

    public override Preset Read(BinaryReader br)
    {
        Preset p = new Preset();
        string s = Encoding.UTF8.GetString(br.ReadBytes(20), 0, 20);
        if (s.IndexOf('\0') >= 0)
        {
            s = s.Substring(0, s.IndexOf('\0'));
        }
        p.Name = s;
        p.PatchNumber = br.ReadUInt16();
        p.Bank = br.ReadUInt16();
        p.startPresetZoneIndex = br.ReadUInt16();
        p.library = br.ReadUInt32();
        p.genre = br.ReadUInt32();
        p.morphology = br.ReadUInt32();
        if (lastPreset != null)
            lastPreset.endPresetZoneIndex = (ushort)(p.startPresetZoneIndex - 1);
        data.Add(p);
        lastPreset = p;
        return p;
    }

    public override void Write(BinaryWriter bw, Preset preset)
    {
    }

    public override int Length => 38;

    public void LoadZones(Zone[] presetZones)
    {
        if (data.Count == 0)
            throw new InvalidDataException("SoundFont has no preset headers (missing EOP record)");
        // don't do the last preset, which is simply EOP
        for (int preset = 0; preset < data.Count - 1; preset++)
        {
            Preset p = data[preset];
            // measured from the next header's start as an int: the ushort end index wraps
            // when start indices decrease, which only a corrupt file does
            int start = p.startPresetZoneIndex;
            int count = data[preset + 1].startPresetZoneIndex - start;
            ZoneBuilder.CheckRange(start, count, presetZones.Length, "preset zone");
            p.Zones = new Zone[count];
            Array.Copy(presetZones, start, p.Zones, 0, count);
        }
        // we can get rid of the EOP record now
        data.RemoveAt(data.Count - 1);
    }

    public Preset[] Presets => data.ToArray();
}
