using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace NAudio.Gui.TrackView
{
    /// <summary>
    /// A track header control
    /// </summary>
    public partial class TrackHeader : UserControl
    {
        private Track track;

        /// <summary>
        /// Creates a new track header control
        /// </summary>
        public TrackHeader()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Associate the control with a track
        /// </summary>
        /// <param name="track"></param>
        public void Initialize(Track track)
        {
            this.track = track;
            textBoxTrackName.Text = track.Name;
            volumeSlider1.Volume = track.Volume;
            panSlider1.Pan = track.Pan;
        }

        private void volumeSlider1_VolumeChanged(object sender, EventArgs e)
        {
            track.Volume = volumeSlider1.Volume;
        }

        private void panSlider1_PanChanged(object sender, EventArgs e)
        {
            track.Pan = panSlider1.Pan;
        }
    }
}
