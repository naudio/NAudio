using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;
using NAudioDemo.Utils;

namespace NAudioDemo.AsioRecordingDemo
{
    public partial class AsioRecordingPanel : UserControl
    {
        private WaveFileWriter writer;
        private AsioDevice device;
        private string fileName;

        public AsioRecordingPanel()
        {
            InitializeComponent();
            Disposed += OnAsioDirectPanelDisposed;
            foreach (var driverName in AsioDevice.GetDriverNames())
            {
                comboBoxAsioDevice.Items.Add(driverName);
            }
            if (comboBoxAsioDevice.Items.Count > 0)
            {
                // Don't trigger the probe during construction — some ASIO drivers can't be opened
                // while the form is still initializing. Defer it to Load.
                comboBoxAsioDevice.SelectedIndexChanged -= OnDeviceChanged;
                comboBoxAsioDevice.SelectedIndex = 0;
                comboBoxAsioDevice.SelectedIndexChanged += OnDeviceChanged;
            }
            Load += (_, _) => RefreshChannelList();
        }

        void OnAsioDirectPanelDisposed(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (device != null)
            {
                device.Dispose();
                device = null;
            }
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        private void OnDeviceChanged(object sender, EventArgs e)
        {
            RefreshChannelList();
        }

        private void RefreshChannelList()
        {
            checkedListBoxChannels.Items.Clear();
            if (comboBoxAsioDevice.SelectedItem is not string driverName)
            {
                labelHelp.Text = "No ASIO drivers installed.";
                return;
            }

            try
            {
                using var probe = AsioDevice.Open(driverName);
                int inputs = probe.Capabilities.NbInputChannels;
                for (int i = 0; i < inputs; i++)
                {
                    var info = probe.Capabilities.InputChannelInfos[i];
                    var label = string.IsNullOrEmpty(info.name)
                        ? $"Channel {i} ({info.type})"
                        : $"Channel {i}: {info.name} ({info.type})";
                    checkedListBoxChannels.Items.Add(label);
                }
                if (inputs > 0) checkedListBoxChannels.SetItemChecked(0, true);
                labelHelp.Text = $"{inputs} input channel(s) — tick the ones you want to record. Non-contiguous selection (e.g. 0 and 3) is supported.";
            }
            catch (Exception ex)
            {
                // Don't popup — many ASIO drivers fail to open while another app is using them,
                // and a blocking messagebox during panel load is disruptive. Show an inline hint;
                // Start() will surface a proper error if the driver is still unavailable.
                labelHelp.Text = $"Could not probe '{driverName}': {ex.Message}. Close other ASIO apps and re-select the driver.";
            }
        }

        private void OnSelectAllClick(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxChannels.Items.Count; i++)
                checkedListBoxChannels.SetItemChecked(i, true);
        }

        private void OnSelectNoneClick(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxChannels.Items.Count; i++)
                checkedListBoxChannels.SetItemChecked(i, false);
        }

        private int[] GetSelectedChannels()
        {
            return checkedListBoxChannels.CheckedIndices.Cast<int>().ToArray();
        }

        private void OnButtonStartClick(object sender, EventArgs args)
        {
            try
            {
                Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Cleanup();
                SetButtonStates();
            }
        }

        private void Start()
        {
            var channels = GetSelectedChannels();
            if (channels.Length == 0)
            {
                MessageBox.Show("Tick at least one input channel to record.");
                return;
            }

            if (device != null && device.DriverName != comboBoxAsioDevice.Text)
            {
                Cleanup();
            }

            device ??= AsioDevice.Open(comboBoxAsioDevice.Text);

            if (device.State == AsioDeviceState.Unconfigured)
            {
                device.InitRecording(new AsioRecordingOptions
                {
                    InputChannels = channels,
                    SampleRate = device.CurrentSampleRate,
                });
                device.AudioCaptured += OnAudioCaptured;
                device.Stopped += OnStopped;
            }

            var format = WaveFormat.CreateIeeeFloatWaveFormat(device.CurrentSampleRate, channels.Length);
            fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav");
            writer = new WaveFileWriter(fileName, format);

            device.Start();
            timer1.Enabled = true;
            SetButtonStates();
        }

        private void OnAudioCaptured(object sender, AsioAudioCapturedEventArgs e)
        {
            // Interleave per-channel float spans into the WAV writer.
            var localWriter = writer;
            if (localWriter == null) return;
            for (int frame = 0; frame < e.Frames; frame++)
            {
                for (int ch = 0; ch < e.ChannelCount; ch++)
                {
                    localWriter.WriteSample(e.GetChannel(ch)[frame]);
                }
            }
        }

        private void OnStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                MessageBox.Show($"Recording stopped with error: {e.Exception.Message}");
            }
        }

        private void SetButtonStates()
        {
            bool running = device != null && device.State == AsioDeviceState.Running;
            buttonStart.Enabled = !running;
            buttonStop.Enabled = running;
            comboBoxAsioDevice.Enabled = !running;
            checkedListBoxChannels.Enabled = !running;
            buttonSelectAll.Enabled = !running;
            buttonSelectNone.Enabled = !running;
        }

        private void OnButtonStopClick(object sender, EventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            if (device != null && device.State == AsioDeviceState.Running)
            {
                device.Stop();
            }
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
            timer1.Enabled = false;
            SetButtonStates();

            if (fileName != null && File.Exists(fileName))
            {
                int index = listBoxRecordings.Items.Add(fileName);
                listBoxRecordings.SelectedIndex = index;
            }

            // Drop the AsioDevice so a new Init* can happen next Start — AsioDevice is single-config.
            Cleanup();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            // Auto-stop at 30 seconds to match the old behaviour.
            if (writer != null && writer.Length > writer.WaveFormat.AverageBytesPerSecond * 30)
            {
                Stop();
            }
        }

        private void OnButtonPlayClick(object sender, EventArgs e)
        {
            if (listBoxRecordings.SelectedItem != null)
            {
                ProcessHelper.ShellExecute((string)listBoxRecordings.SelectedItem);
            }
        }

        private void OnButtonDeleteClick(object sender, EventArgs e)
        {
            if (listBoxRecordings.SelectedItem != null)
            {
                File.Delete((string)listBoxRecordings.SelectedItem);
                listBoxRecordings.Items.Remove(listBoxRecordings.SelectedItem);
            }
        }

        public string SelectedDeviceName => (string)comboBoxAsioDevice.SelectedItem;

        private void OnButtonControlPanelClick(object sender, EventArgs args)
        {
            try
            {
                using var probe = AsioDevice.Open(SelectedDeviceName);
                probe.ShowControlPanel();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }

    public class AsioRecordingPanelPlugin : INAudioDemoPlugin
    {
        public string Name => "ASIO Recording";

        public Control CreatePanel() => new AsioRecordingPanel();
    }
}
