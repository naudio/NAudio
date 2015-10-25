using System;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.Compression;

namespace NAudioDemo.AcmDemo
{
    public partial class AcmPanel : UserControl
    {
        public AcmPanel()
        {
            InitializeComponent();
        }

        private void OnAcmFormLoad(object sender, EventArgs e)
        {
            RefreshDriversList();
        }

        private void RefreshDriversList()
        {
            listBoxAcmDrivers.Items.Clear();
            foreach (var driver in AcmDriver.EnumerateAcmDrivers())
            {
                listBoxAcmDrivers.Items.Add(driver);
            }
        }

        private void OnButtonEncodeClick(object sender, EventArgs args)
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
                Handle,
                "Select Compressed Format:",
                AcmFormatEnumFlags.Convert,
                inputFormat,
                out outputFormat,
                out formatDescription,
                out formatTagDescription);
            return outputFormat;
        }

        private void EncodeFile()
        {
            string inputFileName = GetInputFileName("Select PCM WAV File to Encode");
            if (inputFileName == null)
                return;
            using (var reader = new WaveFileReader(inputFileName))
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
            using (var reader = new WaveFileReader(inputFileName))
            {
                if (reader.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
                {
                    MessageBox.Show("Please select a compressed WAV file to decode");
                    return;
                }
                string outputFileName = GetOutputFileName("Select Output File Name");
                if (outputFileName == null)
                {
                    return;
                }
                using (var convertedStream = WaveFormatConversionStream.CreatePcmStream(reader))
                {
                    WaveFileWriter.CreateWaveFile(outputFileName, convertedStream);
                }
                if (checkBoxAutoLaunchConvertedFile.Checked)
                {
                    System.Diagnostics.Process.Start(outputFileName);
                }
            }
        }

        private string SelectFileToOpen(string title, string filter)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = filter;
            openFileDialog.Title = title;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

        private string GetInputFileName(string title)
        {
            return SelectFileToOpen(title, "WAV File (*.wav)|*.wav");
        }

        private string GetOutputFileName(string title)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "WAV File (*.wav)|*.wav";
            saveFileDialog.Title = title;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                return saveFileDialog.FileName;
            }
            return null;
        }

        private void OnListBoxAcmDriversSelectedIndexChanged(object sender, EventArgs e)
        {
            var driver = listBoxAcmDrivers.SelectedItem as AcmDriver;
            richTextBox1.Text = DescribeCodec(driver);
        }

        private void OnButtonChooseFormatClick(object sender, EventArgs e)
        {
            WaveFormat selectedFormat;
            string selectedFormatDescription;
            string selectedFormatTagDescription;
            if(AcmDriver.ShowFormatChooseDialog(Handle,"Choose a WaveFormat",AcmFormatEnumFlags.None,
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
                driver.Open();
                var builder = new StringBuilder();
                DescribeAcmDriver(driver, builder);
                foreach (var formatTag in driver.FormatTags)
                {
                    DescribeAcmFormatTag(builder, formatTag);
                    foreach (var format in driver.GetFormats(formatTag))
                    {
                        DescribeAcmFormat(builder, format);
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

        private static void DescribeAcmDriver(AcmDriver driver, StringBuilder builder)
        {
            builder.AppendFormat("Long Name: {0}\r\n", driver.LongName);
            builder.AppendFormat("Short Name: {0}\r\n", driver.ShortName);
            builder.AppendFormat("Driver ID: {0}\r\n", driver.DriverId);
            builder.AppendFormat("FormatTags:\r\n");
        }

        private static void DescribeAcmFormatTag(StringBuilder builder, AcmFormatTag formatTag)
        {
            builder.AppendFormat("===========================================\r\n");
            builder.AppendFormat("Format Tag {0}: {1}\r\n", formatTag.FormatTagIndex, formatTag.FormatDescription);
            builder.AppendFormat("   Standard Format Count: {0}\r\n", formatTag.StandardFormatsCount);
            builder.AppendFormat("   Support Flags: {0}\r\n", formatTag.SupportFlags);
            builder.AppendFormat("   Format Tag: {0}, Format Size: {1}\r\n", formatTag.FormatTag, formatTag.FormatSize);
            builder.AppendFormat("   Formats:\r\n");
        }

        private static void DescribeAcmFormat(StringBuilder builder, AcmFormat format)
        {
            builder.AppendFormat("   ===========================================\r\n");
            builder.AppendFormat("   Format {0}: {1}\r\n", format.FormatIndex, format.FormatDescription);
            builder.AppendFormat("      FormatTag: {0}, Support Flags: {1}\r\n", format.FormatTag, format.SupportFlags);
            builder.AppendFormat(
                "      WaveFormat: {0} {1}Hz Channels: {2} Bits: {3} Block Align: {4}, AverageBytesPerSecond: {5} ({6:0.0} kbps), Extra Size: {7}\r\n",
                format.WaveFormat.Encoding, format.WaveFormat.SampleRate, format.WaveFormat.Channels,
                format.WaveFormat.BitsPerSample, format.WaveFormat.BlockAlign, format.WaveFormat.AverageBytesPerSecond,
                (format.WaveFormat.AverageBytesPerSecond*8)/1000.0,
                format.WaveFormat.ExtraSize);
            if (format.WaveFormat is WaveFormatExtraData && format.WaveFormat.ExtraSize > 0)
            {
                var wfed = (WaveFormatExtraData) format.WaveFormat;
                builder.Append("      Extra Bytes:\r\n      ");
                for (int n = 0; n < format.WaveFormat.ExtraSize; n++)
                {
                    builder.AppendFormat("{0:X2} ", wfed.ExtraData[n]);
                }
                builder.Append("\r\n");
            }
        }

        private void buttonAdd_Click(object sender, EventArgs args)
        {
            var codecFile = SelectFileToOpen("Select file to open", "ACM Codecs|*.acm;*.dll");
            if (codecFile != null)
            {
                try
                {
                    var driver = AcmDriver.AddLocalDriver(codecFile);
                    listBoxAcmDrivers.Items.Add(driver);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error Adding Driver", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }

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