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
        private WaveStream file;
        private string fileName;

        public SimplePlaybackPanel()
        {
            InitializeComponent();
            EnableButtons(false);
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
            this.wavePlayer = new WaveOut();
            this.file = new AudioFileReader(filename);
            this.wavePlayer.Init(file);
            this.wavePlayer.PlaybackStopped += new EventHandler(wavePlayer_PlaybackStopped);
            this.wavePlayer.Play();
            EnableButtons(true);
        }

        private void EnableButtons(bool playing)
        {
            buttonPlay.Enabled = !playing;
            buttonStop.Enabled = playing;
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
    }
}
