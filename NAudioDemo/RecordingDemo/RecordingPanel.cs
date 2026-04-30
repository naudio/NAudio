using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wasapi;
using NAudio.Wave;
using NAudioDemo.Utils;

#pragma warning disable CS0618 // WasapiCapture / WasapiLoopbackCapture are intentionally kept in the demo for comparison

namespace NAudioDemo.RecordingDemo
{
    public partial class RecordingPanel : UserControl
    {
        private static readonly int[] StandardSampleRates = { 8000, 16000, 22050, 32000, 44100, 48000, 96000 };

        private enum RecordingApi
        {
            WaveIn,
            WaveInWindow,
            WasapiCaptureLegacy,
            WasapiLoopbackLegacy,
            WasapiRecorderCapture,
            WasapiRecorderLoopback,
        }

        private sealed class ApiOption
        {
            public RecordingApi Api { get; }
            public string Label { get; }
            public string FileNamePrefix { get; }
            public bool IsWasapi { get; }
            public bool IsLoopback { get; }
            public bool SupportsEventCallback { get; }

            public ApiOption(RecordingApi api, string label, string fileNamePrefix,
                bool isWasapi, bool isLoopback, bool supportsEventCallback)
            {
                Api = api;
                Label = label;
                FileNamePrefix = fileNamePrefix;
                IsWasapi = isWasapi;
                IsLoopback = isLoopback;
                SupportsEventCallback = supportsEventCallback;
            }

            public override string ToString() => Label;
        }

        private static readonly ApiOption[] ApiOptions =
        {
            new ApiOption(RecordingApi.WaveIn,                 "waveIn (event callback)",          "WaveIn",                false, false, false),
            new ApiOption(RecordingApi.WaveInWindow,           "waveIn (window callback)",         "WaveInWindow",          false, false, false),
            new ApiOption(RecordingApi.WasapiCaptureLegacy,    "WasapiCapture (legacy)",           "WasapiCapture",         true,  false, true),
            new ApiOption(RecordingApi.WasapiLoopbackLegacy,   "WasapiLoopbackCapture (legacy)",   "WasapiLoopbackCapture", true,  true,  false),
            new ApiOption(RecordingApi.WasapiRecorderCapture,  "WasapiRecorder",                   "WasapiRecorder",        true,  false, true),
            new ApiOption(RecordingApi.WasapiRecorderLoopback, "WasapiRecorder (Loopback)",        "WasapiRecorderLoopback",true,  true,  false),
        };

        private sealed class WaveInDeviceItem
        {
            public int DeviceNumber { get; }
            public string DisplayName { get; }
            public WaveInDeviceItem(int deviceNumber, string productName)
            {
                DeviceNumber = deviceNumber;
                DisplayName = deviceNumber == -1
                    ? $"Default ({productName})"
                    : $"Device {deviceNumber} ({productName})";
            }
            public override string ToString() => DisplayName;
        }

        private IWaveIn captureDevice;
        private WaveFileWriter writer;
        private string outputFilename;
        private readonly string outputFolder;
        private readonly MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();
        private readonly DeviceChangeNotifier deviceChangeNotifier;
        private bool populating;

        public RecordingPanel()
        {
            InitializeComponent();
            Disposed += OnRecordingPanelDisposed;
            deviceChangeNotifier = new DeviceChangeNotifier(deviceEnumerator);
            deviceChangeNotifier.DevicesChanged += OnDevicesChanged;

            foreach (var option in ApiOptions)
            {
                comboRecordingApi.Items.Add(option);
            }

            comboBoxSampleRate.DataSource = StandardSampleRates.ToArray();
            comboBoxSampleRate.SelectedItem = 44100;

            comboBoxChannels.DataSource = new[] { "Mono", "Stereo" };
            comboBoxChannels.SelectedItem = "Stereo";

            outputFolder = Path.Combine(Path.GetTempPath(), "NAudioDemo");
            Directory.CreateDirectory(outputFolder);

            comboRecordingApi.SelectedIndexChanged += (s, a) => OnApiChanged();
            comboDevice.SelectedIndexChanged += (s, a) => OnDeviceChanged();
            checkBoxEventCallback.CheckedChanged += (s, a) => Cleanup();
            comboBoxSampleRate.SelectedIndexChanged += (s, a) => Cleanup();
            comboBoxChannels.SelectedIndexChanged += (s, a) => Cleanup();

            comboRecordingApi.SelectedIndex = 0; // triggers OnApiChanged -> populates devices
        }

