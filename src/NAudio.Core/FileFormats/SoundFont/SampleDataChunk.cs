using System.IO;

namespace NAudio.SoundFont;

internal class SampleDataChunk
{
    public SampleDataChunk(RiffChunk chunk)
    {
        string header = chunk.ReadChunkID();
        if (header != "sdta")
        {
            throw new InvalidDataException($"Not a sample data chunk ({header})");
        }

        RiffChunk c;
        while ((c = chunk.GetNextSubChunk()) != null)
        {
            switch (c.ChunkID)
            {
                case "smpl":
                    // the upper 16 bits of each (up to) 24-bit sample
                    SampleData = c.GetData();
                    break;
                case "sm24":
                    // optional: the least-significant 8 bits of each 24-bit
                    // sample, one byte per sample, paired with smpl
                    SampleData24 = c.GetData();
                    break;
            }
        }

        SampleData ??= new byte[0];

        // sm24 is only meaningful when it pairs one byte per 16-bit smpl
        // sample; ignore it otherwise (the spec allows a padding byte, so a
        // length of (smpl/2) or (smpl/2)+1 rounded to even is acceptable).
        if (SampleData24 != null && SampleData24.Length < SampleData.Length / 2)
        {
            SampleData24 = null;
        }
    }

    /// <summary>
    /// The 16-bit sample data (the high 16 bits of each sample). For 24-bit
    /// SoundFonts the low 8 bits are in <see cref="SampleData24"/>.
    /// </summary>
    public byte[] SampleData { get; private set; }

    /// <summary>
    /// The optional 24-bit extension data (one byte per sample, the low 8
    /// bits of each sample), or null if the SoundFont is 16-bit.
    /// </summary>
    public byte[] SampleData24 { get; private set; }
}
