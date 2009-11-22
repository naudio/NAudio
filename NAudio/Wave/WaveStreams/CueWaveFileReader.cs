using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// A wave file reader supporting cue reading
    /// </summary>
    public class CueWaveFileReader : WaveFileReader
    {
        private CueList cues = null;

        /// <summary>
        /// Loads a wavefile and supports reading cues
        /// </summary>
        /// <param name="fileName"></param>
        public CueWaveFileReader(string fileName)
            : base(fileName)
        {
        }

        /// <summary>
        /// Cue List (can be null if cues not present)
        /// </summary>
        public CueList Cues
        {
            get
            {
                if (cues == null)
                {
                    cues = CueList.FromChunks(this);
                }
                return cues;
            }
        }
    }
}
