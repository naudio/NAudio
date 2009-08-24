using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using NAudio.Wave;
using MarkHeath.AudioUtils.Properties;

namespace MarkHeath.AudioUtils
{
    public partial class MixDiffForm : Form
    {        
        private PlaybackStatus playbackStatus;
        private IWavePlayer wavePlayer;
        readonly Font BigFont = new Font("Verdana", 36, FontStyle.Bold);
        readonly Font EmptyFont = new Font("Verdana", 16, FontStyle.Bold);
        private WaveMixerStream32 mixer;
        private int skipSeconds;
        private Button selectedButton;
        private CompareMode compareMode;
        private List<Button> fileButtons;
        private bool shuffled;
        
        public MixDiffForm()
        {
            InitializeComponent();
            mixer = new WaveMixerStream32();
            mixer.AutoStop = false;
            skipSeconds = 3;
            fileButtons = new List<Button>();
            fileButtons.Add(buttonA);
            fileButtons.Add(buttonB);
            fileButtons.Add(buttonC);
            fileButtons.Add(buttonD);

        }


        private bool LoadFile(Button button)
        {
            // prompt for file load
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WAV Files (*.wav)|*.wav";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                MixdownInfo info = new MixdownInfo(openFileDialog.FileName);
                // TODO: sort this out - post shuffle
                info.Letter = button.Name.Substring(button.Name.Length - 1);
                SetButtonInfo(button, info);
                return true;
            }
            return false;
        }

        private void SetButtonInfo(Button button, MixdownInfo info)
        {
            if (button.Tag != null)
            {
                ClearFile(button);
            }
            button.Tag = info;
            SetButtonAppearance(button);
            mixer.AddInputStream(info.Stream);
            SetLengthLabel();
        }

        private void ClearFile(Button button)
        {
            if (button.Tag != null)
            {
                MixdownInfo buttonInfo = button.Tag as MixdownInfo;
                mixer.RemoveInputStream(buttonInfo.Stream);
                buttonInfo.Stream.Close();
                button.Tag = null;
                SetButtonAppearance(button);
            }
        }

        private void SetButtonAppearance(Button button)
        {
            MixdownInfo info = button.Tag as MixdownInfo;
            if (info == null)
            {
                button.Text = "<Empty>";
                button.Font = EmptyFont;
                toolTip1.SetToolTip(button, null);
            }
            else
            {
                if (shuffled)
                {
                    button.Text = "?";
                    toolTip1.SetToolTip(button, null);
                    button.Font = BigFont;
                }
                else
                {
                    button.Text = info.Letter;
                    toolTip1.SetToolTip(button, info.FileName);
                    button.Font = BigFont;            
                }
            }
        }

