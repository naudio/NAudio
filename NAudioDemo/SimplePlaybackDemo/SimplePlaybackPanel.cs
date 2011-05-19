using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudioDemo.SimplePlaybackDemo
{
    public partial class SimplePlaybackPanel : UserControl
    {
        private IWavePlayer wavePlayer;
        private AudioFileReader file;
        private string fileName;

        public SimplePlaybackPanel()
        {
            InitializeComponent();
            EnableButtons(false);
            comboBoxOutputDriver.Items.Add("WaveOut");
            comboBoxOutputDriver.Items.Add("WaveOut Function Callbacks");
            comboBoxOutputDriver.Items.Add("WaveOut Event Callbacks");
            comboBoxOutputDriver.SelectedIndex = 0;
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (SelectInputFile())
            {
                BeginPlayback(fileName);
            }
        }

        private bool SelectInputFile()
        {
            if (fileName == null)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Audio Files|*.mp3;*.wav;*.aiff";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    this.fileName = ofd.FileName;
                }
            }
            return fileName != null;
        }

        private void BeginPlayback(string filename)
        {
            Debug.Assert(this.wavePlayer == null);
            this.wavePlayer = CreateWavePlayer();
            this.file = new AudioFileReader(filename);
            this.file.Volume = volumeSlider1.Volume;
            this.wavePlayer.Init(file);
            this.wavePlayer.PlaybackStopped += new EventHandler(wavePlayer_PlaybackStopped);
            this.wavePlayer.Play();
            EnableButtons(true);
        }

        private IWavePlayer CreateWavePlayer()
        {
            switch (comboBoxOutputDriver.SelectedIndex)
            {
                case 2:
                    return new WaveOutEvent();
                case 1:
                    return new WaveOut(WaveCallbackInfo.FunctionCallback());
                case 0:
                default:
                    return new WaveOut();
            }
        }

        private void EnableButtons(bool playing)
        {
            buttonPlay.Enabled = !playing;
            buttonStop.Enabled = playing;
            buttonOpen.Enabled = !playing;
        }

        void wavePlayer_PlaybackStopped(object sender, EventArgs e)
        {
            // we want it to be safe to clean up input stream and playback device in the handler for PlaybackStopped
            CleanUp();
            // we want to be always on the GUI thread and be able to change GUI components
            Debug.Assert(!this.InvokeRequired, "PlaybackStopped on wrong thread");
            EnableButtons(false);
        }

        private void CleanUp()
        {
            this.file.Dispose();
            this.file = null;
            this.wavePlayer.Dispose();
            this.wavePlayer = null;
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            this.wavePlayer.Stop();
            // don't set button states now, we'll wait for our PlaybackStopped to come
        }

        private void volumeSlider1_VolumeChanged(object sender, EventArgs e)
        {
            if (this.file != null)
            {
                this.file.Volume = volumeSlider1.Volume;
            }
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            SelectInputFile();
        }
    }
}
