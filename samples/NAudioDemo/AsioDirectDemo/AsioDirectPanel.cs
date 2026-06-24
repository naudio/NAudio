using System;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;

namespace NAudioDemo.AsioDirectDemo
{
    public partial class AsioDirectPanel : UserControl
    {
        private AudioFileReader reader;
        private AsioOut asioOut;        

        public AsioDirectPanel()
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
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
        }

        private void OnButtonSelectFileClick(object sender, EventArgs e)
        {
            Cleanup();
            var ofd = new OpenFileDialog();
            ofd.Filter = "Audio files|*.wav;*.mp3";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                reader = new AudioFileReader(ofd.FileName);
            }
        }

        private void OnButtonPlayClick(object sender, EventArgs args)
        {
            try
            {
                Play();
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

        private void Play()
        {
            // allow change device
            if (asioOut != null && 
                (asioOut.DriverName != comboBoxAsioDevice.Text || 
                asioOut.ChannelOffset != GetUserSpecifiedChannelOffset()))
            {
                asioOut.Dispose();
                asioOut = null;
            }
            
            // create device if necessary
            if (asioOut == null)
            {
                asioOut = new AsioOut(comboBoxAsioDevice.Text);
                asioOut.ChannelOffset = GetUserSpecifiedChannelOffset();
                asioOut.Init(reader);
            }
            
            reader.Position = 0;
            asioOut.Play();
            timer1.Enabled = true;
            SetButtonStates();
        }

        private void SetButtonStates()
        {
            buttonPlay.Enabled = asioOut != null && asioOut.PlaybackState != PlaybackState.Playing;
            buttonStop.Enabled = asioOut != null && asioOut.PlaybackState == PlaybackState.Playing;            
        }

        private void OnButtonStopClick(object sender, EventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            asioOut.Stop();
            timer1.Enabled = false;
            SetButtonStates();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (asioOut != null && asioOut.PlaybackState == PlaybackState.Playing && reader.Position >= reader.Length)
            {
                Stop();
            }
        }        
    }

    public class AsioDirectPanelPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "ASIO Direct Playback"; }
        }

        public Control CreatePanel()
        {
            return new AsioDirectPanel();
        }
    }
}
