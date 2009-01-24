using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudioDemo
{
    public partial class RecordingForm : Form
    {
        WaveIn waveInStream;
        WaveFileWriter writer;
        string outputFilename;

        public RecordingForm()
        {
            InitializeComponent();
        }

        private void buttonStartRecording_Click(object sender, EventArgs e)
        {
            if (waveInStream == null)
            {
                if(outputFilename == null)
                {
                    buttonSelectOutputFile_Click(sender, e);
                }
                if(outputFilename == null)
                {
                    return;
                }
                waveInStream = new WaveIn(8000, 1);                
                writer = new WaveFileWriter(outputFilename, waveInStream.WaveFormat);

                waveInStream.DataAvailable += new EventHandler<WaveInEventArgs>(waveInStream_DataAvailable);
                waveInStream.StartRecording();
                buttonStartRecording.Enabled = false;                                
            }
        }

        void waveInStream_DataAvailable(object sender, WaveInEventArgs e)
        {
            writer.WriteData(e.Buffer, 0, e.BytesRecorded);
            int secondsRecorded = (int) (writer.Length / writer.WaveFormat.AverageBytesPerSecond);
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
            waveInStream.StopRecording();
            waveInStream.Dispose();
            waveInStream = null;
            writer.Close();
            writer = null;
            buttonStartRecording.Enabled = true;
            progressBar1.Value = 0;
            if (checkBoxAutoPlay.Checked)
            {
                Process.Start(outputFilename);
            }
        }

        private void buttonStopRecording_Click(object sender, EventArgs e)
        {
            if (waveInStream != null)
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
    }
}
