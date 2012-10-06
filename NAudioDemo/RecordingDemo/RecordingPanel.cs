using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

namespace NAudioDemo
{
    public partial class RecordingPanel : UserControl
    {
        IWaveIn waveIn;
        WaveFileWriter writer;
        string outputFilename;

        public RecordingPanel()
        {
            InitializeComponent();
            if (System.Environment.OSVersion.Version.Major >= 6)
            {
                LoadWasapiDevicesCombo();
            }
            else
            {
                radioButtonWasapi.Enabled = false;
                comboWasapiDevices.Enabled = false;
                radioButtonWasapiLoopback.Enabled = false;
            }
        }

        private void LoadWasapiDevicesCombo()
        {
            MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
            MMDeviceCollection deviceCol = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            Collection<MMDevice> devices = new Collection<MMDevice>();

            foreach (MMDevice device in deviceCol)
            {
                devices.Add(device);
            }

            this.comboWasapiDevices.DataSource = devices;
            this.comboWasapiDevices.DisplayMember = "FriendlyName";
        }

        private void buttonStartRecording_Click(object sender, EventArgs e)
        {
            if (waveIn == null)
            {
                if(outputFilename == null)
                {
                    buttonSelectOutputFile_Click(sender, e);
                }
                if(outputFilename == null)
                {
                    return;
                }
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
                    var device = (MMDevice)comboWasapiDevices.SelectedItem;
                    waveIn = new WasapiCapture(device);
                }
                else
                {
                    // can't set WaveFormat as WASAPI doesn't support SRC
                    waveIn = new WasapiLoopbackCapture();
                }
                
                writer = new WaveFileWriter(outputFilename, waveIn.WaveFormat);

                waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(waveIn_DataAvailable);
                waveIn.RecordingStopped += waveIn_RecordingStopped;
                waveIn.StartRecording();
                buttonStartRecording.Enabled = false;
            }
        }

        void waveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<StoppedEventArgs>(waveIn_RecordingStopped), sender, e);
            }
            else
            {
                waveIn.Dispose();
                waveIn = null;
                writer.Close();
                writer = null;
                buttonStartRecording.Enabled = true;
                progressBar1.Value = 0;
                if (e.Exception != null)
                {
                    MessageBox.Show(String.Format("A problem was encountered during recording {0}", e.Exception.Message));
                }
                if (checkBoxAutoPlay.Checked)
                {
                    Process.Start(outputFilename);
                }
            }
        }

        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.InvokeRequired)
            {
                //Debug.WriteLine("Data Available");
                this.BeginInvoke(new EventHandler<WaveInEventArgs>(waveIn_DataAvailable), sender, e);
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
            waveIn.StopRecording();
        }

        private void buttonStopRecording_Click(object sender, EventArgs e)
        {
            if (waveIn != null)
            {
                StopRecording();
            }
        }

        private void buttonSelectOutputFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Select output file:";
            saveFileDialog.Filter = "WAV Files (*.wav)|*.wav";
            saveFileDialog.FileName = outputFilename;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                outputFilename = saveFileDialog.FileName;
            }
        }

        private void RecordingPanel_Load(object sender, EventArgs e)
        {

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
