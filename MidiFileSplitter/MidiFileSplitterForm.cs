using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NAudio.Midi;
using System.IO;
using MarkHeath.MidiUtils.Properties;
using NAudio.Utils;

// notes:
// for MIDI type 1 files
// prunes some event types out
// names track 1 for the marker
// copes with changing tempos and time sigs
// copes with no event on first beat
// copes with a marker at the end with nothing following

namespace MarkHeath.MidiUtils
{
    public partial class MidiFileSplitterForm : Form
    {
        private bool workQueued;
        private Settings settings = Settings.Default;

        public MidiFileSplitterForm()
        {
            InitializeComponent();

            if (Settings.Default.FirstTime)
            {
                UpgradeSettings();
            }
        }

        private void UpgradeSettings()
        {
            string productVersion = (string)settings.GetPreviousVersion("ProductVersion");
            if ((productVersion != null) && (productVersion.Length > 0))
            {
                try
                {
                    settings.CustomFolder = (string)settings.GetPreviousVersion("CustomFolder");
                    settings.UniqueFilename = (bool)settings.GetPreviousVersion("UniqueFilename");
                    settings.OutputFolder = (OutputFolderSettings)settings.GetPreviousVersion("OutputFolder");
                    settings.NoteDuration = (NoteDurationSettings)settings.GetPreviousVersion("NoteDuration");
                    settings.FixedNoteLength = (int)settings.GetPreviousVersion("FixedNoteLength");
                    settings.ModifyChannel = (bool)settings.GetPreviousVersion("ModifyChannel");
                    settings.FixedChannel = (int)settings.GetPreviousVersion("FixedChannel");
                    settings.AllowOrphanedNoteEvents = (bool)settings.GetPreviousVersion("AllowOrphanedNoteEvents");
                    settings.MidiFileType = (int)settings.GetPreviousVersion("MidiFileType");
                    settings.TextEventMarkers = (bool)settings.GetPreviousVersion("TextEventMarkers");
                    settings.LyricsAsMarkers = (bool)settings.GetPreviousVersion("LyricsAsMarkers");
                    // add new settings at the bottom
                }
                catch (System.Configuration.SettingsPropertyNotFoundException)
                {
                }
            }
        }

        private void panelDragTarget_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            ProcessFiles(files);
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
                Settings.Default.FirstTime = false;
                Settings.Default.ProductVersion = Application.ProductVersion;
                Settings.Default.Save();
            }
            base.OnClosing(e);
        }

        private void ProcessFiles(string[] files)
        {
            if (workQueued)
            {
                MessageBox.Show("Please wait until the current operation has finished", Application.ProductName);
            }
            else
            {
                workQueued = System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(ProcessFilesThread), files);
            }
        }        

        private void ProcessFilesThread(object files)
        {
            try
            {
                MidiFileSplitter splitter = new MidiFileSplitter();
                splitter.Progress += new EventHandler<ProgressEventArgs>(splitter_Progress);
                int exported = 0;
                foreach (string file in (string[]) files)
                {
                    if (Path.GetExtension(file).ToLower() == ".mid")
                    {
                        exported += splitter.SplitMidiFile(file);
                    }
                    else
                    {
                        progressLog1.ReportProgress(new ProgressEventArgs(ProgressMessageType.Warning,"Can only open .mid files - " + file));
                    }
                }
                this.Invoke(new FinishedDelegate(ShowFinishedMessage), exported);
                
            }
            finally
            {
                workQueued = false;                
            }
        }
        delegate void FinishedDelegate(int exported);

        void ShowFinishedMessage(int exported)
        {
            MessageBox.Show(String.Format("Extracted {0} MIDI files", exported), Application.ProductName);
        }

        void splitter_Progress(object sender, ProgressEventArgs e)
        {
            progressLog1.ReportProgress(e);
        }

        private void panelDragTarget_DragOver(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {                
                e.Effect = DragDropEffects.Copy;
            }
        }
        
        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Standard MIDI Files (*.mid)|*.mid";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ProcessFiles(openFileDialog.FileNames);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            this.Close();
        }

        private void clearOutputWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            progressLog1.ClearLog();
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsForm optionsForm = new OptionsForm();
            optionsForm.ShowDialog();
        }

        private void contentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //string helpFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "midi_file_splitter.html");
            try
            {
                System.Diagnostics.Process.Start("http://www.codeplex.com/naudio/Wiki/View.aspx?title=MIDI%20File%20Splitter");
            }
            catch (Win32Exception)
            {
                MessageBox.Show("Could not display the help file", Application.ProductName);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

    }
}
