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
            SampleData = chunk.GetData();
        }

        public byte[] SampleData { get; private set; }
    }

}