        private ApiOption SelectedApi => (ApiOption)comboRecordingApi.SelectedItem;

        private void OnApiChanged()
        {
            Cleanup();
            PopulateDevices();
            checkBoxEventCallback.Visible = SelectedApi.SupportsEventCallback;
        }

        private void OnDeviceChanged()
        {
            if (populating) return;
            ApplyDefaultFormatFromDevice();
            Cleanup();
        }

        private void PopulateDevices()
        {
            populating = true;
            try
            {
                comboDevice.DataSource = null;
                comboDevice.Items.Clear();
                var api = SelectedApi;

                if (!api.IsWasapi)
                {
                    var devices = Enumerable.Range(-1, WaveIn.DeviceCount + 1)
                        .Select(n => new WaveInDeviceItem(n, WaveIn.GetCapabilities(n).ProductName))
                        .ToArray();
                    comboDevice.DisplayMember = nameof(WaveInDeviceItem.DisplayName);
                    comboDevice.DataSource = devices;
                    comboDevice.SelectedIndex = 0; // index 0 is device -1, the default
                }
                else
                {
                    var flow = api.IsLoopback ? DataFlow.Render : DataFlow.Capture;
                    var devices = deviceEnumerator.EnumerateAudioEndPoints(flow, DeviceState.Active).ToArray();
                    comboDevice.DisplayMember = nameof(MMDevice.FriendlyName);
                    comboDevice.DataSource = devices;
                    if (devices.Length > 0)
                    {
                        try
                        {
                            var defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(flow, Role.Console);
                            var index = Array.FindIndex(devices, d => d.ID == defaultDevice.ID);
                            comboDevice.SelectedIndex = Math.Max(0, index);
                        }
                        catch
                        {
                            comboDevice.SelectedIndex = 0;
                        }
                    }
                }
            }
            finally
            {
                populating = false;
            }
            OnDeviceChanged();
        }

        private void ApplyDefaultFormatFromDevice()
        {
            var api = SelectedApi;
            if (!api.IsWasapi || !(comboDevice.SelectedItem is MMDevice device))
            {
                return;
            }

            try
            {
                using var audioClient = device.CreateAudioClient();
                var mix = audioClient.MixFormat;
                var bestRate = StandardSampleRates
                    .OrderBy(r => Math.Abs(r - mix.SampleRate))
                    .ThenBy(r => r == mix.SampleRate ? 0 : 1)
                    .First();
                comboBoxSampleRate.SelectedItem = bestRate;
                comboBoxChannels.SelectedItem = mix.Channels >= 2 ? "Stereo" : "Mono";
            }
            catch
            {
                // Some devices may fail to activate; leave the current selection alone.
            }
        }

        private void OnButtonStartRecordingClick(object sender, EventArgs e)
        {
            // winmm-based devices do not always cope with being re-used across recordings.
            var api = SelectedApi;
            if (!api.IsWasapi)
            {
                Cleanup();
            }

            try
            {
                captureDevice ??= CreateCaptureDevice();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to start recording");
                return;
            }

            // A loopback render device cannot have its endpoint volume un-muted the way a
            // capture device can, so skip the forced un-mute when we're on a loopback API.
            if (api.IsWasapi && !api.IsLoopback && comboDevice.SelectedItem is MMDevice captureMmDevice)
            {
                try { captureMmDevice.AudioEndpointVolume.Mute = false; } catch { /* best effort */ }
            }

            outputFilename = BuildFileName(api);
            writer = new WaveFileWriter(Path.Combine(outputFolder, outputFilename), captureDevice.WaveFormat);
            captureDevice.StartRecording();
            SetControlStates(true);
        }

