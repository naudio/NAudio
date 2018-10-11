using System;
using System.IO;

namespace NAudio.SoundFont 
{
	class SampleDataChunk 
	{
		private byte[] sampleData;
        private byte[] sampleData24;

        public SampleDataChunk(RiffChunk chunk) 
		{
			string header = chunk.ReadChunkID();
            if (header != "sdta")
            {
                throw new InvalidDataException(String.Format("Not a sample data chunk ({0})", header));
            }

            var smplChunk = chunk.GetNextSubChunk();
            sampleData = smplChunk.GetData();

            var sm24Chunk = chunk.GetNextSubChunk();
            if (sm24Chunk?.ChunkID == "sm24" && sm24Chunk.ChunkSize == smplChunk.ChunkSize / 2)
            {
                sampleData24 = sm24Chunk.GetData();
            }
            else
            {
                //ignore if next subchunk is not a sm24 chunk or the size is not exactly half of smpl data
            }
		}

        public bool Is24Bit => sampleData24 != null;

        public byte[] SampleData
		{
			get
			{
				return sampleData;
			}
		}

        public byte[] SampleData24bit
        {
            get
            {
                return sampleData24;
            }
        }
    }

} // end of namespace