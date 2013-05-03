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
using NAudio.Wave.SampleProviders;

namespace NAudioDemo.FadeInOutDemo
{
    public partial class FadeInOutPanel : UserControl
    {
        private IWavePlayer wavePlayer;
        private AudioFileReader file;
        private FadeInOutSampleProvider fadeInOut;
        private string fileName;

        public FadeInOutPanel()
        {
            InitializeComponent();
            EnableButtons(false);
            this.Disposed += new EventHandler(SimplePlaybackPanel_Disposed);
            this.timer1.Interval = 250;
            this.timer1.Tick += new EventHandler(timer1_Tick);
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            return string.Format("{0:D2}:{1:D2}", (int)ts.TotalMinutes, ts.Seconds);
        }

        void timer1_Tick(object sender, EventArgs e)
        {
            if (file != null)
            {
                labelNowTime.Text = FormatTimeSpan(file.CurrentTime);
                labelTotalTime.Text = FormatTimeSpan(file.TotalTime);
            }
        }

        void SimplePlaybackPanel_Disposed(object sender, EventArgs e)
        {
            CleanUp();
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
            this.wavePlayer = new WaveOutEvent();
            this.file = new AudioFileReader(filename);
            this.fadeInOut = new FadeInOutSampleProvider(file);
            this.file.Volume = volumeSlider1.Volume;
            this.wavePlayer.Init(fadeInOut);
            this.wavePlayer.PlaybackStopped += wavePlayer_PlaybackStopped;
            this.wavePlayer.Play();
            EnableButtons(true);
            timer1.Enabled = true; // timer for updating current time label
        }

        private void EnableButtons(bool playing)
        {
            buttonPlay.Enabled = !playing;
            buttonStop.Enabled = playing;
            buttonOpen.Enabled = !playing;
            buttonBeginFadeIn.Enabled = playing;
            buttonBeginFadeOut.Enabled = playing;
        }

        void wavePlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            // we want to be always on the GUI thread and be able to change GUI components
            Debug.Assert(!this.InvokeRequired, "PlaybackStopped on wrong thread");
            // we want it to be safe to clean up input stream and playback device in the handler for PlaybackStopped
            CleanUp();
            EnableButtons(false);
            timer1.Enabled = false;
            labelNowTime.Text = "00:00";
        }

        private void CleanUp()
        {
            if (this.file != null)
            {
                this.file.Dispose();
                this.file = null;
            }
            if (this.wavePlayer != null)
            {
                this.wavePlayer.Dispose();
                this.wavePlayer = null;
            }
            this.fadeInOut = null;
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

        private int GetFadeDuration()
        {
            int fadeDuration = 5000;
            int.TryParse(textBoxFadeDuration.Text, out fadeDuration);
            return fadeDuration;
        }

        private void buttonBeginFadeIn_Click(object sender, EventArgs e)
        {
            if (this.fadeInOut != null)
            {
                this.fadeInOut.BeginFadeIn(GetFadeDuration());
            }
        }

        private void buttonBeginFadeOut_Click(object sender, EventArgs e)
        {
            if (this.fadeInOut != null)
            {
                this.fadeInOut.BeginFadeOut(GetFadeDuration());
            }
        }
    }
}
