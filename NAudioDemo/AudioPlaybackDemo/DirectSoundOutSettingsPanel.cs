using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;

namespace NAudioDemo.AudioPlaybackDemo
{
    public partial class DirectSoundOutSettingsPanel : UserControl
    {
        public DirectSoundOutSettingsPanel()
        {
            InitializeComponent();
            InitialiseDirectSoundControls();
        }

        private void InitialiseDirectSoundControls()
        {
            comboBoxDirectSound.DisplayMember = "Description";
            comboBoxDirectSound.ValueMember = "Guid";
            comboBoxDirectSound.DataSource = DirectSoundOut.Devices;
        }

        public Guid SelectedDevice 
        {
            get { return (Guid)comboBoxDirectSound.SelectedValue; }
        }
    }
}
