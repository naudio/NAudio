using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NAudio.Utils
{
    /// <summary>
    /// A standard about form
    /// </summary>
    public partial class AboutForm : Form
    {
        /// <summary>
        /// Creates a new about form
        /// </summary>
        public AboutForm()
        {
            InitializeComponent();
            labelProductName.Text = Application.ProductName;
            labelVersion.Text = String.Format("Version: {0}", Application.ProductVersion);
            this.Text = String.Format("About {0}", Application.ProductName);
        }

        private void linkLabelWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(linkLabelWebsite.Text);
        }

        private void linkLabelFeedback_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("mailto:" + linkLabelFeedback.Text);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// The URL of the website to use for help
        /// e.g. http://www.codeplex.com/naudio
        /// </summary>
        public string Url
        {
            get { return linkLabelWebsite.Text; }
            set { linkLabelWebsite.Text = value; }
        }

        /// <summary>
        /// The email address for feedback
        /// </summary>
        public string Email
        {
            get { return linkLabelFeedback.Text; }
            set { linkLabelFeedback.Text = value; }
        }

        /// <summary>
        /// The copyright info
        /// e.g. Copyright © 2007 Mark Heath
        /// </summary>
        public string Copyright
        {
            get { return labelCopyright.Text; }
            set { labelCopyright.Text = value; }
        }
    }
}