        private IWaveIn CreateCaptureDevice()
        {
            var api = SelectedApi;
            var sampleRate = (int)comboBoxSampleRate.SelectedItem;
            var channels = comboBoxChannels.SelectedIndex + 1;
            var requestedFormat = new WaveFormat(sampleRate, channels);

            IWaveIn device;
            switch (api.Api)
            {
                case RecordingApi.WaveIn:
                {
                    var item = (WaveInDeviceItem)comboDevice.SelectedItem;
                    device = new WaveIn { DeviceNumber = item.DeviceNumber, WaveFormat = requestedFormat };
                    break;
                }
                case RecordingApi.WaveInWindow:
                {
                    var item = (WaveInDeviceItem)comboDevice.SelectedItem;
                    device = new WaveInWindow { DeviceNumber = item.DeviceNumber, WaveFormat = requestedFormat };
                    break;
                }
                case RecordingApi.WasapiCaptureLegacy:
                {
                    var mm = (MMDevice)comboDevice.SelectedItem;
                    var cap = new WasapiCapture(mm, checkBoxEventCallback.Checked) { WaveFormat = requestedFormat };
                    device = cap;
                    break;
                }
                case RecordingApi.WasapiLoopbackLegacy:
                {
                    var mm = (MMDevice)comboDevice.SelectedItem;
                    var cap = new WasapiLoopbackCapture(mm) { WaveFormat = requestedFormat };
                    device = cap;
                    break;
                }
                case RecordingApi.WasapiRecorderCapture:
                {
                    var mm = (MMDevice)comboDevice.SelectedItem;
                    var builder = new WasapiRecorderBuilder()
                        .WithDevice(mm)
                        .WithFormat(requestedFormat);
                    _ = checkBoxEventCallback.Checked ? builder.WithEventSync() : builder.WithPollingSync();
                    device = new WasapiRecorderAdapter(builder.Build());
                    break;
                }
                case RecordingApi.WasapiRecorderLoopback:
                {
                    var mm = (MMDevice)comboDevice.SelectedItem;
                    // Shared-mode loopback does not reliably support event sync — force polling.
                    var builder = new WasapiRecorderBuilder()
                        .WithDevice(mm)
                        .WithLoopbackCapture()
                        .WithPollingSync()
                        .WithFormat(requestedFormat);
                    device = new WasapiRecorderAdapter(builder.Build());
                    break;
                }
                default:
                    throw new InvalidOperationException($"Unhandled recording API {api.Api}");
            }

            device.DataAvailable += OnDataAvailable;
            device.RecordingStopped += OnRecordingStopped;
            return device;
        }

