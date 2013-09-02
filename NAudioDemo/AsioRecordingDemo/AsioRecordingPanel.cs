using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using System.ComponentModel.Composition;
using System.IO;
using System.Diagnostics;

namespace NAudioDemo
{
    public partial class AsioRecordingPanel : UserControl
    {
        private WaveFileWriter writer;
        private AsioOut asioOut;
        private string fileName;

        public AsioRecordingPanel()
        {
            InitializeComponent();
            this.Disposed += new EventHandler(AsioDirectPanel_Disposed);
            foreach(var device in AsioOut.GetDriverNames())
            {
                this.comboBoxAsioDevice.Items.Add(device);
            }
            if (this.comboBoxAsioDevice.Items.Count > 0)
            {
                this.comboBoxAsioDevice.SelectedIndex = 0;
            }
        }

        void AsioDirectPanel_Disposed(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (this.asioOut != null)
            {
                this.asioOut.Dispose();
                this.asioOut = null;
            }
            if (this.writer != null)
            {
                this.writer.Dispose();
                this.writer = null;
            }
        }

        private void buttonStart_Click(object sender, EventArgs args)
        {
            try
            {
                Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private int GetUserSpecifiedChannelOffset()
        {
            int channelOffset = 0;
            int.TryParse(textBoxChannelOffset.Text, out channelOffset);
            return channelOffset;
        }

        private int GetUserSpecifiedChannelCount()
        {
            int channelCount = 1;
            int.TryParse(textBoxChannelCount.Text, out channelCount);
            return channelCount;
        }

        private void Start()
        {
            // allow change device
            if (this.asioOut != null && 
                (this.asioOut.DriverName != comboBoxAsioDevice.Text || 
                this.asioOut.ChannelOffset != GetUserSpecifiedChannelOffset()))
            {
                this.asioOut.AudioAvailable -= asioOut_AudioAvailable;
                this.asioOut.Dispose();
                this.asioOut = null;
            }

            int recordChannelCount = GetUserSpecifiedChannelCount();
            
            // create device if necessary
            if (this.asioOut == null)
            {
                this.asioOut = new AsioOut(comboBoxAsioDevice.Text);
                this.asioOut.InputChannelOffset = GetUserSpecifiedChannelOffset();
                this.asioOut.InitRecordAndPlayback(null, recordChannelCount, 44100);
                this.asioOut.AudioAvailable += asioOut_AudioAvailable;
            }
            
            this.fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            this.writer = new WaveFileWriter(fileName, new WaveFormat(44100, recordChannelCount));
            this.asioOut.Play();
            this.timer1.Enabled = true;
            SetButtonStates();
        }

        void asioOut_AudioAvailable(object sender, AsioAudioAvailableEventArgs e)
        {
            var samples = e.GetAsInterleavedSamples();
            writer.WriteSamples(samples, 0, samples.Length);
        }

        private void SetButtonStates()
        {
            buttonStart.Enabled = asioOut != null && asioOut.PlaybackState != PlaybackState.Playing;
            buttonStop.Enabled = asioOut != null && asioOut.PlaybackState == PlaybackState.Playing;
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            this.asioOut.Stop();
            if (this.writer != null)
            {
                this.writer.Dispose();
                this.writer = null;
            }
            this.timer1.Enabled = false;
            SetButtonStates();
            int index = listBoxRecordings.Items.Add(fileName);
            listBoxRecordings.SelectedIndex = index;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (asioOut != null && asioOut.PlaybackState == PlaybackState.Playing && writer.Length > writer.WaveFormat.AverageBytesPerSecond * 30)
            {
                Stop();
            }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (listBoxRecordings.SelectedItem != null)
            {
                Process.Start((string)listBoxRecordings.SelectedItem);
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (listBoxRecordings.SelectedItem != null)
            {
                File.Delete((string)listBoxRecordings.SelectedItem);
                listBoxRecordings.Items.Remove(listBoxRecordings.SelectedItem);
            }
        }

        public string SelectedDeviceName { get { return (string)comboBoxAsioDevice.SelectedItem; } }

        private void buttonControlPanel_Click(object sender, EventArgs args)
        {
            try
            {
                using (var asio = new AsioOut(SelectedDeviceName))
                {
                    asio.ShowControlPanel();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }

    [Export(typeof(INAudioDemoPlugin))]
    public class AsioRecordingPanelPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "ASIO Recording"; }
        }

        public Control CreatePanel()
        {
            return new AsioRecordingPanel();
        }
    }
}
