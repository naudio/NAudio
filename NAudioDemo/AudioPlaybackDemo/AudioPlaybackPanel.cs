using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudioDemo.AudioPlaybackDemo
{
    [Export]
    public partial class AudioPlaybackPanel : UserControl
    {
        private IWavePlayer waveOut;
        private string fileName = null;
        private WaveStream fileWaveStream;
        private Action<float> setVolumeDelegate;

        [ImportingConstructor]
        public AudioPlaybackPanel([ImportMany]IEnumerable<IOutputDevicePlugin> outputDevicePlugins)
        {
            InitializeComponent();
            LoadOutputDevicePlugins(outputDevicePlugins);
        }

        [ImportMany(typeof(IInputFileFormatPlugin))]
        public IEnumerable<IInputFileFormatPlugin> InputFileFormats { get; set; }

        private void LoadOutputDevicePlugins(IEnumerable<IOutputDevicePlugin> outputDevicePlugins)
        {
            comboBoxOutputDevice.DisplayMember = "Name";
            comboBoxOutputDevice.SelectedIndexChanged += new EventHandler(comboBoxOutputDevice_SelectedIndexChanged);
            foreach (var outputDevicePlugin in outputDevicePlugins.OrderBy(p => p.Priority))
            {
                comboBoxOutputDevice.Items.Add(outputDevicePlugin);
            }
            comboBoxOutputDevice.SelectedIndex = 0;
        }

        void comboBoxOutputDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            panelOutputDeviceSettings.Controls.Clear();
            Control settingsPanel;
            if (SelectedOutputDevicePlugin.IsAvailable)
            {
                settingsPanel = SelectedOutputDevicePlugin.CreateSettingsPanel();
            }
            else
            {
                settingsPanel = new Label() { Text = "This output device is unavailable on your system", Dock=DockStyle.Fill };
            }
            panelOutputDeviceSettings.Controls.Add(settingsPanel);
        }

        private IOutputDevicePlugin SelectedOutputDevicePlugin
        {
            get { return (IOutputDevicePlugin)comboBoxOutputDevice.SelectedItem; }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (!SelectedOutputDevicePlugin.IsAvailable)
            {
                MessageBox.Show("The selected output driver is not available on this system");
                return;
            }

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

            ISampleProvider sampleProvider = null;
            try
            {
                sampleProvider = CreateInputStream(fileName);
            }
            catch (Exception createException)
            {
                MessageBox.Show(String.Format("{0}", createException.Message), "Error Loading File");
                return;
            }


            trackBarPosition.Maximum = (int)fileWaveStream.TotalTime.TotalSeconds;
            labelTotalTime.Text = String.Format("{0:00}:{1:00}", (int)fileWaveStream.TotalTime.TotalMinutes,
                fileWaveStream.TotalTime.Seconds);
            trackBarPosition.TickFrequency = trackBarPosition.Maximum / 30;

            try
            {
                waveOut.Init(new SampleToWaveProvider(sampleProvider));
            }
            catch (Exception initException)
            {
                MessageBox.Show(String.Format("{0}", initException.Message), "Error Initializing Output");
                return;
            }

            setVolumeDelegate(volumeSlider1.Volume); 
            groupBoxDriverModel.Enabled = false;
            waveOut.Play();
        }

        private IInputFileFormatPlugin GetPluginForFile(string fileName)
        {
            return (from f in this.InputFileFormats where fileName.EndsWith(f.Extension, StringComparison.OrdinalIgnoreCase) select f).FirstOrDefault();
        }

        private ISampleProvider CreateInputStream(string fileName)
        {
            var plugin = GetPluginForFile(fileName);
            if(plugin == null)
            {
                throw new InvalidOperationException("Unsupported file extension");
            }
            this.fileWaveStream = plugin.CreateWaveStream(fileName);
            var waveChannel =  new SampleChannel(this.fileWaveStream);
            this.setVolumeDelegate = (vol) => waveChannel.Volume = vol;
            waveChannel.PreVolumeMeter += OnPreVolumeMeter;
            
            var postVolumeMeter = new MeteringSampleProvider(waveChannel);
            postVolumeMeter.StreamVolume += OnPostVolumeMeter;

            return postVolumeMeter;
        }

        void OnPreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            // we know it is stereo
            waveformPainter1.AddMax(e.MaxSampleValues[0]);
            waveformPainter2.AddMax(e.MaxSampleValues[1]);
        }

        void OnPostVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            // we know it is stereo
            volumeMeter1.Amplitude = e.MaxSampleValues[0];
            volumeMeter2.Amplitude = e.MaxSampleValues[1];
        }

        private void CreateWaveOut()
        {
            CloseWaveOut();
            int latency = (int)comboBoxLatency.SelectedItem;
            this.waveOut = SelectedOutputDevicePlugin.CreateDevice(latency);
        }

        private void CloseWaveOut()
        {
            if (waveOut != null)
            {
                waveOut.Stop();
            }
            if (fileWaveStream != null)
            {
                // this one really closes the file and ACM conversion
                fileWaveStream.Dispose();
                this.setVolumeDelegate = null;
            }
            if (waveOut != null)
            {
                waveOut.Dispose();
                waveOut = null;
            }
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
            if (setVolumeDelegate != null)
            {
                setVolumeDelegate(volumeSlider1.Volume);
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
            if (waveOut != null && fileWaveStream != null)
            {
                TimeSpan currentTime = (waveOut.PlaybackState == PlaybackState.Stopped) ? TimeSpan.Zero : fileWaveStream.CurrentTime;
                if (fileWaveStream.Position >= fileWaveStream.Length)
                {
                    buttonStop_Click(sender, e);
                }
                else
                {
                    trackBarPosition.Value = (int)currentTime.TotalSeconds;
                    labelCurrentTime.Text = String.Format("{0:00}:{1:00}", (int)currentTime.TotalMinutes,
                        currentTime.Seconds);
                }
            }
            else
            {
                trackBarPosition.Value = 0;
            }
        }

        private void trackBarPosition_Scroll(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                fileWaveStream.CurrentTime = TimeSpan.FromSeconds(trackBarPosition.Value);
            }
        }

        private void toolStripButtonOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string allExtensions = string.Join(";", (from f in InputFileFormats select "*" + f.Extension).ToArray());
            openFileDialog.Filter = String.Format("All Supported Files|{0}|All Files (*.*)|*.*", allExtensions);
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileName = openFileDialog.FileName;
            }
        }
    }
}

