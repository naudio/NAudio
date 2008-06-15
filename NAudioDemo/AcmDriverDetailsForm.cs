using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave.Compression;

namespace NAudioDemo
{
    public partial class AcmDriverDetailsForm : Form
    {
        public AcmDriverDetailsForm(AcmDriver driver)
        {
            InitializeComponent();
            AddLine("Long Name: {0}", driver.LongName);
            AddLine("Short Name: {0}", driver.ShortName);
            AddLine("Driver ID: {0}", driver.DriverId);
            driver.Open();
            AddLine("FormatTags:", driver.FormatTags);
            foreach (AcmFormatTag formatTag in driver.FormatTags)
            {
                AddLine("{0}: {1}", formatTag.FormatTagIndex, formatTag.FormatDescription);
                AddLine("   Standard Format Count: {0}", formatTag.StandardFormatsCount);
                AddLine("   Support Flags: {0}", formatTag.SupportFlags);
                AddLine("   Format Tag: {0}, Format Size: {1}", formatTag.FormatTag, formatTag.FormatSize);
                AddLine("   Formats:");
                foreach (AcmFormat format in driver.GetFormats(formatTag))
                {
                    AddLine("   {0}: {1}", format.FormatIndex, format.FormatDescription);
                    AddLine("      FormatTag: {0}, Support Flags: {1}", format.FormatTag, format.SupportFlags);
                    AddLine("      WaveFormat: {0} {1}Hz {2} Channels {3} bits, Block Align: {4}, AverageBytesPerSecond: {5}, Extra Size: {6}",
                        format.WaveFormat.Encoding, format.WaveFormat.SampleRate, format.WaveFormat.Channels,
                        format.WaveFormat.BitsPerSample, format.WaveFormat.BlockAlign, format.WaveFormat.AverageBytesPerSecond,
                        format.WaveFormat.ExtraSize);                
                }
            }
            driver.Close();



            
        }

        private void AddLine(string formatString, params object[] args)
        {
            richTextBox1.AppendText(String.Format(formatString,args) + "\r\n");
        }
    }
}
