using System;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// https://tech.ebu.ch/docs/tech/tech3285.pdf
    /// </summary>
    public class BextChunkInfo
    {
        /// <summary>
        /// Constructs a new BextChunkInfo
        /// </summary>
        public BextChunkInfo()
        {
            //UniqueMaterialIdentifier = Guid.NewGuid().ToString(); 
            Reserved = new byte[190];
        }

        /// <summary>
        /// Description (max 256 chars)
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Originator (max 32 chars)
        /// </summary>
        public string Originator { get; set; }
        /// <summary>
        /// Originator Reference (max 32 chars)
        /// </summary>
        public string OriginatorReference { get; set; } 
        /// <summary>
        /// Originator Date Time
        /// </summary>
        public DateTime OriginationDateTime { get; set; }
        /// <summary>
        /// Origination Date as string
        /// </summary>
        public string OriginationDate => OriginationDateTime.ToString("yyyy-MM-dd");
        /// <summary>
        /// Origination as time
        /// </summary>
        public string OriginationTime => OriginationDateTime.ToString("HH:mm:ss");
        /// <summary>
        /// Time reference (first sample count since midnight)
        /// </summary>
        public long TimeReference { get; set; }
        /// <summary>
        /// version 2 has loudness stuff which we don't know so using version 1
        /// </summary>
        public ushort Version => 1;
        /// <summary>
        /// 64 bytes http://en.wikipedia.org/wiki/UMID
        /// </summary>
        public string UniqueMaterialIdentifier { get; set; }
        /// <summary>
        /// for version 2 = 180 bytes (10 before are loudness values), using version 1 = 190 bytes
        /// </summary>
        public byte[] Reserved { get; }
        /// <summary>
        /// Coding history arbitrary length string at end of structure
        /// http://www.ebu.ch/CMSimages/fr/tec_text_r98-1999_tcm7-4709.pdf
        /// A=PCM,F=48000,W=16,M=stereo,T=original,CR/LF
        /// </summary>
        public string CodingHistory { get; set; }
    }
}