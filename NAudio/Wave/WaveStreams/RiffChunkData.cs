namespace NAudio.Wave
{
    /// <summary>
    /// Holds information about a RIFF file chunk
    /// </summary>
    public class RiffChunkData
    {
        /// <summary>
        /// Creates a RiffChunk object
        /// </summary>
        public RiffChunkData(int identifier, byte[] data)
        {
            Identifier = identifier;
            Data = data;
        }

        /// <summary>
        /// The chunk identifier
        /// </summary>
        public int Identifier { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public byte[] Data { get; set; }
    }
}