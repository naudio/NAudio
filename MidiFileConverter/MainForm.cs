using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Xml;
using System.Configuration;
using MarkHeath.MidiUtils.Properties;
using NAudio.Utils;

namespace MarkHeath.MidiUtils
{
    public partial class MainForm : Form
    {
        bool workQueued;
        NamingRules namingRules;
        MidiConverter midiConverter;

        Properties.Settings settings;

        public MainForm()
        {
            InitializeComponent();
            
            settings = Properties.Settings.Default;
            
            if (Settings.Default.FirstTime)
            {
                UpgradeSettings();
            }

            // could look in HKLM \ Software \ Toontrack \ Superior \ EZDrummer \ HomePath
            LoadSettings();       
        }

        private void UpgradeSettings()
        {
            string productVersion = (string)settings.GetPreviousVersion("ProductVersion");
            if ((productVersion != null) && (productVersion.Length > 0))
            {
                settings.InputFolder = (string)settings.GetPreviousVersion("InputFolder");
                settings.OutputFolder = (string)settings.GetPreviousVersion("OutputFolder");
                settings.OutputChannelNumber = (int)settings.GetPreviousVersion("OutputChannelNumber");
                settings.OutputMidiType = (OutputMidiType)settings.GetPreviousVersion("OutputMidiType");
                settings.VerboseOutput = (bool)settings.GetPreviousVersion("VerboseOutput");
                settings.UseFileName = (bool)settings.GetPreviousVersion("UseFileName");
                try
                {
                    settings.AddNameMarker = (bool)settings.GetPreviousVersion("AddNameMarker");
                    settings.TrimTextEvents = (bool)settings.GetPreviousVersion("TrimTextEvents");
                    settings.RemoveEmptyTracks = (bool)settings.GetPreviousVersion("RemoveEmptyTracks");
                    settings.RemoveSequencerSpecific = (bool)settings.GetPreviousVersion("RemoveSequencerSpecific");
                    settings.RecreateEndTrackMarkers = (bool)settings.GetPreviousVersion("RecreateEndTrackMarkers");
                    settings.RemoveExtraTempoEvents = (bool)settings.GetPreviousVersion("RemoveExtraTempoEvents");
                    settings.RemoveExtraMarkers = (bool)settings.GetPreviousVersion("RemoveExtraMarkers");
                    // add new settings at the bottom
                }
                catch (SettingsPropertyNotFoundException)
                {
                }
            }
        }


        private void LoadSettings()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            
            if (settings.InputFolder.Length == 0)
                textBoxInputFolder.Text = Path.Combine(programFiles, "Toontrack\\EZDrummer\\Midi");
            else
                textBoxInputFolder.Text = settings.InputFolder;

            if(settings.OutputFolder.Length == 0)
                textBoxOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            else
                textBoxOutputFolder.Text = settings.OutputFolder;

            checkBoxApplyNamingRules.Checked = settings.ApplyNamingRules;
            checkBoxUseFilename.Checked = settings.UseFileName;
            checkBoxVerbose.Checked = settings.VerboseOutput;
            if (settings.OutputMidiType == OutputMidiType.Type0)
                radioButtonType0.Checked = true;
            else if (settings.OutputMidiType == OutputMidiType.Type1)
                radioButtonType1.Checked = true;
            else
                radioButtonTypeUnchanged.Checked = true;

            if (settings.OutputChannelNumber == 1)
                radioButtonChannel1.Checked = true;
            else if (settings.OutputChannelNumber == 10)
                radioButtonChannel10.Checked = true;
            else
                radioButtonChannelUnchanged.Checked = true;
        }

        private void UpdateSettings()
        {
            settings.InputFolder = textBoxInputFolder.Text;
            settings.OutputFolder = textBoxOutputFolder.Text;
            settings.ApplyNamingRules = checkBoxApplyNamingRules.Checked;
            settings.VerboseOutput = checkBoxVerbose.Checked;
            settings.UseFileName = checkBoxUseFilename.Checked;
            if(radioButtonType0.Checked)
                settings.OutputMidiType = OutputMidiType.Type0;
            else if(radioButtonType1.Checked)
                settings.OutputMidiType = OutputMidiType.Type1;
            else
                settings.OutputMidiType = OutputMidiType.LeaveUnchanged;

            if (radioButtonChannel1.Checked)
                settings.OutputChannelNumber = 1;
            else if (radioButtonChannel10.Checked)
                settings.OutputChannelNumber = 10;
            else
                settings.OutputChannelNumber = -1;
        }


