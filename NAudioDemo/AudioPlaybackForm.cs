using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using System.IO;
using NAudioDemo.Properties;
using NAudio.CoreAudioApi;

namespace NAudioDemo
{
    public partial class AudioPlaybackForm : Form
    {
        IWavePlayer waveOut;
        List<WaveStream> inputs = new List<WaveStream>();
        string fileName = null;
        WaveStream mainOutputStream;

        public AudioPlaybackForm()
        {            
            InitializeComponent();
        }
            
        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    return;
                }
                else if (waveOut.PlaybackState == PlaybackState.Paused)
                {
                    waveOut.Play();
                    groupBoxDriverModel.Enabled = false;
                    return;
                }
            }
            
            // we are in a stopped state
            // TODO: only re-initialise if necessary

            if (String.IsNullOrEmpty(fileName))
            {
                toolStripButtonOpenFile_Click(sender, e);
                return;
            }

            try
            {
                CreateWaveOut();
            }
            catch (Exception driverCreateException)
            {
                MessageBox.Show(String.Format("{0}", driverCreateException.Message));
                return;
            }

            WaveStream reader = CreateInputStream(fileName);
            trackBarPosition.Maximum = (int) reader.TotalTime.TotalSeconds;
            trackBarPosition.TickFrequency = trackBarPosition.Maximum / 30;
            inputs.Add(reader);
            
            if (inputs.Count == 0)
            {
                MessageBox.Show("No WAV files found to play in the input folder");
                return;
            }

            //WaveMixerStream32 mixer = new WaveMixerStream32(inputs, false);
            //Wave32To16Stream mixdown = new Wave32To16Stream(mixer);
            mainOutputStream = inputs[0];
            waveOut.Init(mainOutputStream);
            waveOut.Volume = volumeSlider1.Volume;
            groupBoxDriverModel.Enabled = false;
            waveOut.Play();
        }

        private WaveStream CreateInputStream(string fileName)
        {
            if (fileName.EndsWith(".wav"))
            {
                return new WaveChannel32(new WaveFileReader(fileName));
            }
            else if (fileName.EndsWith(".mp3"))
            {                
                return new WaveChannel32(WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(fileName)));
            }
            else
            {
                throw new InvalidOperationException("Unsupported extension");
            }
        }

        private void CreateWaveOut()
        {
            CloseWaveOut();
            int latency = (int)comboBoxLatency.SelectedItem;
            if (radioButtonWaveOut.Checked)
            {
                waveOut = new WaveOut(0, latency, checkBoxWaveOutWindow.Checked ? this : null);
            }
            else if (radioButtonDirectSound.Checked)
            {
                if (checkBoxDirectSoundNative.Checked)
                {
                    waveOut = new NativeDirectSoundOut(latency);
                }
                else
                {
                    waveOut = new DirectSoundOut(this, latency);
                }
            }
            else if (radioButtonAsio.Checked)
            {
                waveOut = new AsioOut(0);
            }
            else
            {
                waveOut = new WasapiOut(
                    checkBoxWasapiExclusiveMode.Checked ?
                        AudioClientShareMode.Exclusive :
                        AudioClientShareMode.Shared,
                    checkBoxWasapiEventCallback.Checked,
                    latency);
            }
        }

        private void CloseWaveOut()
        {
            if (waveOut != null)
            {
                waveOut.Stop();
            }
            foreach (WaveStream input in inputs)
            {
                input.Dispose();
            }
            inputs.Clear();
            if (waveOut != null)
            {
                waveOut.Dispose();
                waveOut = null;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseWaveOut();            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBoxLatency.Items.Add(25);
            comboBoxLatency.Items.Add(50);
            comboBoxLatency.Items.Add(100);
            comboBoxLatency.Items.Add(150);
            comboBoxLatency.Items.Add(200);
            comboBoxLatency.Items.Add(300);
            comboBoxLatency.Items.Add(400);
            comboBoxLatency.Items.Add(500);
            comboBoxLatency.SelectedIndex = 5;
        }

        private void buttonPause_Click(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    waveOut.Pause();
                }
            }
        }

        private void volumeSlider1_VolumeChanged(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                waveOut.Volume = volumeSlider1.Volume;
            }
        }

        private void buttonControlPanel_Click(object sender, EventArgs e)
        {
            AsioOut asio = waveOut as AsioOut;
            if (asio != null)
            {
                asio.ShowControlPanel();
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                waveOut.Stop();
                groupBoxDriverModel.Enabled = true;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                trackBarPosition.Value = (int)mainOutputStream.CurrentTime.TotalSeconds;
            }
        }

        private void trackBarPosition_Scroll(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                mainOutputStream.CurrentTime = TimeSpan.FromSeconds(trackBarPosition.Value);
            }
        }

        private void toolStripButtonOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WAV files (*.wav)|*.wav|MP3 Files (*.mp3)|*.mp3|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileName = openFileDialog.FileName;
            }
        }

    }
}