        private string BuildFileName(ApiOption api)
        {
            var format = captureDevice?.WaveFormat;
            string rate, channels;
            if (format != null)
            {
                rate = $"{format.SampleRate / 1000}kHz";
                channels = format.Channels == 1 ? "mono" : "stereo";
            }
            else
            {
                rate = $"{((int)comboBoxSampleRate.SelectedItem) / 1000}kHz";
                channels = comboBoxChannels.SelectedIndex == 0 ? "mono" : "stereo";
            }
            var eventSuffix = api.SupportsEventCallback && checkBoxEventCallback.Checked ? " event" : string.Empty;
            return $"{api.FileNamePrefix} {rate} {channels}{eventSuffix} {DateTime.Now:yyyy-MM-dd HH-mm-ss}.wav";
        }

        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<StoppedEventArgs>(OnRecordingStopped), sender, e);
                return;
            }

            FinalizeWaveFile();
            progressBar1.Value = 0;
            if (e.Exception != null)
            {
                MessageBox.Show(
                    $"A problem was encountered during recording: {e.Exception.Message}",
                    "Recording Error");
            }
            if (!string.IsNullOrEmpty(outputFilename))
            {
                int newItemIndex = listBoxRecordings.Items.Add(outputFilename);
                listBoxRecordings.SelectedIndex = newItemIndex;
            }
            SetControlStates(false);
        }

        private void OnDevicesChanged()
        {
            // Preserve the current selection by ID (WASAPI path) or device number (WaveIn path)
            // across re-enumeration. If the selected device has been removed, fall back to the default.
            var api = SelectedApi;
            if (!api.IsWasapi)
            {
                var prev = (comboDevice.SelectedItem as WaveInDeviceItem)?.DeviceNumber;
                PopulateDevices();
                if (prev.HasValue)
                {
                    var idx = -1;
                    for (int i = 0; i < comboDevice.Items.Count; i++)
                    {
                        if (((WaveInDeviceItem)comboDevice.Items[i]).DeviceNumber == prev.Value) { idx = i; break; }
                    }
                    if (idx >= 0) comboDevice.SelectedIndex = idx;
                }
            }
            else
            {
                var prevId = (comboDevice.SelectedItem as MMDevice)?.ID;
                PopulateDevices();
                if (prevId != null && comboDevice.DataSource is MMDevice[] arr)
                {
                    var idx = Array.FindIndex(arr, d => d.ID == prevId);
                    if (idx >= 0) comboDevice.SelectedIndex = idx;
                }
            }
        }

        private void OnRecordingPanelDisposed(object sender, EventArgs e)
        {
            deviceChangeNotifier?.Dispose();
            Cleanup();
        }

        private void Cleanup()
        {
            if (captureDevice != null)
            {
                captureDevice.DataAvailable -= OnDataAvailable;
                captureDevice.RecordingStopped -= OnRecordingStopped;
                captureDevice.Dispose();
                captureDevice = null;
            }
            FinalizeWaveFile();
        }

        private void FinalizeWaveFile()
        {
            writer?.Dispose();
            writer = null;
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<WaveInEventArgs>(OnDataAvailable), sender, e);
                return;
            }

            if (writer == null) return;
            writer.Write(e.Buffer, 0, e.BytesRecorded);
            int secondsRecorded = (int)(writer.Length / writer.WaveFormat.AverageBytesPerSecond);
            if (secondsRecorded >= 30)
            {
                StopRecording();
            }
            else
            {
                progressBar1.Value = secondsRecorded;
            }
        }

        void StopRecording()
        {
            Debug.WriteLine("StopRecording");
            captureDevice?.StopRecording();
        }

        private void OnButtonStopRecordingClick(object sender, EventArgs e)
        {
            StopRecording();
        }

        private void OnButtonPlayClick(object sender, EventArgs e)
        {
            if (listBoxRecordings.SelectedItem != null)
            {
                ProcessHelper.ShellExecute(Path.Combine(outputFolder, (string)listBoxRecordings.SelectedItem));
            }
        }

        private void SetControlStates(bool isRecording)
        {
            groupBoxRecordingApi.Enabled = !isRecording;
            buttonStartRecording.Enabled = !isRecording;
            buttonStopRecording.Enabled = isRecording;
        }

        private void OnButtonDeleteClick(object sender, EventArgs e)
        {
            if (listBoxRecordings.SelectedItem != null)
            {
                try
                {
                    File.Delete(Path.Combine(outputFolder, (string)listBoxRecordings.SelectedItem));
                    listBoxRecordings.Items.Remove(listBoxRecordings.SelectedItem);
                    if (listBoxRecordings.Items.Count > 0)
                    {
                        listBoxRecordings.SelectedIndex = 0;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Could not delete recording");
                }
            }
        }

        private void OnOpenFolderClick(object sender, EventArgs e)
        {
            ProcessHelper.ShellExecute(outputFolder);
        }
    }

    public class RecordingPanelPlugin : INAudioDemoPlugin
    {
        public string Name => "WAV Recording";

        public Control CreatePanel()
        {
            return new RecordingPanel();
        }
    }
}
