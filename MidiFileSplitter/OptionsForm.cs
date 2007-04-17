using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MarkHeath.MidiUtils.Properties;

namespace MarkHeath.MidiUtils
{
    public partial class OptionsForm : Form
    {
        private Settings settings = Settings.Default;

        public OptionsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            switch (settings.OutputFolder)
            {
                case OutputFolderSettings.SameFolder:
                    radioButtonSameDirectory.Checked = true;
                    break;
                case OutputFolderSettings.SubFolder:
                    radioButtonSubdir.Checked = true;
                    break;
                case OutputFolderSettings.CustomFolder:
                    radioButtonCustom.Checked = true;
                    break;
                case OutputFolderSettings.CustomSubFolder:
                    radioButtonCustomSubdir.Checked = true;
                    break;
            }
            checkBoxUnique.Checked = settings.UniqueFilename;
            string customFolder = settings.CustomFolder;
            if (customFolder.Length == 0)
                customFolder = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Desktop);
            textBoxCustomFolder.Text = customFolder;

            switch (settings.NoteDuration)
            {
                case NoteDurationSettings.DoNotModify:
                    radioButtonDoNotModify.Checked = true;
                    break;
                case NoteDurationSettings.Truncate:
                    radioButtonTruncate.Checked = true;
                    break;
                case NoteDurationSettings.ConstantLength:
                    radioButtonSameLength.Checked = true;
                    break;
            }
            textBoxNoteLength.Text = settings.FixedNoteLength.ToString();

            if (settings.MidiFileType == 0)
                radioButtonType0.Checked = true;
            else
                radioButtonType1.Checked = true;

            if (settings.ModifyChannel)
                radioButtonSameChannel.Checked = true;
            else
                radioButtonDoNotModifyChannel.Checked = true;
            textBoxChannel.Text = settings.FixedChannel.ToString();
            checkBoxTextEventsAsMarkers.Checked = settings.TextEventMarkers;
            checkBoxAllowOrphanedNoteEvents.Checked = settings.AllowOrphanedNoteEvents;
            checkBoxLyricsAsMarkers.Checked = settings.LyricsAsMarkers;
        }

        private void buttonSelectCustomFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            folderBrowser.SelectedPath = textBoxCustomFolder.Text;
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                textBoxCustomFolder.Text = folderBrowser.SelectedPath;
            }
        }

        private void UpdateSettings()
        {
            settings.CustomFolder = textBoxCustomFolder.Text;
            settings.UniqueFilename = checkBoxUnique.Checked;
            if (radioButtonSameDirectory.Checked)
                settings.OutputFolder = OutputFolderSettings.SameFolder;
            else if (radioButtonSubdir.Checked)
                settings.OutputFolder = OutputFolderSettings.SubFolder;
            else if (radioButtonCustom.Checked)
                settings.OutputFolder = OutputFolderSettings.CustomFolder;
            else if (radioButtonCustomSubdir.Checked)
                settings.OutputFolder = OutputFolderSettings.CustomSubFolder;


            if(radioButtonDoNotModify.Checked)
            {
                settings.NoteDuration = NoteDurationSettings.DoNotModify;                
            }
            else if(radioButtonTruncate.Checked)
            {
                settings.NoteDuration = NoteDurationSettings.Truncate;
            }
            else if(radioButtonSameLength.Checked)
            {
                settings.NoteDuration = NoteDurationSettings.ConstantLength;
            }

            settings.MidiFileType = radioButtonType0.Checked ? 0 : 1;

            settings.FixedNoteLength = Int32.Parse(textBoxNoteLength.Text);

            settings.ModifyChannel = radioButtonSameChannel.Checked;
            settings.FixedChannel = Int32.Parse(textBoxChannel.Text);
            settings.TextEventMarkers = checkBoxTextEventsAsMarkers.Checked;
            settings.AllowOrphanedNoteEvents = checkBoxAllowOrphanedNoteEvents.Checked;
            settings.LyricsAsMarkers = checkBoxLyricsAsMarkers.Checked;
        }

        private bool ValidateInput()
        {
            int noteLength;

            if (!Int32.TryParse(textBoxNoteLength.Text, out noteLength))
            {
                MessageBox.Show("Please enter a number between 1 and 10000 for fixed note length", Application.ProductName);
                textBoxNoteLength.Focus();
                return false;
            }
            else if(noteLength <= 0 || noteLength > 10000)
            {
                MessageBox.Show("Please enter a number between 1 and 10000 for fixed note length", Application.ProductName);
                textBoxNoteLength.Focus();
                return false;
            }

            int channel;

            if (!Int32.TryParse(textBoxChannel.Text, out channel))
            {
                MessageBox.Show("Please enter a number between 1 and 16 for fixed channel", Application.ProductName);
                textBoxChannel.Focus();
                return false;
            }
            else if (channel <= 0 || channel > 16)
            {
                MessageBox.Show("Please enter a number between 1 and 16 for fixed channel", Application.ProductName);
                textBoxChannel.Focus();
                return false;
            }

            return true;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                UpdateSettings();
                settings.Save();
            }
            this.Close();
        }
    }
}