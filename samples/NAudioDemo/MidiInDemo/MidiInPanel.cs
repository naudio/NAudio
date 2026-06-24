using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Midi;
using Windows.Devices.Enumeration;

namespace NAudioDemo.MidiInDemo;

public partial class MidiInPanel : UserControl
{
    private const string BackendWinRT = "WinRT (Windows.Devices.Midi)";
    private const string BackendWinMM = "Legacy WinMM";

    private IMidiInput midiIn;
    private IMidiOutput midiOut;
    private bool monitoring;
    private bool sendingMidiOut;
    private List<MidiEvent> events;
    private int midiOutIndex;

    // WinRT enumeration results, indexed alongside the corresponding combo box.
    private IReadOnlyList<DeviceInformation> winRtInDevices;
    private IReadOnlyList<DeviceInformation> winRtOutDevices;

    public MidiInPanel()
    {
        InitializeComponent();
    }

    private async void MidiInForm_Load(object sender, EventArgs e)
    {
        comboBoxBackend.Items.AddRange([BackendWinRT, BackendWinMM]);
        comboBoxBackend.SelectedIndex = 0; // WinRT default

        await PopulateDeviceListsAsync();

        events = new List<MidiEvent>();
        for (int note = 50; note < 62; note++)
        {
            AddNoteEvent(note);
        }
    }

    private bool IsWinRT => comboBoxBackend.SelectedItem as string == BackendWinRT;