        private void MainForm_Load(object sender, EventArgs args)
        {
            string executableFolder = Path.GetDirectoryName(Application.ExecutablePath);
            try
            {
                namingRules = NamingRules.LoadRules(Path.Combine(executableFolder, "NamingRules.xml"));
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Error reading NamingRules.xml\r\n{0}", e.ToString()), Application.ProductName);
                Close();
            }
        }
        private void buttonConvert_Click(object sender, EventArgs e)
        {
            if (workQueued)
            {
                MessageBox.Show("Please wait until the current operation has finished", Application.ProductName);
            }
            else
            {
                UpdateSettings();
                if (!CheckInputFolderExists())
                    return;                
                if (!CheckOutputFolderExists())
                    return;
                if (!CheckOutputFolderIsEmpty())
                    return;
                this.Cursor = Cursors.WaitCursor;
                UpdateSettings();
                workQueued = ThreadPool.QueueUserWorkItem(new WaitCallback(ConvertThreadProc));
                if (workQueued)
                {
                    this.Cursor = Cursors.WaitCursor;
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (workQueued)
            {
                MessageBox.Show("Please wait until the current operation has finished", Application.ProductName);
                e.Cancel = true;
            }
            else
            {
                UpdateSettings();

                settings.FirstTime = false;
                settings.ProductVersion = Application.ProductVersion;
                settings.Save();
            }
            base.OnClosing(e);
        }

        private void ConvertThreadProc(object state)
        {
            try
            {
                progressLog1.ClearLog();
                midiConverter = new MidiConverter(namingRules);
                midiConverter.Progress += new EventHandler<ProgressEventArgs>(midiConverter_Progress);
                midiConverter.Start();
            }
            finally
            {
                workQueued = false;
                this.Invoke(new FinishedDelegate(ShowFinishedMessage));
            }
        }

        void midiConverter_Progress(object sender, ProgressEventArgs e)
        {
            var color = Color.Black;
            if (e.MessageType == ProgressMessageType.Warning)
            {
                color = Color.Blue;
            }
            else if (e.MessageType == ProgressMessageType.Error)
            {
                color = Color.Red;
            }
            else if (e.MessageType == ProgressMessageType.Trace)
            {
                color = Color.Purple;
            }

            progressLog1.LogMessage(color, e.Message);
        }

        delegate void FinishedDelegate();
        
        void ShowFinishedMessage()
        {
            this.Cursor = Cursors.Default;
            saveLogToolStripMenuItem.Enabled = true;
            MessageBox.Show(String.Format("Finished:\r\n{0}", midiConverter.Summary), Application.ProductName);
        }

        private bool CheckInputFolderExists()
        {
            if (!Directory.Exists(textBoxInputFolder.Text))
            {
                DialogResult result = MessageBox.Show("Your selected input folder does not exist.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private bool CheckOutputFolderExists()
        {
            if (!Directory.Exists(textBoxOutputFolder.Text))
            {
                DialogResult result = MessageBox.Show("Your selected output folder does not exist.\r\nWould you like to create it now?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Directory.CreateDirectory(textBoxOutputFolder.Text);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckOutputFolderIsEmpty()
        {
            if ((Directory.GetFiles(textBoxOutputFolder.Text).Length > 0) ||
                (Directory.GetDirectories(textBoxOutputFolder.Text).Length > 0))
            {
                MessageBox.Show("Your output folder is not empty.\r\n" +
                    "You must select an empty folder to store the converted MIDI files.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void buttonBrowseEZDrummer_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            folderBrowser.Description = "Select Input Folder";
            folderBrowser.SelectedPath = textBoxInputFolder.Text;
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                textBoxInputFolder.Text = folderBrowser.SelectedPath;
            }
        }

        private void buttonBrowseOutputFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            folderBrowser.Description = "Select Output Folder";
            folderBrowser.SelectedPath = textBoxOutputFolder.Text;
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                textBoxOutputFolder.Text = folderBrowser.SelectedPath;
                if (CheckOutputFolderExists())
                {
                    CheckOutputFolderIsEmpty();
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void contentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string helpFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "midi_file_converter.html");
            try
            {
                System.Diagnostics.Process.Start(helpFilePath);
            }
            catch (Win32Exception)
            {
                MessageBox.Show("Could not display the help file", Application.ProductName);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NAudio.Utils.AboutForm aboutForm = new NAudio.Utils.AboutForm();
            aboutForm.ShowDialog();
        }

        private void clearLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            progressLog1.ClearLog();
            saveLogToolStripMenuItem.Enabled = false;
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (workQueued)
            {
                MessageBox.Show("Please wait until the current operation has finished", Application.ProductName);
            }
            else
            {
                AdvancedOptionsForm optionsForm = new AdvancedOptionsForm();
                optionsForm.ShowDialog();
            }
        }

        private void saveLogToolStripMenuItem_Click(object sender, EventArgs args)
        {
            if (workQueued)
            {
                MessageBox.Show("Please wait until the current operation has finished", Application.ProductName);
            }
            else
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.InitialDirectory = textBoxOutputFolder.Text;
                saveFileDialog.DefaultExt = ".txt";
                saveFileDialog.FileName = "Conversion Log.txt";
                saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
                saveFileDialog.FilterIndex = 1;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        
                        using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                        {
                            string text = progressLog1.Text;
                            if (!text.Contains("\r"))
                            {
                                text = text.Replace("\n", "\r\n");
                            }
                            writer.Write(text);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(
                            String.Format("Error saving conversion log\r\n{0}", e.Message),
                            Application.ProductName,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }

                }
            }

        }
    }
}