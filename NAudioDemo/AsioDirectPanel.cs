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
        private WaveFileReader reader;
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
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "WAV files|*.wav";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.reader = new WaveFileReader(ofd.FileName);
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

        private void Play()
        {
            // allow change device
            if (this.asioOut != null && this.asioOut.DriverName != comboBoxAsioDevice.Text)
            {
                this.asioOut.Dispose();
                this.asioOut = null;
            }
            
            // create device if necessary
            if (this.asioOut == null)
            {
                this.asioOut = new AsioOut(comboBoxAsioDevice.Text);
                this.asioOut.PlaybackStopped += new EventHandler(asioOut_PlaybackStopped);
                this.asioOut.Init(this.reader);
            }
            
            this.reader.Position = 0;
            this.asioOut.Play();
            SetButtonStates();
        }

        void asioOut_PlaybackStopped(object sender, EventArgs e)
        {
            SetButtonStates();
        }

        private void SetButtonStates()
        {
            buttonPlay.Enabled = asioOut != null && asioOut.PlaybackState != PlaybackState.Playing;
            buttonStop.Enabled = asioOut != null && asioOut.PlaybackState == PlaybackState.Playing;            
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            this.asioOut.Stop();
            SetButtonStates();
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