    private async Task PopulateDeviceListsAsync()
    {
        string selectedIn = comboBoxMidiInDevices.SelectedItem as string;
        string selectedOut = comboBoxMidiOutDevices.SelectedItem as string;

        comboBoxMidiInDevices.Items.Clear();
        comboBoxMidiOutDevices.Items.Clear();
        winRtInDevices = null;
        winRtOutDevices = null;

        if (IsWinRT)
        {
            winRtInDevices = await WinRTMidiIn.GetDevicesAsync();
            foreach (var device in winRtInDevices)
            {
                comboBoxMidiInDevices.Items.Add(device.Name);
            }

            winRtOutDevices = await WinRTMidiOut.GetDevicesAsync();
            foreach (var device in winRtOutDevices)
            {
                comboBoxMidiOutDevices.Items.Add(device.Name);
            }
        }
        else
        {
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                comboBoxMidiInDevices.Items.Add(MidiIn.DeviceInfo(device).ProductName);
            }
            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                comboBoxMidiOutDevices.Items.Add(MidiOut.DeviceInfo(device).ProductName);
            }
        }

        if (comboBoxMidiInDevices.Items.Count > 0)
        {
            int index = selectedIn != null ? comboBoxMidiInDevices.Items.IndexOf(selectedIn) : -1;
            comboBoxMidiInDevices.SelectedIndex = index >= 0 ? index : 0;
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
        var noteOnEvent = new NoteOnEvent(0, channel, noteNumber, 100, 50);
        events.Add(noteOnEvent);
        events.Add(noteOnEvent.OffEvent);
    }

    private async void OnButtonMonitorClick(object sender, EventArgs e)
    {
        if (!monitoring)
        {
            await StartMonitoringAsync();
        }
        else
        {
            StopMonitoring();
        }
    }

    private async Task StartMonitoringAsync()
    {
        if (comboBoxMidiInDevices.Items.Count == 0)
        {
            MessageBox.Show("No MIDI input devices available");
            return;
        }
        DisposeMidiIn();
        try
        {
            int index = comboBoxMidiInDevices.SelectedIndex;
            if (IsWinRT)
            {
                midiIn = await WinRTMidiIn.CreateAsync(winRtInDevices[index].Id);
            }
            else
            {
                var legacyIn = new MidiIn(index);
                legacyIn.ErrorReceived += OnMidiInErrorReceived;
                midiIn = legacyIn;
            }
            midiIn.MessageReceived += OnMidiInMessageReceived;
            midiIn.SysexMessageReceived += OnMidiInSysexMessageReceived;
            midiIn.Start();
            monitoring = true;
            buttonMonitor.Text = "Stop";
            UpdateDeviceListEnabledState();
        }
        catch (Exception ex)
        {
            progressLog1.LogMessage(Color.Red, "Failed to open MIDI input: " + ex.Message);
            DisposeMidiIn();
        }
    }

    void OnMidiInErrorReceived(object sender, MidiInMessageEventArgs e)
    {
        progressLog1.LogMessage(Color.Red, $"Time {e.Timestamp} Message 0x{e.RawMessage:X8} Event {e.MidiEvent}");
    }

    private void StopMonitoring()
    {
        if (monitoring)
        {
            try { midiIn?.Stop(); } catch { /* swallow — already disposing */ }
        }
        DisposeMidiIn();
        monitoring = false;
        buttonMonitor.Text = "Monitor";
        UpdateDeviceListEnabledState();
    }

    private void DisposeMidiIn()
    {
        if (midiIn != null)
        {
            midiIn.MessageReceived -= OnMidiInMessageReceived;
            midiIn.SysexMessageReceived -= OnMidiInSysexMessageReceived;
            if (midiIn is MidiIn legacyIn)
            {
                legacyIn.ErrorReceived -= OnMidiInErrorReceived;
            }
            midiIn.Dispose();
            midiIn = null;
        }
    }

    void OnMidiInMessageReceived(object sender, MidiInMessageEventArgs e)
    {
        if (checkBoxFilterAutoSensing.Checked && e.MidiEvent != null && e.MidiEvent.CommandCode == MidiCommandCode.AutoSensing)
        {
            return;
        }
        progressLog1.LogMessage(Color.Blue, $"Time {e.Timestamp} Message 0x{e.RawMessage:X8} Event {e.MidiEvent}");
    }

    void OnMidiInSysexMessageReceived(object sender, MidiInSysexMessageEventArgs e)
    {
        progressLog1.LogMessage(Color.DarkBlue, $"Time {e.Timestamp} Sysex {e.SysexBytes.Length} bytes");
    }

    private void MidiInPanel_Disposed(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        StopMonitoring();
        StopSendingMidiOut();
    }

    private void OnButtonClearLogClick(object sender, EventArgs e)
    {
        progressLog1.ClearLog();
    }

    private async void OnButtonRefreshDevicesClick(object sender, EventArgs e)
    {
        await PopulateDeviceListsAsync();
    }

    private async void OnComboBoxBackendSelectedIndexChanged(object sender, EventArgs e)
    {
        // Stop anything in-flight before switching APIs.
        StopMonitoring();
        StopSendingMidiOut();
        await PopulateDeviceListsAsync();
    }

    private async void OnCheckBoxMidiOutMessagesCheckedChanged(object sender, EventArgs e)
    {
        if (checkBoxMidiOutMessages.Checked)
        {
            await StartSendingMidiOutAsync();
        }
        else
        {
            StopSendingMidiOut();
        }
    }

    private async Task StartSendingMidiOutAsync()
    {
        if (comboBoxMidiOutDevices.Items.Count == 0)
        {
            MessageBox.Show("No MIDI output devices available");
            checkBoxMidiOutMessages.Checked = false;
            return;
        }
        if (midiOut == null)
        {
            try
            {
                int index = comboBoxMidiOutDevices.SelectedIndex;
                midiOut = IsWinRT
                    ? await WinRTMidiOut.CreateAsync(winRtOutDevices[index].Id)
                    : new MidiOut(index);
            }
            catch (Exception ex)
            {
                progressLog1.LogMessage(Color.Red, "Failed to open MIDI output: " + ex.Message);
                checkBoxMidiOutMessages.Checked = false;
                return;
            }
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
            // send NoteOff for any notes that might still be on, to avoid hanging notes in the synth
            var nextEvent = events[midiOutIndex] as NoteEvent;
            if (nextEvent != null && !MidiEvent.IsNoteOn(nextEvent))
            {
                midiOut.Send(nextEvent);
            }

            midiOut.Dispose();
            midiOut = null;
        }
        midiOutIndex = 0;
        UpdateDeviceListEnabledState();
    }

    private void UpdateDeviceListEnabledState()
    {
        comboBoxBackend.Enabled = !monitoring && !sendingMidiOut;
        comboBoxMidiInDevices.Enabled = !monitoring;
        comboBoxMidiOutDevices.Enabled = !sendingMidiOut;
        buttonRefreshDevices.Enabled = !monitoring && !sendingMidiOut;
    }

    private void OnTimer1Tick(object sender, EventArgs e)
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
        midiOut.Send(eventToSend); // extension method — works for both backends
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
