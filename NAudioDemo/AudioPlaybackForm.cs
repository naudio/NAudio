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
        string fileName = "C:\\Users\\Mark\\Recording\\REAPER\\ideas-converted.wav";

        public AudioPlaybackForm()
        {            
            //folder = Settings.Default.AudioFolder;
            InitializeComponent();
        }

            
        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                buttonLoad_Click(sender, e);
                return;
            }

            CloseWaveOut();
            int latency = (int)comboBoxLatency.SelectedItem;
            if (radioButtonWaveOut.Checked)
            {
                waveOut = new WaveOut(0, latency, null);
            }
            else if (radioButtonWaveOutWindow.Checked)
            {
                waveOut = new WaveOut(0, latency, this);
            }
            else if (radioButtonDirectSound.Checked)
            {
                waveOut = new DirectSoundOut(this, 300);
            }
            else if (radioButtonAsio.Checked)
            {
                waveOut = new AsioOut(0);
            }
            else
            {
                waveOut = new WasapiOut(AudioClientShareMode.Shared, latency);
            }

            /*
            foreach (string wavFile in Directory.GetFiles(folder, "*.wav"))
            {
                WaveStream reader = new WaveChannel32(new LoopStream(new WaveFileReader(wavFile)));
                inputs.Add(reader);
            }*/

            WaveStream reader = new WaveChannel32(new WaveFileReader(fileName));
            inputs.Add(reader);
            
            if (inputs.Count == 0)
            {
                MessageBox.Show("No WAV files found to play in the input folder");
                return;
            }

            WaveMixerStream32 mixer = new WaveMixerStream32(inputs, false);
            //Wave32To16Stream mixdown = new Wave32To16Stream(mixer);
            waveOut.Init(mixer);
            waveOut.Volume = volumeSlider1.Volume;
            waveOut.Play();
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
                    waveOut.Stop();
                    buttonPause.Text = "Resume";
                }
                else
                {
                    waveOut.Play();
                    buttonPause.Text = "Pause";
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

        private void volumeSlider1_Load(object sender, EventArgs e)
        {

        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            /*FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Select WAV File Folder";
            folderDialog.SelectedPath = folder;
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                folder = folderDialog.SelectedPath;
                Settings.Default.AudioFolder = folder;
                Settings.Default.Save();
            }*/
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WAV files (*.wav)|*.wav|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileName = openFileDialog.FileName;
            }
        }
    }
}
