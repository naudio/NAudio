using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NAudio.Midi;

namespace NAudioDemo
{
    public partial class MidiInForm : Form
    {
        MidiIn midiIn;
        bool monitoring;

        public MidiInForm()
        {
            InitializeComponent();
        }

        private void MidiInForm_Load(object sender, EventArgs e)
        {
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                comboBoxMidiInDevices.Items.Add(MidiIn.DeviceInfo(device).ProductName);
            }
            if (comboBoxMidiInDevices.Items.Count > 0)
            {
                comboBoxMidiInDevices.SelectedIndex = 0;
            }
        }

        private void buttonMonitor_Click(object sender, EventArgs e)
        {
            if (!monitoring)
            {
                StartMonitoring();
            }
            else
            {
                StopMonitoring();
            }
        }

        private void StartMonitoring()
        {
            if (midiIn == null)
            {
                midiIn = new MidiIn(comboBoxMidiInDevices.SelectedIndex);
                midiIn.MessageReceived += new EventHandler<MidiInMessageEventArgs>(midiIn_MessageReceived);
                midiIn.ErrorReceived += new EventHandler<MidiInMessageEventArgs>(midiIn_ErrorReceived);
            }
            midiIn.Start();
            monitoring = true;
            buttonMonitor.Text = "Stop";
            comboBoxMidiInDevices.Enabled = false;

        }

        void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            progressLog1.LogMessage(Color.Red, String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        private void StopMonitoring()
        {
            if (monitoring)
            {
                midiIn.Stop();
                monitoring = false;
                buttonMonitor.Text = "Monitor";
                comboBoxMidiInDevices.Enabled = true;
            }
        }

        void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            if (checkBoxFilterAutoSensing.Checked && e.MidiEvent != null && e.MidiEvent.CommandCode == MidiCommandCode.AutoSensing)
            {
                return;
            }
            progressLog1.LogMessage(Color.Blue, String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        private void MidiInForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopMonitoring();
            if (midiIn != null)
            {
                midiIn.Dispose();
                midiIn.Close();
            }
        }

        private void buttonClearLog_Click(object sender, EventArgs e)
        {
            progressLog1.ClearLog();
        }
    }
}