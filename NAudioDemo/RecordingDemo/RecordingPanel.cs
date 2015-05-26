using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using System.ComponentModel.Composition;

namespace NAudioDemo
{
    public partial class RecordingPanel : UserControl
    {
        private IWaveIn waveIn;
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
            outputFolder = Path.Combine(Path.GetTempPath(), "NAudioDemo");
            Directory.CreateDirectory(outputFolder);

            // close the device if we change option only
            radioButtonWasapi.CheckedChanged += (s, a) => Cleanup();
            radioButtonWaveIn.CheckedChanged += (s, a) => Cleanup();
            radioButtonWaveInEvent.CheckedChanged += (s, a) => Cleanup();
            radioButtonWasapiLoopback.CheckedChanged += (s, a) => Cleanup();
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
        }

        private void OnButtonStartRecordingClick(object sender, EventArgs e)
        {
            if (radioButtonWaveIn.Checked)
                Cleanup(); // WaveIn is still unreliable in some circumstances to being reused

            if (waveIn == null)
            {
                CreateWaveInDevice();
            }
            // Forcibly turn on the microphone (some programs (Skype) turn it off).
            var device = (MMDevice)comboWasapiDevices.SelectedItem;
            device.AudioEndpointVolume.Mute = false;

            outputFilename = String.Format("NAudioDemo {0:yyy-MM-dd HH-mm-ss}.wav", DateTime.Now);
            writer = new WaveFileWriter(Path.Combine(outputFolder, outputFilename), waveIn.WaveFormat);
            waveIn.StartRecording();
            SetControlStates(true);
        }

        private void CreateWaveInDevice()
        {
            if (radioButtonWaveIn.Checked)
            {
                waveIn = new WaveIn();
                waveIn.WaveFormat = new WaveFormat(8000, 1);
            }
            else if (radioButtonWaveInEvent.Checked)
            {
                waveIn = new WaveInEvent();
                waveIn.WaveFormat = new WaveFormat(8000, 1);
            }
            else if (radioButtonWasapi.Checked)
            {
                // can't set WaveFormat as WASAPI doesn't support SRC
                var device = (MMDevice) comboWasapiDevices.SelectedItem;
                waveIn = new WasapiCapture(device);
            }
            else
            {
                // can't set WaveFormat as WASAPI doesn't support SRC
                waveIn = new WasapiLoopbackCapture();
            }
            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;
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
            if (waveIn != null)
            {
                waveIn.Dispose();
                waveIn = null;
            }
            FinalizeWaveFile();
        }

        private void FinalizeWaveFile()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.InvokeRequired)
            {
                //Debug.WriteLine("Data Available");
                this.BeginInvoke(new EventHandler<WaveInEventArgs>(OnDataAvailable), sender, e);
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
            if (waveIn != null) waveIn.StopRecording();
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

    [Export(typeof(INAudioDemoPlugin))]
    public class RecordingPanelPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "WAV Recording"; }
        }

        public Control CreatePanel()
        {
            return new RecordingPanel();
        }
    }
}
