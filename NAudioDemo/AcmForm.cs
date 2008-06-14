using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;

namespace NAudioDemo
{
    public partial class AcmForm : Form
    {
        public AcmForm()
        {
            InitializeComponent();
        }

        private void AcmForm_Load(object sender, EventArgs e)
        {
            foreach (AcmDriver driver in AcmDriver.EnumerateAcmDrivers())
            {
                listBoxAcmDrivers.Items.Add(driver);
            }
        }

        private void buttonEncode_Click(object sender, EventArgs args)
        {
            try
            {
                ConvertFile();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error Encoding", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private WaveFormat GetTargetFormat(WaveFormat inputFormat)
        {
            if (radioButtonMuLaw.Checked)
            {
                return WaveFormat.CreateCustomFormat(
                    WaveFormatEncoding.MuLaw,
                    inputFormat.Channels,
                    inputFormat.SampleRate,
                    inputFormat.SampleRate * inputFormat.Channels,
                    1, 8);
            }
            else if (radioButtonALaw.Checked)
            {
                return WaveFormat.CreateCustomFormat(
                    WaveFormatEncoding.ALaw,
                    inputFormat.Channels,
                    inputFormat.SampleRate,
                    inputFormat.SampleRate * inputFormat.Channels,
                    1, 8);
            }
            else if (radioButtonGsm610.Checked)
            {
                return WaveFormat.CreateCustomFormat(
                    WaveFormatEncoding.Gsm610,
                    1,
                    8000,
                    1625,
                    65, 0);
            }
            else if (radioButtonAdpcm.Checked)
            {
                return new WaveFormatAdpcm(8000, 1);
            }
            throw new NotImplementedException("Not implemented yet!");
        }

        private void ConvertFile()
        {
            string inputFileName = GetInputFileName();
            if (inputFileName == null)
                return;
            using (WaveFileReader reader = new WaveFileReader(inputFileName))
            {
                WaveFormat targetFormat = GetTargetFormat(reader.WaveFormat);
                string outputFileName = GetOutputFileName();
                if (outputFileName == null)
                {
                    return;
                }
                WaveStream convertedStream = new WaveFormatConversionStream(targetFormat, reader);                
                WaveFileWriter.CreateWaveFile(outputFileName, convertedStream);
                if (checkBoxAutoLaunchEncodedFile.Checked)
                {
                    System.Diagnostics.Process.Start(outputFileName);
                }
            }
        }

        private string GetInputFileName()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WAV File (*.wav)|*.wav";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

        private string GetOutputFileName()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "WAV File (*.wav)|*.wav";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                return saveFileDialog.FileName;
            }
            return null;
        }
    }
}