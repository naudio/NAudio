using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel.Composition;

namespace AudioFileInspector
{
    [Export(typeof(AudioFileInspectorForm))]
    public partial class AudioFileInspectorForm : Form
    {
        
        public ICollection<IAudioFileInspector> Inspectors { get; private set; }
        string filterString;
        int filterIndex;
        string currentFile;
        FindForm findForm;

        public string[] CommandLineArguments { get; set; }

        [ImportingConstructor]
        public AudioFileInspectorForm([ImportMany(typeof(IAudioFileInspector))] IEnumerable<IAudioFileInspector> inspectors)
        {
            InitializeComponent();
            this.Inspectors = new List<IAudioFileInspector>(inspectors);
        }

        private void DescribeFile(string fileName)
        {
            currentFile = fileName;
            textLog.Text = String.Format("Opening {0}\r\n", fileName);

            try
            {
                string extension = System.IO.Path.GetExtension(fileName).ToLower();
                bool described = false;
                foreach (IAudioFileInspector inspector in Inspectors)
                {
                    if (extension == inspector.FileExtension)
                    {
                        textLog.AppendText(inspector.Describe(fileName));
                        described = true;
                        break;
                    }
                }
                if (!described)
                {
                    textLog.AppendText("Unrecognised file type");
                }
            }
            catch (Exception ex)
            {
                textLog.AppendText(ex.ToString());
            }
        }

        private void CreateFilterString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (Inspectors.Count > 0)
            {
                stringBuilder.Append("All Supported Files|");
                foreach (IAudioFileInspector inspector in Inspectors)
                {
                    stringBuilder.AppendFormat("*{0};", inspector.FileExtension);
                }
                stringBuilder.Length--;
                stringBuilder.Append("|");
                foreach (IAudioFileInspector inspector in Inspectors)
                {
                    stringBuilder.AppendFormat("{0}|*{1}|", inspector.FileTypeDescription, inspector.FileExtension);
                }
            }
            stringBuilder.Append("All files (*.*)|*.*");
            filterString = stringBuilder.ToString();
            filterIndex = 1;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.Filter = filterString;
            ofd.FilterIndex = filterIndex;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                filterIndex = ofd.FilterIndex;
                DescribeFile(ofd.FileName);
                textLog.SelectionStart = 0;
                textLog.SelectionLength = 0;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NAudio.Utils.AboutForm aboutForm = new NAudio.Utils.AboutForm();
            aboutForm.ShowDialog();
        }

        private void AudioFileInspectorForm_Load(object sender, EventArgs e)
        {
            CreateFilterString();
            if (CommandLineArguments != null && CommandLineArguments.Length > 0)
            {
                DescribeFile(CommandLineArguments[0]);
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsForm optionsForm = new OptionsForm(Inspectors);
            optionsForm.ShowDialog();

        }

        private void AudioFileInspectorForm_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void AudioFileInspectorForm_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }            
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                DescribeFile(files[0]);
            }
        }

        private void saveLogToolStripMenuItem_Click(object sender, EventArgs args)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = ".txt";
            if (currentFile != null)
            {
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(currentFile);
                saveFileDialog.FileName = Path.GetFileNameWithoutExtension(currentFile) + ".txt";
            }
            saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
            saveFileDialog.FilterIndex = 1;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {

                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        string text = textLog.Text;
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

        private void contentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string helpFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "audio_file_inspector.html");
            try
            {
                System.Diagnostics.Process.Start(helpFilePath);
            }
            catch (Win32Exception)
            {
                MessageBox.Show("Could not display the help file", Application.ProductName);
            }
        }

        private void clearLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textLog.Clear();
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (findForm == null)
            {
                findForm = new FindForm(textLog);
                findForm.Disposed += new EventHandler(findForm_Disposed);
            }
            findForm.Show(this);
        }

        void findForm_Disposed(object sender, EventArgs e)
        {
            findForm = null;
        }

        private void AudioFileInspectorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (findForm != null)
            {
                findForm.Close();
            }
        }

    }
}