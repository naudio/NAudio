using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Midi;

namespace NAudioDemo.MidiInDemo
{
    public partial class MidiInPanel : UserControl
    {
        MidiIn midiIn;
        MidiOut midiOut;
        bool monitoring;
        bool sendingMidiOut;
        List<MidiEvent> events;
        int midiOutIndex;

        public MidiInPanel()
        {
            InitializeComponent();
        }

        private void MidiInForm_Load(object sender, EventArgs e)
        {
            PopulateDeviceLists();
            events = new List<MidiEvent>();
            for (int note = 50; note < 62; note++)
            {
                AddNoteEvent(note);
            }
        }

        private void PopulateDeviceLists()
        {
            string selectedIn = comboBoxMidiInDevices.SelectedItem as string;
            string selectedOut = comboBoxMidiOutDevices.SelectedItem as string;

            comboBoxMidiInDevices.Items.Clear();
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                comboBoxMidiInDevices.Items.Add(MidiIn.DeviceInfo(device).ProductName);
            }
            if (comboBoxMidiInDevices.Items.Count > 0)
            {
                int index = selectedIn != null ? comboBoxMidiInDevices.Items.IndexOf(selectedIn) : -1;
                comboBoxMidiInDevices.SelectedIndex = index >= 0 ? index : 0;
            }

            comboBoxMidiOutDevices.Items.Clear();
            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                comboBoxMidiOutDevices.Items.Add(MidiOut.DeviceInfo(device).ProductName);
            }
            if (comboBoxMidiOutDevices.Items.Count > 0)
            {
                int index = selectedOut != null ? comboBoxMidiOutDevices.Items.IndexOf(selectedOut) : -1;
                comboBoxMidiOutDevices.SelectedIndex = index >= 0 ? index : 0;
            }
        }

        private void AddNoteEvent(int noteNumber)
        {
            int channel = 1;
            NoteOnEvent noteOnEvent = new NoteOnEvent(0, channel, noteNumber, 100, 50);
            events.Add(noteOnEvent);
            events.Add(noteOnEvent.OffEvent);
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
            if (comboBoxMidiInDevices.Items.Count == 0)
            {
                MessageBox.Show("No MIDI input devices available");
                return;
            }
            if (midiIn != null)
            {
                midiIn.MessageReceived -= midiIn_MessageReceived;
                midiIn.ErrorReceived -= midiIn_ErrorReceived;
                midiIn.Dispose();
                midiIn = null;
            }
            midiIn = new MidiIn(comboBoxMidiInDevices.SelectedIndex);
            midiIn.MessageReceived += midiIn_MessageReceived;
            midiIn.ErrorReceived += midiIn_ErrorReceived;
            midiIn.Start();
            monitoring = true;
            buttonMonitor.Text = "Stop";
            UpdateDeviceListEnabledState();
        }

        void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            progressLog1.LogMessage(Color.Red, String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        private void StopMonitoring()
        {
            if (monitoring && midiIn != null)
            {
                midiIn.Stop();
                midiIn.MessageReceived -= midiIn_MessageReceived;
                midiIn.ErrorReceived -= midiIn_ErrorReceived;
                midiIn.Dispose();
                midiIn = null;
            }
            monitoring = false;
            buttonMonitor.Text = "Monitor";
            UpdateDeviceListEnabledState();
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

        private void MidiInPanel_Disposed(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            StopMonitoring();
            StopSendingMidiOut();
        }

        private void buttonClearLog_Click(object sender, EventArgs e)
        {
            progressLog1.ClearLog();
        }

        private void buttonRefreshDevices_Click(object sender, EventArgs e)
        {
            PopulateDeviceLists();
        }

        private void checkBoxMidiOutMessages_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxMidiOutMessages.Checked)
            {
                StartSendingMidiOut();
            }
            else
            {
                StopSendingMidiOut();
            }
        }

        private void StartSendingMidiOut()
        {
            if (comboBoxMidiOutDevices.Items.Count == 0)
            {
                MessageBox.Show("No MIDI output devices available");
                checkBoxMidiOutMessages.Checked = false;
                return;
            }
            if (midiOut == null)
            {
                midiOut = new MidiOut(comboBoxMidiOutDevices.SelectedIndex);
            }
            midiOutIndex = 0;
            sendingMidiOut = true;
            UpdateDeviceListEnabledState();
        }

        private void StopSendingMidiOut()
        {
            sendingMidiOut = false;
            if (midiOut != null)
            {
                // Make sure any currently playing note is silenced.
                midiOut.Reset();
                midiOut.Dispose();
                midiOut = null;
            }
            midiOutIndex = 0;
            UpdateDeviceListEnabledState();
        }

        private void UpdateDeviceListEnabledState()
        {
            comboBoxMidiInDevices.Enabled = !monitoring;
            comboBoxMidiOutDevices.Enabled = !sendingMidiOut;
            buttonRefreshDevices.Enabled = !monitoring && !sendingMidiOut;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (sendingMidiOut)
            {
                SendNextMidiOutMessage();
            }
        }

        private void SendNextMidiOutMessage()
        {
            if (midiOut == null) return;
            MidiEvent eventToSend = events[midiOutIndex++];
            midiOut.Send(eventToSend.GetAsShortMessage());
            progressLog1.LogMessage(Color.Green, String.Format("Sent {0}", eventToSend));
            if (midiOutIndex >= events.Count)
            {
                midiOutIndex = 0;
            }
        }
    }

    public class MidiInPanelPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "MIDI In"; }
        }

        public Control CreatePanel()
        {
            return new MidiInPanel();
        }
    }
}
