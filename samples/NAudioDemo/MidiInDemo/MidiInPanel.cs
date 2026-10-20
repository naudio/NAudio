using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    // Messages offered by the Send SysEx button. GM System On is a real-world message any General
    // MIDI synth acts on; the test patterns are sized to span several of the winmm backend's 4 KB
    // receive buffers, so a loopback echo exercises both the long-message send and receive paths.
    private static readonly (string Name, Func<byte[]> Create)[] SysexTests =
    [
        ("GM System On (6 bytes)", () => [0xF0, 0x7E, 0x7F, 0x09, 0x01, 0xF7]),
        ("Test pattern (256 bytes)", () => CreateTestSysex(256)),
        ("Test pattern (4 KB)", () => CreateTestSysex(4 * 1024)),
        ("Test pattern (16 KB)", () => CreateTestSysex(16 * 1024)),
        ("Test pattern (64 KB)", () => CreateTestSysex(64 * 1024)),
    ];

    private IMidiInput midiIn;
    private IMidiOutput midiOut;
    private bool monitoring;
    private bool sendingMidiOut;
    private bool sendingSysex;
    private List<MidiEvent> events;
    private int midiOutIndex;

    // WinRT enumeration results, indexed alongside the corresponding combo box.
    private IReadOnlyList<DeviceInformation> winRtInDevices;
    private IReadOnlyList<DeviceInformation> winRtOutDevices;

    // The last sysex sent, and the one being reassembled from incoming chunks, so that a loopback
    // echo can be checked byte for byte. Input arrives on a driver/WinRT callback thread.
    private readonly object sysexLock = new();
    private readonly List<byte> incomingSysex = new();
    private byte[] lastSentSysex;

    public MidiInPanel()
    {
        InitializeComponent();
    }

    private async void MidiInForm_Load(object sender, EventArgs e)
    {
        comboBoxBackend.Items.AddRange([BackendWinRT, BackendWinMM]);
        comboBoxBackend.SelectedIndex = 0; // WinRT default

        foreach (var test in SysexTests)
        {
            comboBoxSysexTest.Items.Add(test.Name);
        }
        comboBoxSysexTest.SelectedIndex = 0;

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

    private void OnMidiInErrorReceived(object sender, MidiInMessageEventArgs e)
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

    private void OnMidiInMessageReceived(object sender, MidiInMessageEventArgs e)
    {
        if (checkBoxFilterAutoSensing.Checked && e.MidiEvent != null && e.MidiEvent.CommandCode == MidiCommandCode.AutoSensing)
        {
            return;
        }
        progressLog1.LogMessage(Color.Blue, $"Time {e.Timestamp} Message 0x{e.RawMessage:X8} Event {e.MidiEvent}");
    }

    private void OnMidiInSysexMessageReceived(object sender, MidiInSysexMessageEventArgs e)
    {
        progressLog1.LogMessage(Color.DarkBlue, $"Time {e.Timestamp} Sysex {e.SysexBytes.Length} bytes");
        CheckSysexEcho(e.SysexBytes);
    }

    /// <summary>
    /// Reassembles incoming sysex and, once a complete message has arrived, compares it with the
    /// last one sent. The winmm backend delivers a long message in 4 KB chunks; WinRT delivers it whole.
    /// </summary>
    private void CheckSysexEcho(byte[] chunk)
    {
        if (chunk.Length == 0)
        {
            return;
        }

        byte[] received;
        byte[] sent;
        lock (sysexLock)
        {
            if (chunk[0] == 0xF0)
            {
                incomingSysex.Clear();
            }
            incomingSysex.AddRange(chunk);
            if (chunk[^1] != 0xF7)
            {
                return;
            }
            received = incomingSysex.ToArray();
            incomingSysex.Clear();
            sent = lastSentSysex;
        }

        if (sent == null)
        {
            progressLog1.LogMessage(Color.DarkBlue, $"Received complete {received.Length} byte sysex");
        }
        else if (received.AsSpan().SequenceEqual(sent))
        {
            progressLog1.LogMessage(Color.Green, $"Received {received.Length} byte sysex: matches the last one sent");
        }
        else
        {
            int firstDifference = received.AsSpan().CommonPrefixLength(sent);
            progressLog1.LogMessage(Color.Red, $"Received {received.Length} byte sysex: differs from the {sent.Length} bytes sent, from byte {firstDifference}");
        }
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
        if (!await EnsureMidiOutOpenAsync())
        {
            checkBoxMidiOutMessages.Checked = false;
            return;
        }
        midiOutIndex = 0;
        sendingMidiOut = true;
        UpdateDeviceListEnabledState();
    }

    /// <summary>
    /// Opens the selected MIDI output device, unless it is already open.
    /// </summary>
    /// <returns>False if there is no output device or it failed to open; the reason is reported.</returns>
    private async Task<bool> EnsureMidiOutOpenAsync()
    {
        if (midiOut != null)
        {
            return true;
        }
        if (comboBoxMidiOutDevices.Items.Count == 0)
        {
            MessageBox.Show("No MIDI output devices available");
            return false;
        }
        try
        {
            int index = comboBoxMidiOutDevices.SelectedIndex;
            midiOut = IsWinRT
                ? await WinRTMidiOut.CreateAsync(winRtOutDevices[index].Id)
                : new MidiOut(index);
            return true;
        }
        catch (Exception ex)
        {
            progressLog1.LogMessage(Color.Red, "Failed to open MIDI output: " + ex.Message);
            return false;
        }
    }

    // The output is shared by the test-message timer and the Send SysEx button, so it stays open
    // while either is using it.
    private void CloseMidiOut()
    {
        midiOut?.Dispose();
        midiOut = null;
    }

    private async void OnButtonSendSysexClick(object sender, EventArgs e)
    {
        if (sendingSysex)
        {
            return;
        }
        var test = SysexTests[comboBoxSysexTest.SelectedIndex];
        sendingSysex = true;
        UpdateDeviceListEnabledState();
        try
        {
            if (!await EnsureMidiOutOpenAsync())
            {
                return;
            }
            var output = midiOut;
            var sysex = test.Create();
            lock (sysexLock)
            {
                lastSentSysex = sysex;
            }

            progressLog1.LogMessage(Color.Green, $"Sending sysex: {test.Name}");
            var stopwatch = Stopwatch.StartNew();
            // The winmm SendBuffer blocks until the driver has finished with the buffer, which for
            // a large dump over a 31250 baud DIN cable takes seconds, so keep it off the UI thread.
            await Task.Run(() => output.SendBuffer(sysex));
            if (!IsDisposed)
            {
                progressLog1.LogMessage(Color.Green, $"Sent {sysex.Length} byte sysex in {stopwatch.ElapsedMilliseconds} ms");
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                progressLog1.LogMessage(Color.Red, "Failed to send sysex: " + ex.Message);
            }
        }
        finally
        {
            sendingSysex = false;
            if (!sendingMidiOut)
            {
                CloseMidiOut();
            }
            if (!IsDisposed)
            {
                UpdateDeviceListEnabledState();
            }
        }
    }

    /// <summary>
    /// Builds a sysex message of the given total length, framing bytes included.
    /// </summary>
    private static byte[] CreateTestSysex(int length)
    {
        // Manufacturer ID 0x7D is reserved for non-commercial use, so real instruments ignore this
        // message and it is safe to send to hardware. The body is a repeating 7-bit ramp, so a
        // dropped, duplicated or corrupted byte shows up when the echo is compared.
        var sysex = new byte[length];
        sysex[0] = 0xF0;
        sysex[1] = 0x7D;
        for (int i = 2; i < length - 1; i++)
        {
            sysex[i] = (byte)(i & 0x7F);
        }
        sysex[length - 1] = 0xF7;
        return sysex;
    }

    private void StopSendingMidiOut()
    {
        sendingMidiOut = false;
        // A sysex send in flight still needs the output; it closes it when it finishes.
        if (midiOut != null && !sendingSysex)
        {
            // send NoteOff for any notes that might still be on, to avoid hanging notes in the synth
            var nextEvent = events[midiOutIndex] as NoteEvent;
            if (nextEvent != null && !MidiEvent.IsNoteOn(nextEvent))
            {
                midiOut.Send(nextEvent);
            }

            CloseMidiOut();
        }
        midiOutIndex = 0;
        UpdateDeviceListEnabledState();
    }

    private void UpdateDeviceListEnabledState()
    {
        comboBoxBackend.Enabled = !monitoring && !sendingMidiOut && !sendingSysex;
        comboBoxMidiInDevices.Enabled = !monitoring;
        comboBoxMidiOutDevices.Enabled = !sendingMidiOut && !sendingSysex;
        buttonRefreshDevices.Enabled = !monitoring && !sendingMidiOut && !sendingSysex;
        checkBoxMidiOutMessages.Enabled = !sendingSysex;
        buttonSendSysex.Enabled = !sendingSysex;
    }

    private void OnTimer1Tick(object sender, EventArgs e)
    {
        // Hold the test notes while a sysex is in flight: a note interleaved into it would corrupt it.
        if (sendingMidiOut && !sendingSysex)
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
