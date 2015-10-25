using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;

namespace NAudioDemo.AsioRecordingDemo
{
    public partial class AsioRecordingPanel : UserControl
    {
        private WaveFileWriter writer;
        private AsioOut asioOut;
        private string fileName;

        public AsioRecordingPanel()
        {
            InitializeComponent();
            Disposed += OnAsioDirectPanelDisposed;
            foreach(var device in AsioOut.GetDriverNames())
            {
                comboBoxAsioDevice.Items.Add(device);
            }
            if (comboBoxAsioDevice.Items.Count > 0)
            {
                comboBoxAsioDevice.SelectedIndex = 0;
            }
        }

        void OnAsioDirectPanelDisposed(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (asioOut != null)
            {
                asioOut.Dispose();
                asioOut = null;
            }
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
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
            }
        }

        private int GetUserSpecifiedChannelOffset()
        {
            int channelOffset;
            int.TryParse(textBoxChannelOffset.Text, out channelOffset);
            return channelOffset;
        }

        private int GetUserSpecifiedChannelCount()
        {
            int channelCount;
            return int.TryParse(textBoxChannelCount.Text, out channelCount) ? channelCount : 1;
        }

        private void Start()
        {
            // allow change device
            if (asioOut != null && 
                (asioOut.DriverName != comboBoxAsioDevice.Text || 
                asioOut.ChannelOffset != GetUserSpecifiedChannelOffset()))
            {
                asioOut.AudioAvailable -= OnAsioOutAudioAvailable;
                asioOut.Dispose();
                asioOut = null;
            }

            int recordChannelCount = GetUserSpecifiedChannelCount();
            
            // create device if necessary
            if (asioOut == null)
            {
                asioOut = new AsioOut(comboBoxAsioDevice.Text);
                asioOut.InputChannelOffset = GetUserSpecifiedChannelOffset();
                asioOut.InitRecordAndPlayback(null, recordChannelCount, 44100);
                asioOut.AudioAvailable += OnAsioOutAudioAvailable;
            }
            
            fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav");
            writer = new WaveFileWriter(fileName, new WaveFormat(44100, recordChannelCount));
            asioOut.Play();
            timer1.Enabled = true;
            SetButtonStates();
        }

        void OnAsioOutAudioAvailable(object sender, AsioAudioAvailableEventArgs e)
        {
#pragma warning disable 618
            var samples = e.GetAsInterleavedSamples();
#pragma warning restore 618
            writer.WriteSamples(samples, 0, samples.Length);
        }

        private void SetButtonStates()
        {
            buttonStart.Enabled = asioOut != null && asioOut.PlaybackState != PlaybackState.Playing;
            buttonStop.Enabled = asioOut != null && asioOut.PlaybackState == PlaybackState.Playing;
        }

        private void OnButtonStopClick(object sender, EventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            asioOut.Stop();
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
            timer1.Enabled = false;
            SetButtonStates();
            int index = listBoxRecordings.Items.Add(fileName);
            listBoxRecordings.SelectedIndex = index;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (asioOut != null && asioOut.PlaybackState == PlaybackState.Playing && writer.Length > writer.WaveFormat.AverageBytesPerSecond * 30)
            {
                Stop();
            }
        }

        private void OnButtonPlayClick(object sender, EventArgs e)
        {
            if (listBoxRecordings.SelectedItem != null)
            {
                Process.Start((string)listBoxRecordings.SelectedItem);
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

        public string SelectedDeviceName { get { return (string)comboBoxAsioDevice.SelectedItem; } }

        private void OnButtonControlPanelClick(object sender, EventArgs args)
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
