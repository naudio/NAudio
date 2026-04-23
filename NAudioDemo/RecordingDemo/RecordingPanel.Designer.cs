namespace NAudioDemo.RecordingDemo
{
    partial class RecordingPanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RecordingPanel));
            this.buttonStartRecording = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonStopRecording = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.listBoxRecordings = new System.Windows.Forms.ListBox();
            this.buttonPlay = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.buttonOpenFolder = new System.Windows.Forms.Button();
            this.groupBoxRecordingApi = new System.Windows.Forms.GroupBox();
            this.checkBoxEventCallback = new System.Windows.Forms.CheckBox();
            this.comboBoxChannels = new System.Windows.Forms.ComboBox();
            this.comboBoxSampleRate = new System.Windows.Forms.ComboBox();
            this.comboRecordingApi = new System.Windows.Forms.ComboBox();
            this.comboDevice = new System.Windows.Forms.ComboBox();
            this.labelApi = new System.Windows.Forms.Label();
            this.labelDevice = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBoxRecordingApi.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonStartRecording
            // 
            this.buttonStartRecording.Location = new System.Drawing.Point(1056, 307);
            this.buttonStartRecording.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.buttonStartRecording.Name = "buttonStartRecording";
            this.buttonStartRecording.Size = new System.Drawing.Size(298, 72);
            this.buttonStartRecording.TabIndex = 0;
            this.buttonStartRecording.Text = "Start Recording";
            this.buttonStartRecording.UseVisualStyleBackColor = true;
            this.buttonStartRecording.Click += new System.EventHandler(this.OnButtonStartRecordingClick);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(34, 51);
            this.label1.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1863, 138);
            this.label1.TabIndex = 1;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // buttonStopRecording
            // 
            this.buttonStopRecording.Location = new System.Drawing.Point(1401, 307);
            this.buttonStopRecording.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.buttonStopRecording.Name = "buttonStopRecording";
            this.buttonStopRecording.Size = new System.Drawing.Size(298, 72);
            this.buttonStopRecording.TabIndex = 0;
            this.buttonStopRecording.Text = "Stop Recording";
            this.buttonStopRecording.UseVisualStyleBackColor = true;
            this.buttonStopRecording.Click += new System.EventHandler(this.OnButtonStopRecordingClick);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(1056, 494);
            this.progressBar1.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.progressBar1.Maximum = 30;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(729, 72);
            this.progressBar1.TabIndex = 4;
            // 
            // listBoxRecordings
            // 
            this.listBoxRecordings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxRecordings.FormattingEnabled = true;
            this.listBoxRecordings.ItemHeight = 41;
            this.listBoxRecordings.Location = new System.Drawing.Point(42, 774);
            this.listBoxRecordings.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.listBoxRecordings.Name = "listBoxRecordings";
            this.listBoxRecordings.Size = new System.Drawing.Size(1617, 332);
            this.listBoxRecordings.TabIndex = 8;
            // 
            // buttonPlay
            // 
            this.buttonPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPlay.Location = new System.Drawing.Point(1685, 858);
            this.buttonPlay.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(212, 72);
            this.buttonPlay.TabIndex = 9;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.OnButtonPlayClick);
            // 
            // buttonDelete
            // 
            this.buttonDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDelete.Location = new System.Drawing.Point(1685, 948);
            this.buttonDelete.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(212, 72);
            this.buttonDelete.TabIndex = 10;
            this.buttonDelete.Text = "Delete";
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += new System.EventHandler(this.OnButtonDeleteClick);
            // 
            // buttonOpenFolder
            // 
            this.buttonOpenFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOpenFolder.Location = new System.Drawing.Point(1685, 1040);
            this.buttonOpenFolder.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.buttonOpenFolder.Name = "buttonOpenFolder";
            this.buttonOpenFolder.Size = new System.Drawing.Size(212, 72);
            this.buttonOpenFolder.TabIndex = 10;
            this.buttonOpenFolder.Text = "Open Folder";
            this.buttonOpenFolder.UseVisualStyleBackColor = true;
            this.buttonOpenFolder.Click += new System.EventHandler(this.OnOpenFolderClick);
            //
            // groupBoxRecordingApi
            //
            this.groupBoxRecordingApi.Controls.Add(this.labelApi);
            this.groupBoxRecordingApi.Controls.Add(this.comboRecordingApi);
            this.groupBoxRecordingApi.Controls.Add(this.labelDevice);
            this.groupBoxRecordingApi.Controls.Add(this.comboDevice);
            this.groupBoxRecordingApi.Controls.Add(this.checkBoxEventCallback);
            this.groupBoxRecordingApi.Controls.Add(this.label3);
            this.groupBoxRecordingApi.Controls.Add(this.comboBoxSampleRate);
            this.groupBoxRecordingApi.Controls.Add(this.comboBoxChannels);
            this.groupBoxRecordingApi.Location = new System.Drawing.Point(42, 208);
            this.groupBoxRecordingApi.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.groupBoxRecordingApi.Name = "groupBoxRecordingApi";
            this.groupBoxRecordingApi.Padding = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.groupBoxRecordingApi.Size = new System.Drawing.Size(966, 546);
            this.groupBoxRecordingApi.TabIndex = 11;
            this.groupBoxRecordingApi.TabStop = false;
            this.groupBoxRecordingApi.Text = "Recording API";
            //
            // labelApi
            //
            this.labelApi.AutoSize = true;
            this.labelApi.Location = new System.Drawing.Point(34, 49);
            this.labelApi.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.labelApi.Name = "labelApi";
            this.labelApi.Size = new System.Drawing.Size(60, 41);
            this.labelApi.TabIndex = 0;
            this.labelApi.Text = "API";
            //
            // comboRecordingApi
            //
            this.comboRecordingApi.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboRecordingApi.FormattingEnabled = true;
            this.comboRecordingApi.Location = new System.Drawing.Point(186, 41);
            this.comboRecordingApi.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.comboRecordingApi.Name = "comboRecordingApi";
            this.comboRecordingApi.Size = new System.Drawing.Size(764, 49);
            this.comboRecordingApi.TabIndex = 1;
            //
            // labelDevice
            //
            this.labelDevice.AutoSize = true;
            this.labelDevice.Location = new System.Drawing.Point(34, 121);
            this.labelDevice.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.labelDevice.Name = "labelDevice";
            this.labelDevice.Size = new System.Drawing.Size(98, 41);
            this.labelDevice.TabIndex = 2;
            this.labelDevice.Text = "Device";
            //
            // comboDevice
            //
            this.comboDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboDevice.FormattingEnabled = true;
            this.comboDevice.Location = new System.Drawing.Point(186, 113);
            this.comboDevice.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.comboDevice.Name = "comboDevice";
            this.comboDevice.Size = new System.Drawing.Size(764, 49);
            this.comboDevice.TabIndex = 3;
            //
            // checkBoxEventCallback
            //
            this.checkBoxEventCallback.AutoSize = true;
            this.checkBoxEventCallback.Checked = true;
            this.checkBoxEventCallback.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxEventCallback.Location = new System.Drawing.Point(34, 205);
            this.checkBoxEventCallback.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.checkBoxEventCallback.Name = "checkBoxEventCallback";
            this.checkBoxEventCallback.Size = new System.Drawing.Size(259, 45);
            this.checkBoxEventCallback.TabIndex = 4;
            this.checkBoxEventCallback.Text = "Event Callbacks";
            this.checkBoxEventCallback.UseVisualStyleBackColor = true;
            //
            // comboBoxSampleRate
            //
            this.comboBoxSampleRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSampleRate.FormattingEnabled = true;
            this.comboBoxSampleRate.Location = new System.Drawing.Point(34, 455);
            this.comboBoxSampleRate.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.comboBoxSampleRate.Name = "comboBoxSampleRate";
            this.comboBoxSampleRate.Size = new System.Drawing.Size(336, 49);
            this.comboBoxSampleRate.TabIndex = 6;
            //
            // comboBoxChannels
            //
            this.comboBoxChannels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxChannels.FormattingEnabled = true;
            this.comboBoxChannels.Location = new System.Drawing.Point(400, 455);
            this.comboBoxChannels.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.comboBoxChannels.Name = "comboBoxChannels";
            this.comboBoxChannels.Size = new System.Drawing.Size(336, 49);
            this.comboBoxChannels.TabIndex = 7;
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(34, 392);
            this.label3.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(225, 41);
            this.label3.TabIndex = 5;
            this.label3.Text = "Capture Format";
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1048, 433);
            this.label2.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(283, 41);
            this.label2.TabIndex = 5;
            this.label2.Text = "Recording Progress:";
            // 
            // RecordingPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxRecordingApi);
            this.Controls.Add(this.buttonOpenFolder);
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.buttonPlay);
            this.Controls.Add(this.listBoxRecordings);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonStopRecording);
            this.Controls.Add(this.buttonStartRecording);
            this.Margin = new System.Windows.Forms.Padding(8, 10, 8, 10);
            this.Name = "RecordingPanel";
            this.Size = new System.Drawing.Size(1931, 1171);
            this.groupBoxRecordingApi.ResumeLayout(false);
            this.groupBoxRecordingApi.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStartRecording;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonStopRecording;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.ListBox listBoxRecordings;
        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.Button buttonDelete;
        private System.Windows.Forms.Button buttonOpenFolder;
        private System.Windows.Forms.GroupBox groupBoxRecordingApi;
        private System.Windows.Forms.ComboBox comboRecordingApi;
        private System.Windows.Forms.ComboBox comboDevice;
        private System.Windows.Forms.Label labelApi;
        private System.Windows.Forms.Label labelDevice;
        private System.Windows.Forms.CheckBox checkBoxEventCallback;
        private System.Windows.Forms.ComboBox comboBoxChannels;
        private System.Windows.Forms.ComboBox comboBoxSampleRate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}