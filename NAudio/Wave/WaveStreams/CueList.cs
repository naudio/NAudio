using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace NAudio.Wave
{
    /// <summary>
    /// Holds information on a cue: a labeled position within a Wave file
    /// </summary>
    public class Cue
    {
        /// <summary>
        /// Cue position in samples
        /// </summary>
        public int Position
        {
            get;
            private set;
        }
        /// <summary>
        /// Label of the cue
        /// </summary>
        public string Label
        {
            get;
            private set;
        }
        /// <summary>
        /// Creates a Cue based on a sample position and label 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="label"></param>
        public Cue(int position, string label)
        {
            Position = position;
            if (label == null)
            {
                label = "";
            }
            Label = Regex.Replace(label, @"[^\u0000-\u00FF]", "");
        }
    }

    /// <summary>
    /// Holds a list of cues
    /// </summary>
    /// <remarks>
    /// The specs for reading and writing cues from the cue and list RIFF chunks 
    /// are from http://www.sonicspot.com/guide/wavefiles.html and http://www.wotsit.org/
    /// ------------------------------
    /// The cues are stored like this:
    /// ------------------------------
    /// struct CuePoint
    /// {
    ///  Int32 dwIdentifier;
    ///  Int32 dwPosition;
    ///  Int32 fccChunk;
    ///  Int32 dwChunkStart;
    ///  Int32 dwBlockStart;
    ///  Int32 dwSampleOffset;
    /// } 
    ///
    /// struct CueChunk
    /// {
    ///  Int32 chunkID;
    ///  Int32 chunkSize;
    ///  Int32 dwCuePoints;
    ///  CuePoint[] points;
    /// }
    /// ------------------------------
    /// Labels look like this:
    /// ------------------------------
    /// struct ListHeader 
    /// {
    ///   Int32 listID;      /* 'list' */
    ///   Int32 chunkSize;   /* includes the Type ID below */
    ///   Int32 typeID;      /* 'adtl' */
    /// } 
    ///
    /// struct LabelChunk 
    /// {
    ///   Int32 chunkID;
    ///   Int32 chunkSize;
    ///   Int32 dwIdentifier;
    ///   Char[] dwText;  /* Encoded with extended ASCII */
    /// } LabelChunk;
    /// </remarks>
    public class CueList
    {
        private List<Cue> cues;
        /// <summary>
        /// Creates an empty cue list
        /// </summary>
        public CueList()
        {
            cues = new List<Cue>();
        }

        /// <summary>
        /// Adds an item to the list
        /// </summary>
        /// <param name="cue">Cue</param>
        public void Add(Cue cue)
        {
            cues.Add(cue);
        }

        /// <summary>
        /// Gets sample positions for the embedded cues
        /// </summary>
        /// <returns>Array containing the cue positions</returns>
        public int[] CuePositions
        {
            get
            {
                int[] positions = new int[cues.Count];
                for (int i = 0; i < cues.Count; i++)
                {
                    positions[i] = cues[i].Position;
                }
                return positions;
            }
        }

        /// <summary>
        /// Gets labels for the embedded cues
        /// </summary>
        /// <returns>Array containing the labels</returns>
        public string[] CueLabels
        {
            get
            {
                string[] labels = new string[cues.Count];
                for (int i = 0; i < cues.Count; i++)
                {
                    labels[i] = cues[i].Label;
                }
                return labels;
            }
        }

        /// <summary>
        /// Creates a cue list from the cue RIFF chunk and the list RIFF chunk
        /// </summary>
        /// <param name="cueChunkData">The data contained in the cue chunk</param>
        /// <param name="listChunkData">The data contained in the list chunk</param>
        internal CueList(byte[] cueChunkData, byte[] listChunkData)
        {
            int cueCount = BitConverter.ToInt32(cueChunkData, 0);
            Dictionary<int, int> cueIndex = new Dictionary<int, int>();
            int[] positions = new int[cueCount];
            int cue = 0;

            for (int p = 4; cueChunkData.Length - p >= 24; p += 24, cue++)
            {
                cueIndex[BitConverter.ToInt32(cueChunkData, p)] = cue;
                positions[cue] = BitConverter.ToInt32(cueChunkData, p + 20);
            }

            string[] labels = new string[cueCount];
            int labelLength = 0;
            int cueID = 0;

            Int32 labelChunkID = WaveInterop.mmioStringToFOURCC("labl", 0);
            for (int p = 4; listChunkData.Length - p >= 16; p += labelLength + labelLength % 2 + 12)
            {
                if (BitConverter.ToInt32(listChunkData, p) == labelChunkID)
                {
                    labelLength = BitConverter.ToInt32(listChunkData, p + 4) - 4;
                    cueID = BitConverter.ToInt32(listChunkData, p + 8);
                    cue = cueIndex[cueID];
                    labels[cue] = Encoding.Default.GetString(listChunkData, p + 12, labelLength - 1);
                }
            }

            for (int i = 0; i < cueCount; i++)
            {
                cues.Add(new Cue(positions[i], labels[i]));
            }
        }

        /// <summary>
        /// Gets the cues as the concatenated cue and list RIFF chunks.
        /// </summary>
        /// <returns>RIFF chunks containing the cue data</returns>
        internal byte[] GetRIFFChunks()
        {
            if (this.Count == 0)
            {
                return null;
            }
            else
            {
                int cueChunkLength = 12 + 24 * this.Count;
                int listChunkLength = 12;
                int labelChunkLength = 0;
                for (int i = 0; i < this.Count; i++)
                {
                    labelChunkLength = this[i].Label.Length + 1;
                    listChunkLength += labelChunkLength + labelChunkLength % 2 + 12;
                }

                byte[] chunks = new byte[cueChunkLength + listChunkLength];
                Int32 cueChunkID = WaveInterop.mmioStringToFOURCC("cue ", 0);
                Int32 dataChunkID = WaveInterop.mmioStringToFOURCC("data", 0);
                Int32 listChunkID = WaveInterop.mmioStringToFOURCC("LIST", 0);
                Int32 adtlTypeID = WaveInterop.mmioStringToFOURCC("adtl", 0);
                Int32 labelChunkID = WaveInterop.mmioStringToFOURCC("labl", 0);

                using (MemoryStream stream = new MemoryStream(chunks))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(cueChunkID);
                        writer.Write(cueChunkLength - 8);
                        writer.Write(this.Count);
                        for (int cue = 0; cue < this.Count; cue++)
                        {
                            writer.Write(cue);
                            writer.Seek(4, SeekOrigin.Current);
                            writer.Write(dataChunkID);
                            writer.Seek(8, SeekOrigin.Current);
                            writer.Write(this[cue].Position);
                        }
                        writer.Write(listChunkID);
                        writer.Write(listChunkLength - 8);
                        writer.Write(adtlTypeID);
                        for (int cue = 0; cue < this.Count; cue++)
                        {
                            writer.Write(labelChunkID);
                            writer.Write(this[cue].Label.Length + 1 + 4);
                            writer.Write(cue);
                            writer.Write(Encoding.Default.GetBytes(this[cue].Label.ToCharArray()));
                            if (this[cue].Label.Length % 2 == 0)
                            {
                                writer.Seek(2, SeekOrigin.Current);
                            }
                            else
                            {
                                writer.Seek(1, SeekOrigin.Current);
                            }
                        }
                    }
                }
                return chunks;
            }
        }

        /// <summary>
        /// Number of cues
        /// </summary>
        public int Count
        {
            get { return cues.Count; }
        }

        /// <summary>
        /// Accesses the cue at the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Cue this[int index]
	    {
            get { return cues[index]; }
	    }

        /// <summary>
        /// Checks if the cue and list chunks exist and if so, creates a cue list
        /// </summary>
        internal static CueList FromChunks(WaveFileReader reader)
        {
            CueList cueList = null;
            byte[] cueChunkData = null;
            byte[] listChunkData = null;

            foreach (RiffChunk chunk in reader.ExtraChunks)
            {
                if (chunk.IdentifierAsString.ToLower() == "cue ")
                {
                    cueChunkData = reader.GetChunkData(chunk);
                }
                else if (chunk.IdentifierAsString.ToLower() == "list")
                {
                    listChunkData = reader.GetChunkData(chunk);
                }
            }
            if (cueChunkData != null && listChunkData != null)
            {
                cueList = new CueList(cueChunkData, listChunkData);
            }
            return cueList;
        }
    }
}
