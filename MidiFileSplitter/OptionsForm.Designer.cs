namespace MarkHeath.MidiUtils
{
    partial class OptionsForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBoxCustomFolder = new System.Windows.Forms.TextBox();
            this.labelCustomFolder = new System.Windows.Forms.Label();
            this.buttonSelectCustomFolder = new System.Windows.Forms.Button();
            this.radioButtonSubdir = new System.Windows.Forms.RadioButton();
            this.radioButtonCustomSubdir = new System.Windows.Forms.RadioButton();
            this.radioButtonCustom = new System.Windows.Forms.RadioButton();
            this.radioButtonSameDirectory = new System.Windows.Forms.RadioButton();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.groupBoxDuration = new System.Windows.Forms.GroupBox();
            this.textBoxNoteLength = new System.Windows.Forms.TextBox();
            this.radioButtonSameLength = new System.Windows.Forms.RadioButton();
            this.radioButtonTruncate = new System.Windows.Forms.RadioButton();
            this.radioButtonDoNotModify = new System.Windows.Forms.RadioButton();
            this.groupBoxChannel = new System.Windows.Forms.GroupBox();
            this.textBoxChannel = new System.Windows.Forms.TextBox();
            this.radioButtonDoNotModifyChannel = new System.Windows.Forms.RadioButton();
            this.radioButtonSameChannel = new System.Windows.Forms.RadioButton();
            this.groupBoxOutputFileSettings = new System.Windows.Forms.GroupBox();
            this.radioButtonType1 = new System.Windows.Forms.RadioButton();
            this.radioButtonType0 = new System.Windows.Forms.RadioButton();
            this.checkBoxUnique = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.checkBoxTextEventsAsMarkers = new System.Windows.Forms.CheckBox();
            this.checkBoxAllowOrphanedNoteEvents = new System.Windows.Forms.CheckBox();
            this.checkBoxLyricsAsMarkers = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBoxDuration.SuspendLayout();
            this.groupBoxChannel.SuspendLayout();
            this.groupBoxOutputFileSettings.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.textBoxCustomFolder);
            this.groupBox1.Controls.Add(this.labelCustomFolder);
            this.groupBox1.Controls.Add(this.buttonSelectCustomFolder);
            this.groupBox1.Controls.Add(this.radioButtonSubdir);
            this.groupBox1.Controls.Add(this.radioButtonCustomSubdir);
            this.groupBox1.Controls.Add(this.radioButtonCustom);
            this.groupBox1.Controls.Add(this.radioButtonSameDirectory);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(387, 149);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Output Folder Settings";
            // 
            // textBoxCustomFolder
            // 
            this.textBoxCustomFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxCustomFolder.Location = new System.Drawing.Point(87, 123);
            this.textBoxCustomFolder.Name = "textBoxCustomFolder";
            this.textBoxCustomFolder.ReadOnly = true;
            this.textBoxCustomFolder.Size = new System.Drawing.Size(213, 20);
            this.textBoxCustomFolder.TabIndex = 6;
            // 
            // labelCustomFolder
            // 
            this.labelCustomFolder.AutoSize = true;
            this.labelCustomFolder.Location = new System.Drawing.Point(4, 125);
            this.labelCustomFolder.Name = "labelCustomFolder";
            this.labelCustomFolder.Size = new System.Drawing.Size(77, 13);
            this.labelCustomFolder.TabIndex = 5;
            this.labelCustomFolder.Text = "Custom Folder:";
            // 
            // buttonSelectCustomFolder
            // 
            this.buttonSelectCustomFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelectCustomFolder.Location = new System.Drawing.Point(306, 120);
            this.buttonSelectCustomFolder.Name = "buttonSelectCustomFolder";
            this.buttonSelectCustomFolder.Size = new System.Drawing.Size(75, 23);
            this.buttonSelectCustomFolder.TabIndex = 7;
            this.buttonSelectCustomFolder.Text = "Select...";
            this.buttonSelectCustomFolder.UseVisualStyleBackColor = true;
            this.buttonSelectCustomFolder.Click += new System.EventHandler(this.buttonSelectCustomFolder_Click);
            // 
            // radioButtonSubdir
            // 
            this.radioButtonSubdir.AutoSize = true;
            this.radioButtonSubdir.Checked = true;
            this.radioButtonSubdir.Location = new System.Drawing.Point(7, 43);
            this.radioButtonSubdir.Name = "radioButtonSubdir";
            this.radioButtonSubdir.Size = new System.Drawing.Size(225, 17);
            this.radioButtonSubdir.TabIndex = 2;
            this.radioButtonSubdir.TabStop = true;
            this.radioButtonSubdir.Text = "Create a new subfolder named by filename";
            this.radioButtonSubdir.UseVisualStyleBackColor = true;
            // 
            // radioButtonCustomSubdir
            // 
            this.radioButtonCustomSubdir.AutoSize = true;
            this.radioButtonCustomSubdir.Location = new System.Drawing.Point(7, 89);
            this.radioButtonCustomSubdir.Name = "radioButtonCustomSubdir";
            this.radioButtonCustomSubdir.Size = new System.Drawing.Size(248, 17);
            this.radioButtonCustomSubdir.TabIndex = 4;
            this.radioButtonCustomSubdir.Text = "Custom folder plus subfolder named by filename";
            this.radioButtonCustomSubdir.UseVisualStyleBackColor = true;
            // 
            // radioButtonCustom
            // 
            this.radioButtonCustom.AutoSize = true;
            this.radioButtonCustom.Location = new System.Drawing.Point(7, 66);
            this.radioButtonCustom.Name = "radioButtonCustom";
            this.radioButtonCustom.Size = new System.Drawing.Size(89, 17);
            this.radioButtonCustom.TabIndex = 3;
            this.radioButtonCustom.Text = "Custom folder";
            this.radioButtonCustom.UseVisualStyleBackColor = true;
            // 
            // radioButtonSameDirectory
            // 
            this.radioButtonSameDirectory.AutoSize = true;
            this.radioButtonSameDirectory.Location = new System.Drawing.Point(7, 20);
            this.radioButtonSameDirectory.Name = "radioButtonSameDirectory";
            this.radioButtonSameDirectory.Size = new System.Drawing.Size(81, 17);
            this.radioButtonSameDirectory.TabIndex = 1;
            this.radioButtonSameDirectory.Text = "Same folder";
            this.radioButtonSameDirectory.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(244, 536);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 21;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(325, 536);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 22;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // groupBoxDuration
            // 
            this.groupBoxDuration.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxDuration.Controls.Add(this.textBoxNoteLength);
            this.groupBoxDuration.Controls.Add(this.radioButtonSameLength);
            this.groupBoxDuration.Controls.Add(this.radioButtonTruncate);
            this.groupBoxDuration.Controls.Add(this.radioButtonDoNotModify);
            this.groupBoxDuration.Location = new System.Drawing.Point(12, 242);
            this.groupBoxDuration.Name = "groupBoxDuration";
            this.groupBoxDuration.Size = new System.Drawing.Size(387, 100);
            this.groupBoxDuration.TabIndex = 10;
            this.groupBoxDuration.TabStop = false;
            this.groupBoxDuration.Text = "Note Durations";
            // 
            // textBoxNoteLength
            // 
            this.textBoxNoteLength.Location = new System.Drawing.Point(143, 67);
            this.textBoxNoteLength.Name = "textBoxNoteLength";
            this.textBoxNoteLength.Size = new System.Drawing.Size(47, 20);
            this.textBoxNoteLength.TabIndex = 14;
            this.textBoxNoteLength.Text = "1";
            // 
            // radioButtonSameLength
            // 
            this.radioButtonSameLength.AutoSize = true;
            this.radioButtonSameLength.Location = new System.Drawing.Point(7, 66);
            this.radioButtonSameLength.Name = "radioButtonSameLength";
            this.radioButtonSameLength.Size = new System.Drawing.Size(129, 17);
            this.radioButtonSameLength.TabIndex = 13;
            this.radioButtonSameLength.Text = "Set all to same length:";
            this.radioButtonSameLength.UseVisualStyleBackColor = true;
            // 
            // radioButtonTruncate
            // 
            this.radioButtonTruncate.AutoSize = true;
            this.radioButtonTruncate.Location = new System.Drawing.Point(7, 43);
            this.radioButtonTruncate.Name = "radioButtonTruncate";
            this.radioButtonTruncate.Size = new System.Drawing.Size(174, 17);
            this.radioButtonTruncate.TabIndex = 12;
            this.radioButtonTruncate.Text = "Truncate to before next marker ";
            this.radioButtonTruncate.UseVisualStyleBackColor = true;
            // 
            // radioButtonDoNotModify
            // 
            this.radioButtonDoNotModify.AutoSize = true;
            this.radioButtonDoNotModify.Checked = true;
            this.radioButtonDoNotModify.Location = new System.Drawing.Point(7, 20);
            this.radioButtonDoNotModify.Name = "radioButtonDoNotModify";
            this.radioButtonDoNotModify.Size = new System.Drawing.Size(90, 17);
            this.radioButtonDoNotModify.TabIndex = 11;
            this.radioButtonDoNotModify.TabStop = true;
            this.radioButtonDoNotModify.Text = "Do not modify";
            this.radioButtonDoNotModify.UseVisualStyleBackColor = true;
            // 
            // groupBoxChannel
            // 
            this.groupBoxChannel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxChannel.Controls.Add(this.textBoxChannel);
            this.groupBoxChannel.Controls.Add(this.radioButtonDoNotModifyChannel);
            this.groupBoxChannel.Controls.Add(this.radioButtonSameChannel);
            this.groupBoxChannel.Location = new System.Drawing.Point(12, 348);
            this.groupBoxChannel.Name = "groupBoxChannel";
            this.groupBoxChannel.Size = new System.Drawing.Size(387, 78);
            this.groupBoxChannel.TabIndex = 15;
            this.groupBoxChannel.TabStop = false;
            this.groupBoxChannel.Text = "MIDI Event Channel Settings";
            // 
            // textBoxChannel
            // 
            this.textBoxChannel.Location = new System.Drawing.Point(142, 39);
            this.textBoxChannel.Name = "textBoxChannel";
            this.textBoxChannel.Size = new System.Drawing.Size(47, 20);
            this.textBoxChannel.TabIndex = 18;
            this.textBoxChannel.Text = "1";
            // 
            // radioButtonDoNotModifyChannel
            // 
            this.radioButtonDoNotModifyChannel.AutoSize = true;
            this.radioButtonDoNotModifyChannel.Checked = true;
            this.radioButtonDoNotModifyChannel.Location = new System.Drawing.Point(7, 19);
            this.radioButtonDoNotModifyChannel.Name = "radioButtonDoNotModifyChannel";
            this.radioButtonDoNotModifyChannel.Size = new System.Drawing.Size(90, 17);
            this.radioButtonDoNotModifyChannel.TabIndex = 16;
            this.radioButtonDoNotModifyChannel.TabStop = true;
            this.radioButtonDoNotModifyChannel.Text = "Do not modify";
            this.radioButtonDoNotModifyChannel.UseVisualStyleBackColor = true;
            // 
            // radioButtonSameChannel
            // 
            this.radioButtonSameChannel.AutoSize = true;
            this.radioButtonSameChannel.Location = new System.Drawing.Point(7, 42);
            this.radioButtonSameChannel.Name = "radioButtonSameChannel";
            this.radioButtonSameChannel.Size = new System.Drawing.Size(138, 17);
            this.radioButtonSameChannel.TabIndex = 17;
            this.radioButtonSameChannel.Text = "Set all to same channel:";
            this.radioButtonSameChannel.UseVisualStyleBackColor = true;
            // 
            // groupBoxOutputFileSettings
            // 
            this.groupBoxOutputFileSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxOutputFileSettings.Controls.Add(this.radioButtonType1);
            this.groupBoxOutputFileSettings.Controls.Add(this.radioButtonType0);
            this.groupBoxOutputFileSettings.Controls.Add(this.checkBoxUnique);
            this.groupBoxOutputFileSettings.Location = new System.Drawing.Point(12, 168);
            this.groupBoxOutputFileSettings.Name = "groupBoxOutputFileSettings";
            this.groupBoxOutputFileSettings.Size = new System.Drawing.Size(387, 68);
            this.groupBoxOutputFileSettings.TabIndex = 8;
            this.groupBoxOutputFileSettings.TabStop = false;
            this.groupBoxOutputFileSettings.Text = "Output File Settings";
            // 
            // radioButtonType1
            // 
            this.radioButtonType1.AutoSize = true;
            this.radioButtonType1.Checked = true;
            this.radioButtonType1.Location = new System.Drawing.Point(147, 42);
            this.radioButtonType1.Name = "radioButtonType1";
            this.radioButtonType1.Size = new System.Drawing.Size(84, 17);
            this.radioButtonType1.TabIndex = 11;
            this.radioButtonType1.TabStop = true;
            this.radioButtonType1.Text = "MIDI Type 1";
            this.radioButtonType1.UseVisualStyleBackColor = true;
            // 
            // radioButtonType0
            // 
            this.radioButtonType0.AutoSize = true;
            this.radioButtonType0.Location = new System.Drawing.Point(7, 42);
            this.radioButtonType0.Name = "radioButtonType0";
            this.radioButtonType0.Size = new System.Drawing.Size(84, 17);
            this.radioButtonType0.TabIndex = 10;
            this.radioButtonType0.Text = "MIDI Type 0";
            this.radioButtonType0.UseVisualStyleBackColor = true;
            // 
            // checkBoxUnique
            // 
            this.checkBoxUnique.Checked = true;
            this.checkBoxUnique.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxUnique.Location = new System.Drawing.Point(7, 15);
            this.checkBoxUnique.Name = "checkBoxUnique";
            this.checkBoxUnique.Size = new System.Drawing.Size(229, 24);
            this.checkBoxUnique.TabIndex = 9;
            this.checkBoxUnique.Text = "Create unique filename if file already exists";
            this.checkBoxUnique.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.checkBoxLyricsAsMarkers);
            this.groupBox2.Controls.Add(this.checkBoxTextEventsAsMarkers);
            this.groupBox2.Controls.Add(this.checkBoxAllowOrphanedNoteEvents);
            this.groupBox2.Location = new System.Drawing.Point(12, 432);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(387, 98);
            this.groupBox2.TabIndex = 19;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Compatibility";
            // 
            // checkBoxTextEventsAsMarkers
            // 
            this.checkBoxTextEventsAsMarkers.AutoSize = true;
            this.checkBoxTextEventsAsMarkers.Location = new System.Drawing.Point(7, 43);
            this.checkBoxTextEventsAsMarkers.Name = "checkBoxTextEventsAsMarkers";
            this.checkBoxTextEventsAsMarkers.Size = new System.Drawing.Size(160, 17);
            this.checkBoxTextEventsAsMarkers.TabIndex = 20;
            this.checkBoxTextEventsAsMarkers.Text = "Treat text events as markers";
            this.checkBoxTextEventsAsMarkers.UseVisualStyleBackColor = true;
            // 
            // checkBoxAllowOrphanedNoteEvents
            // 
            this.checkBoxAllowOrphanedNoteEvents.AutoSize = true;
            this.checkBoxAllowOrphanedNoteEvents.Location = new System.Drawing.Point(7, 20);
            this.checkBoxAllowOrphanedNoteEvents.Name = "checkBoxAllowOrphanedNoteEvents";
            this.checkBoxAllowOrphanedNoteEvents.Size = new System.Drawing.Size(158, 17);
            this.checkBoxAllowOrphanedNoteEvents.TabIndex = 20;
            this.checkBoxAllowOrphanedNoteEvents.Text = "Allow orphaned note events";
            this.checkBoxAllowOrphanedNoteEvents.UseVisualStyleBackColor = true;
            // 
            // checkBoxLyricsAsMarkers
            // 
            this.checkBoxLyricsAsMarkers.AutoSize = true;
            this.checkBoxLyricsAsMarkers.Location = new System.Drawing.Point(7, 66);
            this.checkBoxLyricsAsMarkers.Name = "checkBoxLyricsAsMarkers";
            this.checkBoxLyricsAsMarkers.Size = new System.Drawing.Size(131, 17);
            this.checkBoxLyricsAsMarkers.TabIndex = 20;
            this.checkBoxLyricsAsMarkers.Text = "Treat lyrics as markers";
            this.checkBoxLyricsAsMarkers.UseVisualStyleBackColor = true;
            // 
            // OptionsForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(411, 571);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBoxOutputFileSettings);
            this.Controls.Add(this.groupBoxChannel);
            this.Controls.Add(this.groupBoxDuration);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.groupBox1);
            this.MinimumSize = new System.Drawing.Size(419, 581);
            this.Name = "OptionsForm";
            this.ShowInTaskbar = false;
            this.Text = "Options";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBoxDuration.ResumeLayout(false);
            this.groupBoxDuration.PerformLayout();
            this.groupBoxChannel.ResumeLayout(false);
            this.groupBoxChannel.PerformLayout();
            this.groupBoxOutputFileSettings.ResumeLayout(false);
            this.groupBoxOutputFileSettings.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBoxCustomFolder;
        private System.Windows.Forms.Label labelCustomFolder;
        private System.Windows.Forms.Button buttonSelectCustomFolder;
        private System.Windows.Forms.RadioButton radioButtonSubdir;
        private System.Windows.Forms.RadioButton radioButtonCustomSubdir;
        private System.Windows.Forms.RadioButton radioButtonCustom;
        private System.Windows.Forms.RadioButton radioButtonSameDirectory;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.GroupBox groupBoxDuration;
        private System.Windows.Forms.TextBox textBoxNoteLength;
        private System.Windows.Forms.RadioButton radioButtonSameLength;
        private System.Windows.Forms.RadioButton radioButtonTruncate;
        private System.Windows.Forms.RadioButton radioButtonDoNotModify;
        private System.Windows.Forms.GroupBox groupBoxChannel;
        private System.Windows.Forms.TextBox textBoxChannel;
        private System.Windows.Forms.RadioButton radioButtonDoNotModifyChannel;
        private System.Windows.Forms.RadioButton radioButtonSameChannel;
        private System.Windows.Forms.GroupBox groupBoxOutputFileSettings;
        private System.Windows.Forms.CheckBox checkBoxUnique;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox checkBoxAllowOrphanedNoteEvents;
        private System.Windows.Forms.RadioButton radioButtonType1;
        private System.Windows.Forms.RadioButton radioButtonType0;
        private System.Windows.Forms.CheckBox checkBoxTextEventsAsMarkers;
        private System.Windows.Forms.CheckBox checkBoxLyricsAsMarkers;
    }
}