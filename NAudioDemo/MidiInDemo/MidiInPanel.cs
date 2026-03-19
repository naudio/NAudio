using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Midi;
using Windows.Devices.Enumeration;

namespace NAudioDemo.MidiInDemo
{
    public partial class MidiInPanel : UserControl
    {
        private IMidiInput midiInput;
        private IMidiOutput midiOutput;
        private bool monitoring;
        private List<MidiEvent> events;
        private int midiOutIndex;
        private IReadOnlyList<DeviceInformation> midiInDevices;
        private IReadOnlyList<DeviceInformation> midiOutDevices;

        public MidiInPanel()
        {
            InitializeComponent();
        }

        private async void MidiInForm_Load(object sender, EventArgs e)
        {
            midiInDevices = await WinRTMidiIn.GetDevicesAsync();
            foreach (var device in midiInDevices)
            {
                comboBoxMidiInDevices.Items.Add(device.Name);
            }
            if (comboBoxMidiInDevices.Items.Count > 0)
            {
                comboBoxMidiInDevices.SelectedIndex = 0;
            }

            midiOutDevices = await WinRTMidiOut.GetDevicesAsync();
            foreach (var device in midiOutDevices)
            {
                comboBoxMidiOutDevices.Items.Add(device.Name);
            }
            if (comboBoxMidiOutDevices.Items.Count > 0)
            {
                comboBoxMidiOutDevices.SelectedIndex = 0;
            }

            events = new List<MidiEvent>();
            for (int note = 50; note < 62; note++)
            {
                AddNoteEvent(note);
            }
        }

        private void AddNoteEvent(int noteNumber)
        {
            int channel = 1;
            var noteOnEvent = new NoteOnEvent(0, channel, noteNumber, 100, 50);
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

        private async void StartMonitoring()
        {
            if (midiInDevices == null || midiInDevices.Count == 0)
            {
                MessageBox.Show("No MIDI input devices available");
                return;
            }
            if (midiInput != null)
            {
                midiInput.Dispose();
                midiInput.MessageReceived -= midiIn_MessageReceived;
                midiInput.ErrorReceived -= midiIn_ErrorReceived;
                midiInput = null;
            }

            var deviceId = midiInDevices[comboBoxMidiInDevices.SelectedIndex].Id;
            midiInput = await WinRTMidiIn.CreateAsync(deviceId);
            midiInput.MessageReceived += midiIn_MessageReceived;
            midiInput.ErrorReceived += midiIn_ErrorReceived;
            midiInput.Start();
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
                midiInput.Stop();
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

        private void MidiInPanel_Disposed(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            StopMonitoring();
            if (midiInput != null)
            {
                midiInput.Dispose();
                midiInput = null;
            }
            if (midiOutput != null)
            {
                midiOutput.Dispose();
                midiOutput = null;
            }
        }

        private void buttonClearLog_Click(object sender, EventArgs e)
        {
            progressLog1.ClearLog();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (checkBoxMidiOutMessages.Checked)
            {
                SendNextMidiOutMessage();
            }
        }

        private async void SendNextMidiOutMessage()
        {
            if (midiOutput == null)
            {
                if (midiOutDevices == null || midiOutDevices.Count == 0) return;
                var deviceId = midiOutDevices[comboBoxMidiOutDevices.SelectedIndex].Id;
                midiOutput = await WinRTMidiOut.CreateAsync(deviceId);
            }
            MidiEvent eventToSend = events[midiOutIndex++];
            midiOutput.Send(eventToSend.GetAsShortMessage());
            progressLog1.LogMessage(Color.Green, $"Sent {eventToSend}");
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
