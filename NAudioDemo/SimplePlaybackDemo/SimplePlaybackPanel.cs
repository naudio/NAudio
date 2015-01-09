using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAudio.FileFormats.Mp3;
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
            PopulateOutputDriverCombo();
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

        private void PopulateOutputDriverCombo()
        {
            comboBoxOutputDriver.Items.Add("WaveOut Window Callbacks");
            comboBoxOutputDriver.Items.Add("WaveOut Function Callbacks");
            comboBoxOutputDriver.Items.Add("WaveOut Event Callbacks");
            comboBoxOutputDriver.SelectedIndex = 0;
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (fileName == null) fileName = SelectInputFile();
            if (fileName != null)
            {
                BeginPlayback(fileName);
            }
        }

        private static string SelectInputFile()
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Audio Files|*.mp3;*.wav;*.aiff;*.wma";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                return ofd.FileName;
            }

            return null;
        }

        private void BeginPlayback(string filename)
        {
            Debug.Assert(this.wavePlayer == null);
            this.wavePlayer = CreateWavePlayer();
            this.file = new AudioFileReader(filename);
            this.file.Volume = volumeSlider1.Volume;
            this.wavePlayer.Init(file);
            this.wavePlayer.PlaybackStopped += wavePlayer_PlaybackStopped;
            this.wavePlayer.Play();
            EnableButtons(true);
            timer1.Enabled = true; // timer for updating current time label
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

        void wavePlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            // we want to be always on the GUI thread and be able to change GUI components
            Debug.Assert(!this.InvokeRequired, "PlaybackStopped on wrong thread");
            // we want it to be safe to clean up input stream and playback device in the handler for PlaybackStopped
            CleanUp();
            EnableButtons(false);
            timer1.Enabled = false;
            labelNowTime.Text = "00:00";
            if (e.Exception != null)
            {
                MessageBox.Show(String.Format("Playback Stopped due to an error {0}", e.Exception.Message));
            }
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
            fileName = SelectInputFile();
        }

        private void OnMp3RepositionTestClick(object sender, EventArgs e)
        {
            var filename = SelectInputFile();
            if (filename == null) return;
            var wo = new WaveOut();
            var af = new AudioFileReader(filename);
            wo.Init(af);
            var f = new Form();
            var b = new Button() { Text = "Play" };
            b.Click += (s, a) => wo.Play();
            var b2 = new Button() { Text = "Stop", Left = b.Right };
            b2.Click += (s, a) => wo.Stop();
            var b3 = new Button { Text = "Rewind", Left = b2.Right };
            b3.Click += (s, a) => af.Position = 0;
            f.FormClosed += (s, a) => { wo.Dispose(); af.Dispose(); };
            f.Controls.Add(b);
            f.Controls.Add(b2);
            f.Controls.Add(b3);
            f.ShowDialog(this);
        }
    }
}
