using System.IO;

namespace NAudio.SoundFont
{
    class SampleDataChunk
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
                if (c.ChunkID == "smpl")
                {
                    SampleData = c.GetData();
                }
                // sm24 sub-chunk for 24-bit sample data is not currently supported
            }

            if (SampleData == null)
            {
                SampleData = new byte[0];
            }
        }

        public byte[] SampleData { get; private set; }
    }

}