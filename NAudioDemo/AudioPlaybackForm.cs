using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace NAudioDemo
{
    public partial class AudioPlaybackForm : Form
    {
        IWavePlayer waveOut;
        string fileName = null;
        WaveStream mainOutputStream;
        WaveChannel32 volumeStream;

        public AudioPlaybackForm()
        {            
            InitializeComponent();

            // Disable ASIO if no drivers are available
            if ( ! AsioOut.isSupported() )
            {
                radioButtonAsio.Enabled = false;
                buttonControlPanel.Enabled = false;
                comboBoxAsioDriver.Enabled = false;
            } 
            else
            {
                // Just fill the comboBox AsioDriver with available driver names
                String[] asioDriverNames = AsioOut.GetDriverNames();
                foreach (string driverName in asioDriverNames)
                {
                    comboBoxAsioDriver.Items.Add(driverName);
                }
                comboBoxAsioDriver.SelectedIndex = 0;
            }

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
            }
            if (String.IsNullOrEmpty(fileName))
            {
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

            mainOutputStream = CreateInputStream(fileName);
            trackBarPosition.Maximum = (int)mainOutputStream.TotalTime.TotalSeconds;
            labelTotalTime.Text = String.Format("{0:00}:{1:00}", (int)mainOutputStream.TotalTime.TotalMinutes,
                mainOutputStream.TotalTime.Seconds);
            trackBarPosition.TickFrequency = trackBarPosition.Maximum / 30;

            try
            {
                waveOut.Init(mainOutputStream);
            }
            catch (Exception initException)
            {
                MessageBox.Show(String.Format("{0}", initException.Message), "Error Initializing Output");
                return;
            }

            // not doing Volume on IWavePlayer any more
            volumeStream.Volume = volumeSlider1.Volume; 
            groupBoxDriverModel.Enabled = false;
            waveOut.Play();
        }

        private WaveStream CreateInputStream(string fileName)
        {
            WaveChannel32 inputStream;
            if (fileName.EndsWith(".wav"))
            {
                inputStream = new WaveChannel32(new WaveFileReader(fileName));

            }
            else if (fileName.EndsWith(".mp3"))
            {                
                WaveStream mp3Reader = new Mp3FileReader(fileName);
                WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader);
                WaveStream blockAlignedStream = new BlockAlignReductionStream(pcmStream);
                inputStream = new WaveChannel32(blockAlignedStream);
            }
            else
            {
                throw new InvalidOperationException("Unsupported extension");
            }
            // we are not going into a mixer so we don't need to zero pad
            //((WaveChannel32)inputStream).PadWithZeroes = false;
            volumeStream = inputStream;
            var meteringStream = new MeteringStream(inputStream, inputStream.WaveFormat.SampleRate / 10);
            meteringStream.StreamVolume += new EventHandler<StreamVolumeEventArgs>(meteringStream_StreamVolume);
            
            return meteringStream;
        }

        void meteringStream_StreamVolume(object sender, StreamVolumeEventArgs e)
        {
            volumeMeter1.Amplitude = e.MaxSampleValues[0];
            waveformPainter1.AddMax(e.MaxSampleValues[0]);
            if (e.MaxSampleValues.Length > 1)
            {
                volumeMeter2.Amplitude = e.MaxSampleValues[1];
                waveformPainter2.AddMax(e.MaxSampleValues[1]);
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
                waveOut = new DirectSoundOut(latency);
            }
            else if (radioButtonAsio.Checked)
            {
                waveOut = new AsioOut((String)comboBoxAsioDriver.SelectedItem);
                buttonControlPanel.Enabled = true;
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
            buttonControlPanel.Enabled = false;
            if (waveOut != null)
            {
                waveOut.Stop();
            }
            if (mainOutputStream != null)
            {
                // this one really closes the file and ACM conversion
                volumeStream.Close();
                volumeStream = null;
                // this one does the metering stream
                mainOutputStream.Close();
                mainOutputStream = null;
            }
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
            buttonControlPanel.Enabled = false;
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
            if (mainOutputStream != null)
            {
                volumeStream.Volume = volumeSlider1.Volume;
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
                trackBarPosition.Value = 0;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                if (mainOutputStream.Position >= mainOutputStream.Length)
                {
                    buttonStop_Click(sender, e);
                }
                else
                {
                    TimeSpan currentTime = mainOutputStream.CurrentTime;
                    trackBarPosition.Value = (int)currentTime.TotalSeconds;
                    labelCurrentTime.Text = String.Format("{0:00}:{1:00}", (int)currentTime.TotalMinutes,
                        currentTime.Seconds);
                }
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
            openFileDialog.Filter = "All Supported Files (*.wav, *.mp3)|*.wav;*.mp3|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileName = openFileDialog.FileName;
            }
        }

    }
}