        private void OnMixButtonClick(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button.Tag == null)
            {
                if (LoadFile(button))
                {
                    SelectButton(button);
                }
            }
            else
            {
                SelectButton(button);
            }
        }

        private void SelectButton(Button button)
        {
            MixdownInfo info;
            if (selectedButton != null)
            {
                selectedButton.BackColor = SystemColors.Control;
                selectedButton.ForeColor = SystemColors.ControlText;
                info = selectedButton.Tag as MixdownInfo;
                if (info != null)
                {
                    // can be null if active button is cleared
                    info.Stream.Mute = true;
                }
            }
            info = button.Tag as MixdownInfo;
            button.ForeColor = Color.DarkGreen;
            button.BackColor = Color.LightGoldenrodYellow;
            info.Stream.Mute = false;
            selectedButton = button;
            if (playbackStatus == PlaybackStatus.Playing)
            {
                if (compareMode == CompareMode.SkipBack)
                {
                    SkipBack();
                }
                else if (compareMode == CompareMode.Restart)
                {
                    Rewind();
                }
            }
        }

        private void Play()
        {
            if (playbackStatus != PlaybackStatus.Playing)
            {
                if (playbackStatus != PlaybackStatus.Paused)
                {
                    Rewind();
                }
                if (wavePlayer == null)
                {
                    wavePlayer = new WaveOut();
                    wavePlayer.Init(mixer);                
                }
                wavePlayer.Play();
                playbackStatus = PlaybackStatus.Playing;
                timer1.Start();
            }
        }

        private void Stop()
        {
            if (playbackStatus != PlaybackStatus.Stopped)
            {
                if (wavePlayer != null)
                {
                    wavePlayer.Stop();
                    playbackStatus = PlaybackStatus.Stopped;
                    timer1.Stop();
                }
                Rewind();
            }
        }

        private void Pause()
        {
            if (playbackStatus == PlaybackStatus.Playing)
            {
                wavePlayer.Pause();
                playbackStatus = PlaybackStatus.Paused;
            }
        }

        private void toolStripButtonPlay_Click(object sender, EventArgs e)
        {
            Play();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (playbackStatus == PlaybackStatus.Playing)
            {
                if (mixer.Position > mixer.Length)
                {
                    if (toolStripButtonLoop.Checked)
                    {
                        Rewind();
                    }
                    else
                    {
                        Stop();
                    }
                }

                SetPositionLabel();
            }
        }

        private void SetPositionLabel()
        {
            TimeSpan currentTime = mixer.CurrentTime;
            toolStripLabelPosition.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                currentTime.Hours,
                currentTime.Minutes,
                currentTime.Seconds,
                currentTime.Milliseconds);
        }

        private void SetLengthLabel()
        {
            TimeSpan length = mixer.TotalTime;
            toolStripLabelLength.Text = String.Format("{0:00}:{1:00}:{2:00}",
                length.Hours,
                length.Minutes,
                length.Seconds);

        }

        private void MixDiffForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (playbackStatus != PlaybackStatus.Stopped)
            {
                Stop();
            }
            if (wavePlayer != null)
            {
                wavePlayer.Dispose();
                wavePlayer = null;
            }
            if (mixer != null)
            {
                mixer.Dispose();
                mixer = null;
            }
        }

        private void toolStripButtonStop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void toolStripButtonPause_Click(object sender, EventArgs e)
        {
            Pause();
        }

        private void toolStripButtonBack_Click(object sender, EventArgs e)
        {
            SkipBack();
        }

        private void toolStripButtonForward_Click(object sender, EventArgs e)
        {
            if (mixer != null)
            {
                mixer.CurrentTime += TimeSpan.FromSeconds(skipSeconds);
                SetPositionLabel();

            }
        }

        private void toolStripButtonRewind_Click(object sender, EventArgs e)
        {
            Rewind();
        }

        private void SkipBack()
        {
            if (mixer != null)
            {
                mixer.CurrentTime += TimeSpan.FromSeconds(0 - skipSeconds);
                SetPositionLabel();
            }
        }

        private void Rewind()
        {
            if (mixer != null)
            {
                mixer.Position = 0;
                SetPositionLabel();
            }
        }

        public CompareMode CompareMode
        {
            get { return compareMode; }
            set 
            { 
                compareMode = value;
                currentPositionToolStripMenuItem1.Checked = (compareMode == CompareMode.CurrentPosition);
                skipBackToolStripMenuItem1.Checked = (compareMode == CompareMode.SkipBack);
                restartToolStripMenuItem1.Checked = (compareMode == CompareMode.Restart);
            }       

        }

        private void currentPositionToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CompareMode = CompareMode.CurrentPosition;
        }

        private void skipBackToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CompareMode = CompareMode.SkipBack;
        }

        private void restartToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CompareMode = CompareMode.Restart;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            Button button = (Button)contextMenuStrip1.SourceControl;
            ClearFile(button);
        }

        private void selectFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Button button = (Button)contextMenuStrip1.SourceControl;
            LoadFile(button);
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Button button = (Button)contextMenuStrip1.SourceControl;
            if (button.Tag != null)
            {
                MixdownInfo mixdownInfo = button.Tag as MixdownInfo;
                PropertiesForm propertiesForm = new PropertiesForm(mixdownInfo);
                if (propertiesForm.ShowDialog() == DialogResult.OK)
                {
                    
                }
            }
        }

        private void saveComparisonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = ".MixDiff";
            saveFileDialog.Filter = "*.MixDiff (MixDiff Comparison Files)|*.MixDiff|*.* (All Files)|*.*";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveComparison(saveFileDialog.FileName);
            }
        }

        private void SaveComparison(string fileName)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
                
            using (XmlWriter writer = XmlWriter.Create(fileName,settings))
            {
                writer.WriteStartElement("MixDiff");
                writer.WriteStartElement("Settings");
                writer.WriteAttributeString("CompareMode", compareMode.ToString());
                writer.WriteEndElement();
                foreach (Button button in fileButtons)
                {
                    WriteMixdownInfo(writer, button.Tag as MixdownInfo);
                }
                writer.WriteEndElement();
            }
        }

        private void LoadComparison(string fileName)
        {
            XmlDocument document = new XmlDocument();
            document.Load(fileName);
            XmlNode compareModeNode = document.SelectSingleNode("MixDiff/Settings/@CompareMode");
            CompareMode = (CompareMode)Enum.Parse(typeof(CompareMode), compareModeNode.Value);
            XmlNodeList mixes = document.SelectNodes("MixDiff/Mix");
            int buttonIndex = 0;
            foreach(XmlNode mixNode in mixes)
            {
                Button button = fileButtons[buttonIndex++];
                MixdownInfo info = new MixdownInfo(mixNode.SelectSingleNode("@FileName").Value);
                info.DelayMilliseconds = Int32.Parse(mixNode.SelectSingleNode("@DelayMilliseconds").Value);
                info.OffsetMilliseconds = Int32.Parse(mixNode.SelectSingleNode("@OffsetMilliseconds").Value);
                info.VolumeDecibels = Int32.Parse(mixNode.SelectSingleNode("@VolumeDecibels").Value);
                info.Letter = button.Name.Substring(button.Name.Length - 1);
                info.Stream.Mute = true;
                SetButtonInfo(button,info);                
            }
            SelectButton(fileButtons[0]);
        }

        private void WriteMixdownInfo(XmlWriter writer, MixdownInfo mixdownInfo)
        {
            if (mixdownInfo != null)
            {
                writer.WriteStartElement("Mix");
                writer.WriteAttributeString("FileName", mixdownInfo.FileName);
                writer.WriteAttributeString("DelayMilliseconds", mixdownInfo.DelayMilliseconds.ToString());
                writer.WriteAttributeString("OffsetMilliseconds", mixdownInfo.OffsetMilliseconds.ToString());
                writer.WriteAttributeString("VolumeDecibels", mixdownInfo.VolumeDecibels.ToString());
                writer.WriteEndElement();
            }
        }

        private void openSavedComparisonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".MixDiff";
            openFileDialog.Filter = "*.MixDiff (MixDiff Comparison Files)|*.MixDiff|*.* (All Files)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                Stop();
                foreach(Button button in fileButtons)
                {
                    ClearFile(button);
                }
                LoadComparison(openFileDialog.FileName);
            }
        }

        private void contentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://www.codeplex.com/naudio/Wiki/View.aspx?title=MixDiff");
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to launch browser to show help file");
            }
            
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NAudio.Utils.AboutForm aboutForm = new NAudio.Utils.AboutForm();
            aboutForm.ShowDialog();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                // TODO: reopen WaveOut device
            }

        }

        private void toolStripButtonShuffle_Click(object sender, EventArgs e)
        {
            if (!shuffled)
            {
                Shuffle();
            }
            else
            {
                Reveal();
            }

            toolStripButtonShuffle.Checked = shuffled;
        }

        private void Shuffle()
        {
            List<MixdownInfo> mixdowns = new List<MixdownInfo>();
            foreach (Button button in fileButtons)
            {
                if (button.Tag != null)
                {
                    mixdowns.Add(button.Tag as MixdownInfo);
                }
            }


            Random rand = new Random();
            if (mixdowns.Count < 2)
            {
                MessageBox.Show("You need to have at least two files to compare to use the shuffle feature",
                    Application.ProductName);
                return;
            }

            shuffled = true;

            if (Settings.Default.UseAllSlots)
            {
                foreach (Button button in fileButtons)
                {
                    if (button.Tag == null)
                    {
                        button.Tag = mixdowns[rand.Next() % mixdowns.Count];
                    }
                }
            }
            
            for (int n = 0; n < 12; n++)
            {
                int swap1 = rand.Next() % fileButtons.Count;
                int swap2 = rand.Next() % fileButtons.Count;
                if (swap1 != swap2)
                {
                    object tag1 = fileButtons[swap1].Tag;
                    fileButtons[swap1].Tag = fileButtons[swap2].Tag;
                    fileButtons[swap2].Tag = tag1;
                }
            }

            Button firstMix = null;
            foreach (Button button in fileButtons)
            {
                SetButtonAppearance(button);
                if (button.Tag != null && firstMix == null)
                    firstMix = button;
            }
            SelectButton(firstMix);
        }

        private void Reveal()
        {
            shuffled = false;
            foreach (Button button in fileButtons)
            {
                SetButtonAppearance(button);
            }
        }

        private void MixDiffForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.D1:
                    OnMixButtonClick(buttonA, EventArgs.Empty);
                    e.Handled = true;
                    break;
                case Keys.D2:
                    OnMixButtonClick(buttonB, EventArgs.Empty);
                    e.Handled = true;
                    break;
                case Keys.D3:
                    OnMixButtonClick(buttonC, EventArgs.Empty);
                    e.Handled = true;
                    break;
                case Keys.D4:
                    OnMixButtonClick(buttonD, EventArgs.Empty);
                    e.Handled = true;
                    break;
                case Keys.Space:
                    if (playbackStatus != PlaybackStatus.Playing)
                    {
                        Play();
                    }
                    else
                    {
                        Pause();
                    }
                    e.Handled = true;
                    break;
                case Keys.Home:
                    Rewind();
                    e.Handled = true;
                    break;
            }

        }


    }

    public enum CompareMode
    {
        CurrentPosition,
        SkipBack,
        Restart,
    }

    enum PlaybackStatus
    {
        Stopped,
        Playing,
        Paused
    }
}