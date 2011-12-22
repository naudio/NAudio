using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Gui.TrackView
{
    /// <summary>
    /// Audio Clip
    /// </summary>
    public class AudioClip : Clip
    {
        private string sourceFileName;

        /// <summary>
        /// Creates a new Audio Clip
        /// </summary>
        public AudioClip(string name, TimeSpan startTime, TimeSpan duration)
            : base(name, startTime, duration)
        {

        }        

        /// <summary>
        /// Source File Name
        /// </summary>
        public string SourceFileName
        {
            get { return sourceFileName; }
            set { sourceFileName = value; }
        }


    }
}
