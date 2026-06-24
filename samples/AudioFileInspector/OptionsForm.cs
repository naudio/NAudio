using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NAudio.Utils;

namespace AudioFileInspector
{
    partial class OptionsForm : Form
    {
        IEnumerable<IAudioFileInspector> inspectors;

        public OptionsForm(IEnumerable<IAudioFileInspector> inspectors)
        {
            InitializeComponent();
            this.inspectors = inspectors;
        }

        private void buttonAssociate_Click(object sender, EventArgs args)
        {
            try
            {
                Associate(inspectors);
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    String.Format("Unable to create file associations\r\n{0}", e),
                    Application.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

        }

        private void buttonDisassociate_Click(object sender, EventArgs args)
        {
            try
            {
                Disassociate(inspectors);
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    String.Format("Unable to remove file associations\r\n{0}", e),
                    Application.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public static void Disassociate(IEnumerable<IAudioFileInspector> inspectors)
        {
            foreach (IAudioFileInspector inspector in inspectors)
            {
                if (!FileAssociations.IsFileTypeRegistered(inspector.FileExtension))
                {
                    FileAssociations.RegisterFileType(inspector.FileExtension, inspector.FileTypeDescription, null);
                }
                string command = "\"" + Application.ExecutablePath + "\" \"%1\"";
                FileAssociations.RemoveAction(
                    inspector.FileExtension,
                    "AudioFileInspector");
            }
        }

        public static void Associate(IEnumerable<IAudioFileInspector> inspectors)
        {
            foreach (IAudioFileInspector inspector in inspectors)
            {
                if (!FileAssociations.IsFileTypeRegistered(inspector.FileExtension))
                {
                    FileAssociations.RegisterFileType(inspector.FileExtension, inspector.FileTypeDescription, null);
                }
                string command = "\"" + Application.ExecutablePath + "\" \"%1\"";
                FileAssociations.AddAction(
                    inspector.FileExtension,
                    "AudioFileInspector",
                    "Describe",
                    command);
            }
        }
    }
}