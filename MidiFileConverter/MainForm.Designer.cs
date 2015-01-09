namespace MarkHeath.MidiUtils
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxInputFolder = new System.Windows.Forms.TextBox();
            this.textBoxOutputFolder = new System.Windows.Forms.TextBox();
            this.buttonBrowseEZDrummer = new System.Windows.Forms.Button();
            this.buttonBrowseOutputFolder = new System.Windows.Forms.Button();
            this.buttonConvert = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.checkBoxVerbose = new System.Windows.Forms.CheckBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioButtonType1 = new System.Windows.Forms.RadioButton();
            this.radioButtonTypeUnchanged = new System.Windows.Forms.RadioButton();
            this.radioButtonType0 = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButtonChannel10 = new System.Windows.Forms.RadioButton();
            this.radioButtonChannelUnchanged = new System.Windows.Forms.RadioButton();
            this.radioButtonChannel1 = new System.Windows.Forms.RadioButton();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.checkBoxUseFilename = new System.Windows.Forms.CheckBox();
            this.checkBoxApplyNamingRules = new System.Windows.Forms.CheckBox();
            this.progressLog1 = new NAudio.Utils.ProgressLog();
            this.menuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Input Folder:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Output Folder:";
            // 
            // textBoxInputFolder
            // 
            this.textBoxInputFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxInputFolder.Location = new System.Drawing.Point(144, 31);
            this.textBoxInputFolder.Name = "textBoxInputFolder";
            this.textBoxInputFolder.ReadOnly = true;
            this.textBoxInputFolder.Size = new System.Drawing.Size(310, 20);
            this.textBoxInputFolder.TabIndex = 1;
            // 
            // textBoxOutputFolder
            // 
            this.textBoxOutputFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOutputFolder.Location = new System.Drawing.Point(144, 61);
            this.textBoxOutputFolder.Name = "textBoxOutputFolder";
            this.textBoxOutputFolder.ReadOnly = true;
            this.textBoxOutputFolder.Size = new System.Drawing.Size(310, 20);
            this.textBoxOutputFolder.TabIndex = 4;
            // 
            // buttonBrowseEZDrummer
            // 
            this.buttonBrowseEZDrummer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowseEZDrummer.Location = new System.Drawing.Point(460, 29);
            this.buttonBrowseEZDrummer.Name = "buttonBrowseEZDrummer";
            this.buttonBrowseEZDrummer.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowseEZDrummer.TabIndex = 2;
            this.buttonBrowseEZDrummer.Text = "Browse...";
            this.buttonBrowseEZDrummer.UseVisualStyleBackColor = true;
            this.buttonBrowseEZDrummer.Click += new System.EventHandler(this.buttonBrowseEZDrummer_Click);
            // 
            // buttonBrowseOutputFolder
            // 
            this.buttonBrowseOutputFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowseOutputFolder.Location = new System.Drawing.Point(460, 59);
            this.buttonBrowseOutputFolder.Name = "buttonBrowseOutputFolder";
            this.buttonBrowseOutputFolder.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowseOutputFolder.TabIndex = 5;
            this.buttonBrowseOutputFolder.Text = "Browse...";
            this.buttonBrowseOutputFolder.UseVisualStyleBackColor = true;
            this.buttonBrowseOutputFolder.Click += new System.EventHandler(this.buttonBrowseOutputFolder_Click);
            // 
            // buttonConvert
            // 
            this.buttonConvert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonConvert.Location = new System.Drawing.Point(460, 204);
            this.buttonConvert.Name = "buttonConvert";
            this.buttonConvert.Size = new System.Drawing.Size(75, 23);
            this.buttonConvert.TabIndex = 11;
            this.buttonConvert.Text = "Convert";
            this.buttonConvert.UseVisualStyleBackColor = true;
            this.buttonConvert.Click += new System.EventHandler(this.buttonConvert_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 209);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(319, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "When you have verified the above settings, click Convert to begin";
            // 
            // checkBoxVerbose
            // 
            this.checkBoxVerbose.AutoSize = true;
            this.checkBoxVerbose.Checked = true;
            this.checkBoxVerbose.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxVerbose.Location = new System.Drawing.Point(351, 208);
            this.checkBoxVerbose.Name = "checkBoxVerbose";
            this.checkBoxVerbose.Size = new System.Drawing.Size(100, 17);
            this.checkBoxVerbose.TabIndex = 10;
            this.checkBoxVerbose.Text = "Verbose Output";
            this.checkBoxVerbose.UseVisualStyleBackColor = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(551, 24);
            this.menuStrip1.TabIndex = 10;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem,
            this.clearLogToolStripMenuItem,
            this.saveLogToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.toolsToolStripMenuItem.Text = "&Tools";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.optionsToolStripMenuItem.Text = "Advanced &Options...";
            this.optionsToolStripMenuItem.Click += new System.EventHandler(this.optionsToolStripMenuItem_Click);
            // 
            // clearLogToolStripMenuItem
            // 
            this.clearLogToolStripMenuItem.Name = "clearLogToolStripMenuItem";
            this.clearLogToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.clearLogToolStripMenuItem.Text = "&Clear Log";
            this.clearLogToolStripMenuItem.Click += new System.EventHandler(this.clearLogToolStripMenuItem_Click);
            // 
            // saveLogToolStripMenuItem
            // 
            this.saveLogToolStripMenuItem.Enabled = false;
            this.saveLogToolStripMenuItem.Name = "saveLogToolStripMenuItem";
            this.saveLogToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.saveLogToolStripMenuItem.Text = "&Save Log...";
            this.saveLogToolStripMenuItem.Click += new System.EventHandler(this.saveLogToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contentsToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // contentsToolStripMenuItem
            // 
            this.contentsToolStripMenuItem.Name = "contentsToolStripMenuItem";
            this.contentsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.contentsToolStripMenuItem.Text = "&Contents";
            this.contentsToolStripMenuItem.Click += new System.EventHandler(this.contentsToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButtonType1);
            this.groupBox1.Controls.Add(this.radioButtonTypeUnchanged);
            this.groupBox1.Controls.Add(this.radioButtonType0);
            this.groupBox1.Location = new System.Drawing.Point(337, 96);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(148, 100);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Output File Type";
            // 
            // radioButtonType1
            // 
            this.radioButtonType1.AutoSize = true;
            this.radioButtonType1.Checked = true;
            this.radioButtonType1.Location = new System.Drawing.Point(6, 65);
            this.radioButtonType1.Name = "radioButtonType1";
            this.radioButtonType1.Size = new System.Drawing.Size(84, 17);
            this.radioButtonType1.TabIndex = 2;
            this.radioButtonType1.TabStop = true;
            this.radioButtonType1.Text = "MIDI Type 1";
            this.radioButtonType1.UseVisualStyleBackColor = true;
            // 
            // radioButtonTypeUnchanged
            // 
            this.radioButtonTypeUnchanged.AutoSize = true;
            this.radioButtonTypeUnchanged.Location = new System.Drawing.Point(6, 19);
            this.radioButtonTypeUnchanged.Name = "radioButtonTypeUnchanged";
            this.radioButtonTypeUnchanged.Size = new System.Drawing.Size(114, 17);
            this.radioButtonTypeUnchanged.TabIndex = 0;
            this.radioButtonTypeUnchanged.Text = "Leave Unchanged";
            this.radioButtonTypeUnchanged.UseVisualStyleBackColor = true;
            // 
            // radioButtonType0
            // 
            this.radioButtonType0.AutoSize = true;
            this.radioButtonType0.Location = new System.Drawing.Point(6, 42);
            this.radioButtonType0.Name = "radioButtonType0";
            this.radioButtonType0.Size = new System.Drawing.Size(84, 17);
            this.radioButtonType0.TabIndex = 1;
            this.radioButtonType0.Text = "MIDI Type 0";
            this.radioButtonType0.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButtonChannel10);
            this.groupBox2.Controls.Add(this.radioButtonChannelUnchanged);
            this.groupBox2.Controls.Add(this.radioButtonChannel1);
            this.groupBox2.Location = new System.Drawing.Point(187, 96);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(144, 100);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "MIDI Note Channel";
            // 
            // radioButtonChannel10
            // 
            this.radioButtonChannel10.AutoSize = true;
            this.radioButtonChannel10.Location = new System.Drawing.Point(6, 65);
            this.radioButtonChannel10.Name = "radioButtonChannel10";
            this.radioButtonChannel10.Size = new System.Drawing.Size(134, 17);
            this.radioButtonChannel10.TabIndex = 5;
            this.radioButtonChannel10.Text = "Force all to Channel 10";
            this.radioButtonChannel10.UseVisualStyleBackColor = true;
            // 
            // radioButtonChannelUnchanged
            // 
            this.radioButtonChannelUnchanged.AutoSize = true;
            this.radioButtonChannelUnchanged.Checked = true;
            this.radioButtonChannelUnchanged.Location = new System.Drawing.Point(6, 19);
            this.radioButtonChannelUnchanged.Name = "radioButtonChannelUnchanged";
            this.radioButtonChannelUnchanged.Size = new System.Drawing.Size(114, 17);
            this.radioButtonChannelUnchanged.TabIndex = 3;
            this.radioButtonChannelUnchanged.TabStop = true;
            this.radioButtonChannelUnchanged.Text = "Leave Unchanged";
            this.radioButtonChannelUnchanged.UseVisualStyleBackColor = true;
            // 
            // radioButtonChannel1
            // 
            this.radioButtonChannel1.AutoSize = true;
            this.radioButtonChannel1.Location = new System.Drawing.Point(6, 42);
            this.radioButtonChannel1.Name = "radioButtonChannel1";
            this.radioButtonChannel1.Size = new System.Drawing.Size(128, 17);
            this.radioButtonChannel1.TabIndex = 4;
            this.radioButtonChannel1.Text = "Force all to Channel 1";
            this.radioButtonChannel1.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.checkBoxUseFilename);
            this.groupBox4.Controls.Add(this.checkBoxApplyNamingRules);
            this.groupBox4.Location = new System.Drawing.Point(15, 96);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(163, 100);
            this.groupBox4.TabIndex = 6;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Clip Naming";
            // 
            // checkBoxUseFilename
            // 
            this.checkBoxUseFilename.AutoSize = true;
            this.checkBoxUseFilename.Checked = true;
            this.checkBoxUseFilename.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxUseFilename.Location = new System.Drawing.Point(6, 55);
            this.checkBoxUseFilename.Name = "checkBoxUseFilename";
            this.checkBoxUseFilename.Size = new System.Drawing.Size(117, 30);
            this.checkBoxUseFilename.TabIndex = 2;
            this.checkBoxUseFilename.Text = "Use filename for\r\nother MIDI patterns";
            this.checkBoxUseFilename.UseVisualStyleBackColor = true;
            // 
            // checkBoxApplyNamingRules
            // 
            this.checkBoxApplyNamingRules.AutoSize = true;
            this.checkBoxApplyNamingRules.Checked = true;
            this.checkBoxApplyNamingRules.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxApplyNamingRules.Location = new System.Drawing.Point(6, 19);
            this.checkBoxApplyNamingRules.Name = "checkBoxApplyNamingRules";
            this.checkBoxApplyNamingRules.Size = new System.Drawing.Size(153, 30);
            this.checkBoxApplyNamingRules.TabIndex = 1;
            this.checkBoxApplyNamingRules.Text = "Apply XML naming rules\r\nto Toontrack EZD patterns";
            this.checkBoxApplyNamingRules.UseVisualStyleBackColor = true;
            // 
            // progressLog1
            // 
            this.progressLog1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressLog1.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.progressLog1.Location = new System.Drawing.Point(15, 234);
            this.progressLog1.Name = "progressLog1";
            this.progressLog1.Padding = new System.Windows.Forms.Padding(1);
            this.progressLog1.Size = new System.Drawing.Size(520, 161);
            this.progressLog1.TabIndex = 12;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(551, 407);
            this.Controls.Add(this.progressLog1);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.checkBoxVerbose);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.buttonConvert);
            this.Controls.Add(this.buttonBrowseOutputFolder);
            this.Controls.Add(this.buttonBrowseEZDrummer);
            this.Controls.Add(this.textBoxOutputFolder);
            this.Controls.Add(this.textBoxInputFolder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(559, 441);
            this.Name = "MainForm";
            this.Text = "MIDI File Converter";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxInputFolder;
        private System.Windows.Forms.TextBox textBoxOutputFolder;
        private System.Windows.Forms.Button buttonBrowseEZDrummer;
        private System.Windows.Forms.Button buttonBrowseOutputFolder;
        private System.Windows.Forms.Button buttonConvert;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxVerbose;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contentsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButtonType1;
        private System.Windows.Forms.RadioButton radioButtonTypeUnchanged;
        private System.Windows.Forms.RadioButton radioButtonType0;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButtonChannel10;
        private System.Windows.Forms.RadioButton radioButtonChannelUnchanged;
        private System.Windows.Forms.RadioButton radioButtonChannel1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox checkBoxUseFilename;
        private System.Windows.Forms.CheckBox checkBoxApplyNamingRules;
        private NAudio.Utils.ProgressLog progressLog1;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveLogToolStripMenuItem;
    }
}

