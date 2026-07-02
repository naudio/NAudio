using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Extras;
using NAudio.Gui;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioDemo.Utils;

namespace NAudioDemo.MixingCaptureDemo;

/// <summary>
/// Captures up to three WASAPI sources (microphones and/or loopback endpoints) at once and
/// live-mixes them to a single WAV file, with a level meter per source. Built on
/// <see cref="RealtimeCaptureMixer"/> / <see cref="CaptureMixerInput"/> from NAudio.Extras.
/// </summary>
public class MixingCapturePanel : UserControl
{
    private const int SourceCount = 3;
    private static readonly WaveFormat TargetFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    private sealed class DeviceItem
    {
        public MMDevice Device { get; init; }
        public bool IsLoopback { get; init; }
        public string Display { get; init; }
        public override string ToString() => Display;
    }

    private sealed class SourceRow
    {
        public ComboBox Devices;
        public CheckBox Enabled;
        public VolumeMeter Meter;
    }

    private readonly SourceRow[] rows = new SourceRow[SourceCount];
    private readonly MMDeviceEnumerator enumerator = new();
    private NumericUpDown maxSecondsInput;
    private Button startButton;
    private Button stopButton;
    private Label statusLabel;
    private ListBox recordingsList;
    private readonly string outputFolder;

    // capture state (only touched on the UI thread except where noted)
    private RealtimeCaptureMixer mixer;
    private readonly List<WasapiRecorder> recorders = new();
    private WaveFileWriter writer;
    private Thread pumpThread;
    private volatile bool stopRequested;
    private int captureMaxSeconds;
    private string outputPath;

    public MixingCapturePanel()
    {
        outputFolder = Path.Combine(Path.GetTempPath(), "NAudioDemo");
        Directory.CreateDirectory(outputFolder);
        BuildUi();
        PopulateDevices();
        RefreshRecordings();
        Disposed += (s, e) => StopCapture();
    }

