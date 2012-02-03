using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace NAudio.Gui.TrackView
{
    /// <summary>
    /// A trackview clip
    /// </summary>
    public class Clip
    {
        private string name;
        private Color foreColor = Color.Black;
        private Color backColor = Color.PowderBlue;
        private TimeSpan startTime;
        private TimeSpan duration;

        /// <summary>
        /// Creates a new trackview clip
        /// </summary>
        public Clip(string name, TimeSpan startTime, TimeSpan duration)
        {
            this.name = name;
            this.startTime = startTime;
            this.duration = duration;
        }

        /// <summary>
        /// Clip Name
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Foreground Colour
        /// </summary>
        public Color ForeColor
        {
            get { return foreColor; }
            set { foreColor = value; }
        }

        /// <summary>
        /// Background Colour
        /// </summary>
        public Color BackColor
        {
            get { return backColor; }
            set { backColor = value; }
        }

        /// <summary>
        /// Start Time
        /// </summary>
        public TimeSpan StartTime
        {
            get { return startTime; }
            set { startTime = value; }
        }

        /// <summary>
        /// Duration
        /// </summary>
        public TimeSpan Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        /// <summary>
        /// End Time
        /// </summary>
        public TimeSpan EndTime
        {
            get { return startTime + duration; }
        }
    }
}
