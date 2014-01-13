using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;

namespace NAudioDemo.MediaFoundationDemo
{
    public partial class MediaFoundationDemoPanel : UserControl
    {
        public MediaFoundationDemoPanel()
        {
            InitializeComponent();
            Disposed += OnDisposed;
            timer1.Interval = 500;
        }

        private IWavePlayer wavePlayer;
        private WaveStream reader;

        private void OnDisposed(object sender, EventArgs eventArgs)
        {
            if (wavePlayer != null)
            {
                wavePlayer.Dispose();
                wavePlayer = null;
            }
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
        }

        private void OnTrackBarScroll(object sender, EventArgs e)
        {
            if (reader != null)
            {
                reader.Position = (trackBar1.Value*reader.Length)/trackBar1.Maximum;
            }
        }

        private void OnButtonLoadFileClick(object sender, EventArgs e)
        {

            if (wavePlayer != null)
            {
                wavePlayer.Dispose();
                wavePlayer = null;
            }

            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;

            if (reader != null)
            {
                reader.Dispose();
            }
            reader = new MediaFoundationReader(ofd.FileName, new MediaFoundationReader.MediaFoundationReaderSettings() { SingleReaderObject = true});
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (reader != null)
            {
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            labelPosition.Text = string.Format("{0}/{1}", reader.CurrentTime, reader.TotalTime);
            trackBar1.Value = Math.Min((int) ((trackBar1.Maximum*reader.Position)/reader.Length), trackBar1.Maximum);
        }

        private void OnButtonPlayClick(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            if (reader == null)
            {
                OnButtonLoadFileClick(sender,e);
                if (reader == null) return;
            }
            if (wavePlayer == null)
            {
                if (radioButtonWasapi.Checked)
                {
                    wavePlayer = new WasapiOutGuiThread();
                }
                else
                {
                    wavePlayer = new WaveOut();
                }
                wavePlayer.PlaybackStopped += WavePlayerOnPlaybackStopped;
                wavePlayer.Init(reader);
            }
            wavePlayer.Play();
        }

        private void WavePlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            reader.Position = 0;
            timer1.Enabled = false;
            UpdatePosition();
            if (stoppedEventArgs.Exception != null)
            {
                MessageBox.Show(stoppedEventArgs.Exception.Message);
            }
        }

        private void OnButtonStopClick(object sender, EventArgs e)
        {
            if (wavePlayer != null)
            {
                wavePlayer.Stop();
            }
        }

        private void OnRadioButtonWaveOutCheckedChanged(object sender, EventArgs e)
        {
            if (wavePlayer != null)
            {
                wavePlayer.Dispose();
                wavePlayer = null;
            }
        }
    }

    [Export(typeof(INAudioDemoPlugin))]
    class MediaFoundationDemoPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "Media Foundation Demo"; }
        }

        public Control CreatePanel()
        {
            return new MediaFoundationDemoPanel();
        }
    }
}
