using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace NAudio.Gui.TrackView
{
    /// <summary>
    /// Holds details of a track for the trackvew
    /// </summary>
    public class Track
    {
        private string name;
        private int height;
        private float volume;
        private float pan;
        private List<Clip> clips;

        /// <summary>
        /// Creates a new track
        /// </summary>
        public Track(string name)
        {
            this.name = name;
            this.height = 30;
            this.volume = 1.0f;
            this.pan = 0.0f;
            clips = new List<Clip>();
        }

        /// <summary>
        /// Track name
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Track height
        /// </summary>
        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        /// <summary>
        /// Volume
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }

        /// <summary>
        /// Pan
        /// </summary>
        public float Pan
        {
            get { return pan; }
            set { pan = value; }
        }

        /// <summary>
        /// Clips contained in this track
        /// </summary>
        public List<Clip> Clips
        {
            get { return clips; }
        }

        /// <summary>
        /// Finds the clip at a specified time
        /// </summary>
        public Clip ClipAtTime(TimeSpan time)
        {
            foreach (Clip clip in clips)
            {
                if (clip.StartTime >= time && time < clip.EndTime)
                {
                    return clip;
                }
            }
            return null;
        }

    }
}
