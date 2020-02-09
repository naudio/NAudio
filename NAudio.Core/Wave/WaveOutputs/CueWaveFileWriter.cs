using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NAudio.Wave
{
    /// <summary>
    /// A wave file writer that adds cue support
    /// </summary>
    public class CueWaveFileWriter : WaveFileWriter
    {
        private CueList cues = null;

        /// <summary>
        /// Writes a wave file, including a cues chunk
        /// </summary>
        public CueWaveFileWriter(string fileName, WaveFormat waveFormat)
            : base (fileName, waveFormat)
        {
        }

        /// <summary>
        /// Adds a cue to the Wave file
        /// </summary>
        /// <param name="position">Sample position</param>
        /// <param name="label">Label text</param>
        public void AddCue(int position, string label)
        {
            if (cues == null)
            {
                cues = new CueList();
            }
            cues.Add(new Cue(position, label));
        }

        private void WriteCues(BinaryWriter w)
        {
            // write the cue chunks to the end of the stream
            if (cues != null)
            {
                byte[] cueChunks = cues.GetRiffChunks();
                int cueChunksSize = cueChunks.Length;
                w.Seek(0, SeekOrigin.End);
                
                if (w.BaseStream.Length % 2 == 1)
                {
                    w.Write((Byte)0x00);
                }
                
                w.Write(cues.GetRiffChunks(), 0, cueChunksSize);
                w.Seek(4, SeekOrigin.Begin);
                w.Write((int)(w.BaseStream.Length - 8));
            }
        }

        /// <summary>
        /// Updates the header, and writes the cues out
        /// </summary>
        protected override void UpdateHeader(BinaryWriter writer)
        {
            base.UpdateHeader(writer);
            WriteCues(writer);
        }
    }
}

