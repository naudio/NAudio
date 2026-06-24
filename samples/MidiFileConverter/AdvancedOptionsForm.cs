using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MarkHeath.MidiUtils
{
    public partial class AdvancedOptionsForm : Form
    {
        private Properties.Settings settings;
        public AdvancedOptionsForm()
        {
            InitializeComponent();
            settings = Properties.Settings.Default;
            LoadSettings();
        }

        private void LoadSettings()
        {
            checkBoxAddNameMarker.Checked = settings.AddNameMarker;
            checkBoxRecreateEndTrack.Checked = settings.RecreateEndTrackMarkers;
            checkBoxRemoveEmptyTracks.Checked = settings.RemoveEmptyTracks;
            checkBoxRemoveSequencerSpecific.Checked = settings.RemoveSequencerSpecific;
            checkBoxTrimTextEvents.Checked = settings.TrimTextEvents;
            checkBoxRemoveExtraTempoEvents.Checked = settings.RemoveExtraTempoEvents;
            checkBoxRemoveExtraMarkers.Checked = settings.RemoveExtraMarkers;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            settings.AddNameMarker = checkBoxAddNameMarker.Checked;
            settings.RecreateEndTrackMarkers = checkBoxRecreateEndTrack.Checked;
            settings.RemoveEmptyTracks =checkBoxRemoveEmptyTracks.Checked;
            settings.RemoveSequencerSpecific = checkBoxRemoveSequencerSpecific.Checked;
            settings.TrimTextEvents = checkBoxTrimTextEvents.Checked;
            settings.RemoveExtraTempoEvents = checkBoxRemoveExtraTempoEvents.Checked;
            settings.RemoveExtraMarkers = checkBoxRemoveExtraMarkers.Checked;
            settings.Save();
            this.Close();
        }
    }
}