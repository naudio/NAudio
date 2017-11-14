using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudioDemo.RecordingDemo
{
    public partial class RecordingPanel : UserControl
    {
        private IWaveIn captureDevice;
        private WaveFileWriter writer;
        private string outputFilename;
        private readonly string outputFolder;

        public RecordingPanel()
        {
            InitializeComponent();
            Disposed += OnRecordingPanelDisposed;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                LoadWasapiDevicesCombo();
            }
            else
            {
                radioButtonWasapi.Enabled = false;
                comboWasapiDevices.Enabled = false;
                radioButtonWasapiLoopback.Enabled = false;
            }
            LoadWaveInDevicesCombo();
            comboBoxSampleRate.DataSource = new[] {8000, 16000, 22050, 32000, 44100, 48000};
            comboBoxSampleRate.SelectedIndex = 0;
            comboBoxChannels.DataSource = new[] { "Mono", "Stereo" };
            comboBoxChannels.SelectedIndex = 0;
            outputFolder = Path.Combine(Path.GetTempPath(), "NAudioDemo");
            Directory.CreateDirectory(outputFolder);

            // close the device if we change option only
            radioButtonWasapi.CheckedChanged += (s, a) => Cleanup();
            radioButtonWaveIn.CheckedChanged += (s, a) => Cleanup();
            radioButtonWasapiLoopback.CheckedChanged += (s, a) => Cleanup();
            checkBoxEventCallback.CheckedChanged += (s, a) => Cleanup();
            comboWaveInDevice.SelectedIndexChanged += (s, a) => Cleanup();
            comboBoxChannels.SelectedIndexChanged += (s, a) => Cleanup();
            comboWasapiDevices.SelectedIndexChanged += (s, a) => Cleanup();
            comboWasapiLoopbackDevices.SelectedIndexChanged += (s, a) => Cleanup();
        }

        void OnRecordingPanelDisposed(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void LoadWasapiDevicesCombo()
        {
            var deviceEnum = new MMDeviceEnumerator();
            var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

            comboWasapiDevices.DataSource = devices;
            comboWasapiDevices.DisplayMember = "FriendlyName";

            var renderDevices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
            comboWasapiLoopbackDevices.DataSource = renderDevices;
            comboWasapiLoopbackDevices.DisplayMember = "FriendlyName";

        }

        private void LoadWaveInDevicesCombo()
        {
            var devices = Enumerable.Range(-1, WaveIn.DeviceCount + 1).Select(n => WaveIn.GetCapabilities(n)).ToArray();

            comboWaveInDevice.DataSource = devices;
            comboWaveInDevice.DisplayMember = "ProductName";
        }

        private void OnButtonStartRecordingClick(object sender, EventArgs e)
        {
            if (radioButtonWaveIn.Checked)
                Cleanup(); // WaveIn is still unreliable in some circumstances to being reused

            if (captureDevice == null)
            {
                captureDevice = CreateWaveInDevice();
            }
            // Forcibly turn on the microphone (some programs (Skype) turn it off).
            var device = (MMDevice)comboWasapiDevices.SelectedItem;
            device.AudioEndpointVolume.Mute = false;

            outputFilename = GetFileName();
            writer = new WaveFileWriter(Path.Combine(outputFolder, outputFilename), captureDevice.WaveFormat);
            captureDevice.StartRecording();
            SetControlStates(true);
        }

        private string GetFileName()
        {
            var deviceName = captureDevice.GetType().Name;
            var sampleRate = $"{captureDevice.WaveFormat.SampleRate / 1000}kHz";
            var channels = captureDevice.WaveFormat.Channels == 1 ? "mono" : "stereo";

            return $"{deviceName} {sampleRate} {channels} {DateTime.Now:yyy-MM-dd HH-mm-ss}.wav";
        }

        private IWaveIn CreateWaveInDevice()
        {
            IWaveIn newWaveIn;
            if (radioButtonWaveIn.Checked)
            {
                var deviceNumber = comboWaveInDevice.SelectedIndex - 1;
                if (checkBoxEventCallback.Checked)
                {
                    newWaveIn = new WaveInEvent() { DeviceNumber = deviceNumber };
                }
                else
                {
                    newWaveIn = new WaveIn() { DeviceNumber = deviceNumber };
                }
                var sampleRate = (int)comboBoxSampleRate.SelectedItem;
                var channels = comboBoxChannels.SelectedIndex + 1;
                newWaveIn.WaveFormat = new WaveFormat(sampleRate, channels);
            }
            else if (radioButtonWasapi.Checked)
            {
                // can't set WaveFormat as WASAPI doesn't support SRC
                var device = (MMDevice) comboWasapiDevices.SelectedItem;
                newWaveIn = new WasapiCapture(device);
            }
            else
            {
                // can't set WaveFormat as WASAPI doesn't support SRC
                var device = (MMDevice)comboWasapiLoopbackDevices.SelectedItem;
                newWaveIn = new WasapiLoopbackCapture(device);
            }
            newWaveIn.DataAvailable += OnDataAvailable;
            newWaveIn.RecordingStopped += OnRecordingStopped;
            return newWaveIn;
        }

        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<StoppedEventArgs>(OnRecordingStopped), sender, e);
            }
            else
            {
                FinalizeWaveFile();
                progressBar1.Value = 0;
                if (e.Exception != null)
                {
                    MessageBox.Show(String.Format("A problem was encountered during recording {0}",
                                                  e.Exception.Message));
                }
                int newItemIndex = listBoxRecordings.Items.Add(outputFilename);
                listBoxRecordings.SelectedIndex = newItemIndex;
                SetControlStates(false);
            }
        }

        private void Cleanup()
        {
            if (captureDevice != null)
            {
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
                //Debug.WriteLine("Data Available");
                BeginInvoke(new EventHandler<WaveInEventArgs>(OnDataAvailable), sender, e);
            }
            else
            {
                //Debug.WriteLine("Flushing Data Available");
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
                Process.Start(Path.Combine(outputFolder, (string)listBoxRecordings.SelectedItem));
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
            Process.Start(outputFolder);
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
