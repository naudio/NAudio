using System;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudioDemo.SimplePlaybackDemo
{
    public partial class SimplePlaybackPanel : UserControl
    {
        private IWavePlayer wavePlayer;
        private AudioFileReader audioFileReader;
        private string fileName;

        public SimplePlaybackPanel()
        {
            InitializeComponent();
            EnableButtons(false);
            PopulateOutputDriverCombo();
            Disposed += SimplePlaybackPanel_Disposed;
            timer1.Interval = 250;
            timer1.Tick += OnTimerTick;
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            return string.Format("{0:D2}:{1:D2}", (int)ts.TotalMinutes, ts.Seconds);
        }

        void OnTimerTick(object sender, EventArgs e)
        {
            if (audioFileReader != null)
            {
                labelNowTime.Text = FormatTimeSpan(audioFileReader.CurrentTime);
                labelTotalTime.Text = FormatTimeSpan(audioFileReader.TotalTime);
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
            Debug.Assert(wavePlayer == null);
            wavePlayer = CreateWavePlayer();
            audioFileReader = new AudioFileReader(filename);
            audioFileReader.Volume = volumeSlider1.Volume;
            wavePlayer.Init(audioFileReader);
            wavePlayer.PlaybackStopped += OnPlaybackStopped;
            wavePlayer.Play();
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

        void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            // we want to be always on the GUI thread and be able to change GUI components
            Debug.Assert(!InvokeRequired, "PlaybackStopped on wrong thread");
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
            if (audioFileReader != null)
            {
                audioFileReader.Dispose();
                audioFileReader = null;
            }
            if (wavePlayer != null)
            {
                wavePlayer.Dispose();
                wavePlayer = null;
            }
        }

        private void OnButtonStopClick(object sender, EventArgs e)
        {
            wavePlayer.Stop();
            // don't set button states now, we'll wait for our PlaybackStopped to come
        }

        private void OnVolumeSliderChanged(object sender, EventArgs e)
        {
            if (audioFileReader != null)
            {
                audioFileReader.Volume = volumeSlider1.Volume;
            }
        }

        private void OnButtonOpenClick(object sender, EventArgs e)
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
