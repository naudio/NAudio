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

namespace NAudioDemo
{
    public partial class AsioDirectPanel : UserControl
    {
        private AudioFileReader reader;
        private AsioOut asioOut;        

        public AsioDirectPanel()
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
            if (this.reader != null)
            {
                this.reader.Dispose();
                this.reader = null;
            }
        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            Cleanup();
            var ofd = new OpenFileDialog();
            ofd.Filter = "Audio files|*.wav;*.mp3";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.reader = new AudioFileReader(ofd.FileName);
            }
        }

        private void buttonPlay_Click(object sender, EventArgs args)
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
            int channelOffset = 0;
            int.TryParse(textBoxChannelOffset.Text, out channelOffset);
            return channelOffset;
        }

        private void Play()
        {
            // allow change device
            if (this.asioOut != null && 
                (this.asioOut.DriverName != comboBoxAsioDevice.Text || 
                this.asioOut.ChannelOffset != GetUserSpecifiedChannelOffset()))
            {
                this.asioOut.Dispose();
                this.asioOut = null;
            }
            
            // create device if necessary
            if (this.asioOut == null)
            {
                this.asioOut = new AsioOut(comboBoxAsioDevice.Text);
                this.asioOut.ChannelOffset = GetUserSpecifiedChannelOffset();
                this.asioOut.Init(this.reader);
            }
            
            this.reader.Position = 0;
            this.asioOut.Play();
            this.timer1.Enabled = true;
            SetButtonStates();
        }

        private void SetButtonStates()
        {
            buttonPlay.Enabled = asioOut != null && asioOut.PlaybackState != PlaybackState.Playing;
            buttonStop.Enabled = asioOut != null && asioOut.PlaybackState == PlaybackState.Playing;            
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            this.asioOut.Stop();
            this.timer1.Enabled = false;
            SetButtonStates();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (asioOut != null && asioOut.PlaybackState == PlaybackState.Playing && reader.Position >= reader.Length)
            {
                Stop();
            }
        }        
    }

    [Export(typeof(INAudioDemoPlugin))]
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