    private void BuildUi()
    {
        Size = new Size(640, 470);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(8),
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var header = new Label
        {
            Text = "Select up to three WASAPI sources to capture and mix together. " +
                   "[System] entries are loopback (what a device is playing); [Mic] entries are capture devices. " +
                   $"Output is 48 kHz stereo. Sources are aligned using each packet's high-resolution timestamp.",
            AutoSize = true,
            Margin = new Padding(3, 3, 3, 8),
            MaximumSize = new Size(600, 0),
        };
        layout.Controls.Add(header, 0, 0);
        layout.SetColumnSpan(header, 3);

        for (var i = 0; i < SourceCount; i++)
        {
            var row = new SourceRow
            {
                Devices = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3, 3, 3, 3),
                },
                Enabled = new CheckBox
                {
                    Text = "Capture",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(6, 6, 6, 3),
                },
                Meter = new VolumeMeter
                {
                    Orientation = Orientation.Horizontal,
                    Size = new Size(140, 22),
                    Amplitude = 0f,
                    Margin = new Padding(3, 3, 3, 3),
                    Anchor = AnchorStyles.Left,
                },
            };
            rows[i] = row;
            layout.Controls.Add(row.Devices, 0, i + 1);
            layout.Controls.Add(row.Enabled, 1, i + 1);
            layout.Controls.Add(row.Meter, 2, i + 1);
        }

        var controls = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Margin = new Padding(3, 8, 3, 3),
            WrapContents = false,
        };
        controls.Controls.Add(new Label { Text = "Max length (seconds):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) });
        maxSecondsInput = new NumericUpDown { Minimum = 1, Maximum = 3600, Value = 20, Width = 70, Margin = new Padding(3, 4, 12, 3) };
        controls.Controls.Add(maxSecondsInput);
        startButton = new Button { Text = "Start", Width = 80, Margin = new Padding(3) };
        stopButton = new Button { Text = "Stop", Width = 80, Enabled = false, Margin = new Padding(3) };
        startButton.Click += (s, e) => StartCapture();
        stopButton.Click += (s, e) => StopCapture();
        controls.Controls.Add(startButton);
        controls.Controls.Add(stopButton);
        layout.Controls.Add(controls, 0, SourceCount + 1);
        layout.SetColumnSpan(controls, 3);

        statusLabel = new Label { Text = "Ready.", AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
        layout.Controls.Add(statusLabel, 0, SourceCount + 2);
        layout.SetColumnSpan(statusLabel, 3);

        var recordings = new GroupBox
        {
            Text = "Recordings",
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 150),
            Margin = new Padding(3, 8, 3, 3),
        };
        recordingsList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        recordingsList.DoubleClick += (s, e) => PlaySelectedRecording();
        var recordingButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.TopDown,
            Width = 110,
            WrapContents = false,
        };
        var playButton = new Button { Text = "Play", Width = 95, Margin = new Padding(3) };
        var deleteButton = new Button { Text = "Delete", Width = 95, Margin = new Padding(3) };
        var openFolderButton = new Button { Text = "Open folder", Width = 95, Margin = new Padding(3) };
        playButton.Click += (s, e) => PlaySelectedRecording();
        deleteButton.Click += (s, e) => DeleteSelectedRecording();
        openFolderButton.Click += (s, e) => ProcessHelper.ShellExecute(outputFolder);
        recordingButtons.Controls.Add(playButton);
        recordingButtons.Controls.Add(deleteButton);
        recordingButtons.Controls.Add(openFolderButton);
        recordings.Controls.Add(recordingsList);
        recordings.Controls.Add(recordingButtons);
        layout.Controls.Add(recordings, 0, SourceCount + 3);
        layout.SetColumnSpan(recordings, 3);

        Controls.Add(layout);
    }

    private void PopulateDevices()
    {
        var items = new List<DeviceItem>();
        foreach (var render in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            items.Add(new DeviceItem { Device = render, IsLoopback = true, Display = $"[System] {render.FriendlyName}" });
        }
        foreach (var capture in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
        {
            items.Add(new DeviceItem { Device = capture, IsLoopback = false, Display = $"[Mic] {capture.FriendlyName}" });
        }

        var firstLoopback = items.FindIndex(d => d.IsLoopback);
        var firstMic = items.FindIndex(d => !d.IsLoopback);
        for (var i = 0; i < SourceCount; i++)
        {
            rows[i].Devices.Items.Clear();
            foreach (var item in items)
            {
                rows[i].Devices.Items.Add(item);
            }
            if (items.Count > 0)
            {
                rows[i].Devices.SelectedIndex = 0;
            }
        }

        // Sensible defaults: row 0 = system audio, row 1 = microphone, row 2 off.
        if (firstLoopback >= 0) rows[0].Devices.SelectedIndex = firstLoopback;
        rows[0].Enabled.Checked = firstLoopback >= 0;
        if (firstMic >= 0) rows[1].Devices.SelectedIndex = firstMic;
        rows[1].Enabled.Checked = firstMic >= 0;
        rows[2].Enabled.Checked = false;

        if (items.Count == 0)
        {
            statusLabel.Text = "No active audio endpoints found.";
            startButton.Enabled = false;
        }
    }

    private void StartCapture()
    {
        var anySelected = rows.Any(r => r.Enabled.Checked && r.Devices.SelectedItem is DeviceItem);
        if (!anySelected)
        {
            MessageBox.Show(this, "Enable at least one source to capture.", "Mixing Capture",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        stopRequested = false;
        captureMaxSeconds = (int)maxSecondsInput.Value;
        outputPath = Path.Combine(outputFolder, $"mixed-capture-{DateTime.Now:yyyyMMdd-HHmmss}.wav");
        mixer = new RealtimeCaptureMixer(TargetFormat);
        recorders.Clear();

        try
        {
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (!(row.Enabled.Checked && row.Devices.SelectedItem is DeviceItem item))
                {
                    row.Meter.Amplitude = 0f;
                    continue;
                }

                var rowIndex = i;
                var builder = new WasapiRecorderBuilder().WithDevice(item.Device);
                if (item.IsLoopback)
                {
                    // Shared-mode loopback does not reliably support event sync — use polling.
                    builder = builder.WithLoopbackCapture().WithPollingSync();
                }

                var recorder = builder.Build();
                var input = mixer.AddInput(recorder.WaveFormat, provider =>
                {
                    var metering = new MeteringSampleProvider(provider, TargetFormat.SampleRate / 20);
                    metering.StreamVolume += (s, e) => UpdateMeter(rowIndex, e.MaxSampleValues);
                    return metering;
                });
                recorder.DataAvailable += (data, flags, devicePosition, qpcPosition) =>
                    input.AddSamples(data, qpcPosition, devicePosition);
                recorders.Add(recorder);
            }

            writer = new WaveFileWriter(outputPath, TargetFormat);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to start capture:\r\n{ex.Message}", "Mixing Capture",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            CleanupResources();
            return;
        }

        mixer.Start();
        foreach (var recorder in recorders)
        {
            recorder.StartRecording();
        }

        pumpThread = new Thread(PumpLoop) { IsBackground = true, Name = "MixingCapturePump" };
        pumpThread.Start();

        SetRunningState(true);
        statusLabel.Text = $"Recording {recorders.Count} source(s) to {Path.GetFileName(outputPath)} ...";
    }

    private void PumpLoop()
    {
        var buffer = new float[TargetFormat.SampleRate * TargetFormat.Channels / 5]; // ~200ms
        var maxFrames = (long)captureMaxSeconds * TargetFormat.SampleRate;
        long framesWritten = 0;
        var reachedLimit = false;
        try
        {
            while (!stopRequested)
            {
                var samples = mixer.Read(buffer, 0, buffer.Length);
                if (samples > 0)
                {
                    writer.WriteSamples(buffer, 0, samples);
                    framesWritten += samples / TargetFormat.Channels;
                    if (framesWritten >= maxFrames)
                    {
                        reachedLimit = true;
                        break;
                    }
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }
        catch
        {
            // Any pump failure ends the recording; StopCapture reports the saved file.
        }

        if (IsHandleCreated && !IsDisposed)
        {
            try
            {
                BeginInvoke(() => StopCapture(reachedLimit));
            }
            catch (InvalidOperationException)
            {
                // handle went away between the check and the call — nothing to do
            }
        }
    }

    private void UpdateMeter(int rowIndex, float[] maxSampleValues)
    {
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }
        var level = 0f;
        foreach (var value in maxSampleValues)
        {
            level = Math.Max(level, value);
        }
        try
        {
            BeginInvoke(() =>
            {
                if (!IsDisposed)
                {
                    rows[rowIndex].Meter.Amplitude = level;
                }
            });
        }
        catch (InvalidOperationException)
        {
            // handle destroyed; ignore
        }
    }

    private void StopCapture(bool reachedLimit = false)
    {
        if (mixer == null && recorders.Count == 0 && writer == null && pumpThread == null)
        {
            return; // not running
        }

        stopRequested = true;
        var thread = pumpThread;
        pumpThread = null;
        if (thread != null && thread.ManagedThreadId != Environment.CurrentManagedThreadId)
        {
            thread.Join(2000);
        }

        CleanupResources();

        foreach (var row in rows)
        {
            row.Meter.Amplitude = 0f;
        }
        SetRunningState(false);

        if (outputPath != null)
        {
            statusLabel.Text = reachedLimit
                ? $"Reached {captureMaxSeconds}s limit. Saved {outputPath}"
                : $"Stopped. Saved {outputPath}";
            if (File.Exists(outputPath))
            {
                AddRecording(outputPath);
            }
        }
    }

    private void RefreshRecordings()
    {
        recordingsList.Items.Clear();
        foreach (var file in Directory.EnumerateFiles(outputFolder, "mixed-capture-*.wav").OrderBy(f => f))
        {
            recordingsList.Items.Add(Path.GetFileName(file));
        }
        if (recordingsList.Items.Count > 0)
        {
            recordingsList.SelectedIndex = recordingsList.Items.Count - 1;
        }
    }

    private void AddRecording(string path)
    {
        var name = Path.GetFileName(path);
        var index = recordingsList.Items.IndexOf(name);
        if (index < 0)
        {
            index = recordingsList.Items.Add(name);
        }
        recordingsList.SelectedIndex = index;
    }

    private void PlaySelectedRecording()
    {
        if (recordingsList.SelectedItem is string name)
        {
            ProcessHelper.ShellExecute(Path.Combine(outputFolder, name));
        }
    }

    private void DeleteSelectedRecording()
    {
        if (recordingsList.SelectedItem is not string name)
        {
            return;
        }
        try
        {
            File.Delete(Path.Combine(outputFolder, name));
            recordingsList.Items.Remove(name);
            if (recordingsList.Items.Count > 0)
            {
                recordingsList.SelectedIndex = recordingsList.Items.Count - 1;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not delete recording:\r\n{ex.Message}", "Mixing Capture",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CleanupResources()
    {
        foreach (var recorder in recorders)
        {
            try { recorder.StopRecording(); } catch { /* best effort */ }
            try { recorder.Dispose(); } catch { /* best effort */ }
        }
        recorders.Clear();

        if (writer != null)
        {
            try { writer.Dispose(); } catch { /* best effort */ }
            writer = null;
        }
        mixer = null;
    }

    private void SetRunningState(bool running)
    {
        startButton.Enabled = !running;
        stopButton.Enabled = running;
        maxSecondsInput.Enabled = !running;
        foreach (var row in rows)
        {
            row.Devices.Enabled = !running;
            row.Enabled.Enabled = !running;
        }
    }
}
