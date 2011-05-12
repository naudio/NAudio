using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.Compression;
using System.ComponentModel.Composition;

namespace NAudioDemo
{
    public partial class AcmPanel : UserControl
    {
        public AcmPanel()
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
                EncodeFile();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error Encoding", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void buttonDecode_Click(object sender, EventArgs args)
        {
            try
            {
                DecodeFile();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error Decoding", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private WaveFormat GetTargetFormat(WaveFormat inputFormat)
        {
            WaveFormat outputFormat;
            string formatDescription;
            string formatTagDescription;
            AcmDriver.ShowFormatChooseDialog(
                this.Handle,
                "Select Compressed Format:",
                AcmFormatEnumFlags.Convert,
                inputFormat,
                out outputFormat,
                out formatDescription,
                out formatTagDescription);
            return outputFormat;
        
            /*
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
             */
        }

        private void EncodeFile()
        {
            string inputFileName = GetInputFileName("Select PCM WAV File to Encode");
            if (inputFileName == null)
                return;
            using (WaveFileReader reader = new WaveFileReader(inputFileName))
            {
                if (reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                {
                    MessageBox.Show("Please select a PCM WAV file to encode");
                    return;
                }
                WaveFormat targetFormat = GetTargetFormat(reader.WaveFormat);
                if (targetFormat == null)
                {
                    return;
                }
                string outputFileName = GetOutputFileName("Select Ouput File Name");
                if (outputFileName == null)
                {
                    return;
                }
                using (WaveStream convertedStream = new WaveFormatConversionStream(targetFormat, reader))
                {
                    WaveFileWriter.CreateWaveFile(outputFileName, convertedStream);
                }
                if (checkBoxAutoLaunchConvertedFile.Checked)
                {
                    System.Diagnostics.Process.Start(outputFileName);
                }
            }
        }
        private void DecodeFile()
        {
            string inputFileName = GetInputFileName("Select a compressed WAV File to decode");
            if (inputFileName == null)
                return;
            using (WaveFileReader reader = new WaveFileReader(inputFileName))
            {
                if (reader.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
                {
                    MessageBox.Show("Please select a compressed WAV file to decode");
                    return;
                }
                WaveFormat targetFormat = GetTargetFormat(reader.WaveFormat);
                if (targetFormat == null)
                {
                    return;
                }

                string outputFileName = GetOutputFileName("Select Output File Name");
                if (outputFileName == null)
                {
                    return;
                }
                using (WaveStream convertedStream = new WaveFormatConversionStream(targetFormat, reader))
                {
                    WaveFileWriter.CreateWaveFile(outputFileName, convertedStream);
                }
                if (checkBoxAutoLaunchConvertedFile.Checked)
                {
                    System.Diagnostics.Process.Start(outputFileName);
                }
            }
        }

        private string GetInputFileName(string title)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WAV File (*.wav)|*.wav";
            openFileDialog.Title = title;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

        private string GetOutputFileName(string title)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "WAV File (*.wav)|*.wav";
            saveFileDialog.Title = title;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                return saveFileDialog.FileName;
            }
            return null;
        }

        private void listBoxAcmDrivers_SelectedIndexChanged(object sender, EventArgs e)
        {
            AcmDriver driver = listBoxAcmDrivers.SelectedItem as AcmDriver;
            richTextBox1.Text = DescribeCodec(driver);
        }

        private void buttonChooseFormat_Click(object sender, EventArgs e)
        {
            WaveFormat selectedFormat;
            string selectedFormatDescription;
            string selectedFormatTagDescription;
            if(AcmDriver.ShowFormatChooseDialog(this.Handle,"Choose a WaveFormat",AcmFormatEnumFlags.None,
                null,out selectedFormat,
                out selectedFormatDescription, out selectedFormatTagDescription))
            {
                MessageBox.Show(String.Format("{0}\r\n{1}\r\n{2}",                    
                    selectedFormatDescription,
                    selectedFormatTagDescription,
                    selectedFormat));
            }
        }

        private string DescribeCodec(AcmDriver driver)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("Long Name: {0}\r\n", driver.LongName);
                builder.AppendFormat("Short Name: {0}\r\n", driver.ShortName);
                builder.AppendFormat("Driver ID: {0}\r\n", driver.DriverId);
                driver.Open();
                builder.AppendFormat("FormatTags:\r\n");
                foreach (AcmFormatTag formatTag in driver.FormatTags)
                {
                    builder.AppendFormat("===========================================\r\n");
                    builder.AppendFormat("Format Tag {0}: {1}\r\n", formatTag.FormatTagIndex, formatTag.FormatDescription);
                    builder.AppendFormat("   Standard Format Count: {0}\r\n", formatTag.StandardFormatsCount);
                    builder.AppendFormat("   Support Flags: {0}\r\n", formatTag.SupportFlags);
                    builder.AppendFormat("   Format Tag: {0}, Format Size: {1}\r\n", formatTag.FormatTag, formatTag.FormatSize);
                    builder.AppendFormat("   Formats:\r\n");
                    foreach (AcmFormat format in driver.GetFormats(formatTag))
                    {
                        builder.AppendFormat("   ===========================================\r\n");
                        builder.AppendFormat("   Format {0}: {1}\r\n", format.FormatIndex, format.FormatDescription);
                        builder.AppendFormat("      FormatTag: {0}, Support Flags: {1}\r\n", format.FormatTag, format.SupportFlags);
                        builder.AppendFormat("      WaveFormat: {0} {1}Hz Channels: {2} Bits: {3} Block Align: {4}, AverageBytesPerSecond: {5} ({6:0.0} kbps), Extra Size: {7}\r\n",
                            format.WaveFormat.Encoding, format.WaveFormat.SampleRate, format.WaveFormat.Channels,
                            format.WaveFormat.BitsPerSample, format.WaveFormat.BlockAlign, format.WaveFormat.AverageBytesPerSecond,
                            (format.WaveFormat.AverageBytesPerSecond * 8) / 1000.0,
                            format.WaveFormat.ExtraSize);
                        if (format.WaveFormat is WaveFormatExtraData && format.WaveFormat.ExtraSize > 0)
                        {
                            WaveFormatExtraData wfed = (WaveFormatExtraData)format.WaveFormat;
                            builder.Append("      Extra Bytes:\r\n      ");
                            for (int n = 0; n < format.WaveFormat.ExtraSize; n++)
                            {
                                builder.AppendFormat("{0:X2} ", wfed.ExtraData[n]);
                            }
                            builder.Append("\r\n");
                        }
                    }
                }
                driver.Close();
                return builder.ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
        
    }


    [Export(typeof(INAudioDemoPlugin))]
    public class AcmPanelPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "ACM Format Conversion"; }
        }

        public Control CreatePanel()
        {
            return new AcmPanel();
        }
    }
}