using System;
using System.IO;

namespace NAudio.SoundFont;

internal class ZoneBuilder : StructureBuilder<Zone>
{
    private Zone? lastZone = null;

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
        if (data.Count == 0)
            throw new InvalidDataException("SoundFont has no zones (missing terminal bag record)");
        // don't do the last zone, which is simply EOZ
        for (int zone = 0; zone < data.Count - 1; zone++)
        {
            Zone z = data[zone];
            Zone next = data[zone + 1];
            // as ints: the ushort counts wrap when indices decrease, which only a corrupt file does
            int generatorCount = next.generatorIndex - z.generatorIndex;
            int modulatorCount = next.modulatorIndex - z.modulatorIndex;
            CheckRange(z.generatorIndex, generatorCount, generators.Length, "generator");
            CheckRange(z.modulatorIndex, modulatorCount, modulators.Length, "modulator");
            z.Generators = new Generator[generatorCount];
            Array.Copy(generators, z.generatorIndex, z.Generators, 0, generatorCount);
            z.Modulators = new Modulator[modulatorCount];
            Array.Copy(modulators, z.modulatorIndex, z.Modulators, 0, modulatorCount);
        }
        // we can get rid of the EOP record now
        data.RemoveAt(data.Count - 1);
    }

    public Zone[] Zones => data.ToArray();

    /// <summary>
    /// Checks that a run of <paramref name="count"/> records starting at
    /// <paramref name="start"/>, taken from indices in the file, lies within an array of
    /// <paramref name="available"/> records.
    /// </summary>
    internal static void CheckRange(int start, int count, int available, string what)
    {
        if (count < 0 || start > available || count > available - start)
        {
            throw new InvalidDataException(
                $"SoundFont {what} indices are out of order or out of range (start {start}, count {count}, {available} available)");
        }
    }

    public override int Length => 4;
}
