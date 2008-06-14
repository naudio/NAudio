using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NAudioDemo
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void buttonMidiIn_Click(object sender, EventArgs e)
        {
            MidiInForm midiInForm = new MidiInForm();
            midiInForm.ShowDialog();
        }

        private void buttonWavPlayback_Click(object sender, EventArgs e)
        {
            AudioPlaybackForm audioPlaybackForm = new AudioPlaybackForm();
            audioPlaybackForm.ShowDialog();
        }

        private void buttonAcmFormatConversion_Click(object sender, EventArgs e)
        {
            AcmForm acmForm = new AcmForm();
            acmForm.ShowDialog();
        }
    }
}