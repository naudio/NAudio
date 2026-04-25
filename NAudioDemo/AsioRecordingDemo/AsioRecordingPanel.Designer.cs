namespace NAudioDemo.AsioRecordingDemo
{
    partial class AsioRecordingPanel
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.comboBoxAsioDevice = new System.Windows.Forms.ComboBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.labelChannels = new System.Windows.Forms.Label();
            this.checkedListBoxChannels = new System.Windows.Forms.CheckedListBox();
            this.buttonSelectAll = new System.Windows.Forms.Button();
            this.buttonSelectNone = new System.Windows.Forms.Button();
            this.listBoxRecordings = new System.Windows.Forms.ListBox();
            this.buttonPlay = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.buttonControlPanel = new System.Windows.Forms.Button();
            this.labelHelp = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(4, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(561, 33);
            this.label1.TabIndex = 0;
            this.label1.Text = "ASIO Recording via AsioDevice — pick the input channels you want to capture, then press Start.";
            //
            // buttonStart
            //
            this.buttonStart.Location = new System.Drawing.Point(409, 40);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 2;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.OnButtonStartClick);
            //
            // buttonStop
            //
            this.buttonStop.Location = new System.Drawing.Point(490, 40);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 2;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.OnButtonStopClick);
            //
            // comboBoxAsioDevice
            //
            this.comboBoxAsioDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxAsioDevice.FormattingEnabled = true;
            this.comboBoxAsioDevice.Location = new System.Drawing.Point(118, 42);
            this.comboBoxAsioDevice.Name = "comboBoxAsioDevice";
            this.comboBoxAsioDevice.Size = new System.Drawing.Size(285, 21);
            this.comboBoxAsioDevice.TabIndex = 3;
            this.comboBoxAsioDevice.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            //
            // timer1
            //
            this.timer1.Interval = 250;
            this.timer1.Tick += new System.EventHandler(this.OnTimerTick);
            //
            // labelChannels
            //
            this.labelChannels.AutoSize = true;
            this.labelChannels.Location = new System.Drawing.Point(17, 77);
            this.labelChannels.Name = "labelChannels";
            this.labelChannels.Size = new System.Drawing.Size(95, 13);
            this.labelChannels.TabIndex = 5;
            this.labelChannels.Text = "Input channels:";
            //
            // checkedListBoxChannels
            //
            this.checkedListBoxChannels.CheckOnClick = true;
            this.checkedListBoxChannels.FormattingEnabled = true;
            this.checkedListBoxChannels.Location = new System.Drawing.Point(17, 96);
            this.checkedListBoxChannels.Name = "checkedListBoxChannels";
            this.checkedListBoxChannels.Size = new System.Drawing.Size(260, 124);
            this.checkedListBoxChannels.TabIndex = 6;
            //
            // buttonSelectAll
            //
            this.buttonSelectAll.Location = new System.Drawing.Point(287, 96);
            this.buttonSelectAll.Name = "buttonSelectAll";
            this.buttonSelectAll.Size = new System.Drawing.Size(90, 23);
            this.buttonSelectAll.TabIndex = 7;
            this.buttonSelectAll.Text = "Select All";
            this.buttonSelectAll.UseVisualStyleBackColor = true;
            this.buttonSelectAll.Click += new System.EventHandler(this.OnSelectAllClick);
            //
            // buttonSelectNone
            //
            this.buttonSelectNone.Location = new System.Drawing.Point(287, 125);
            this.buttonSelectNone.Name = "buttonSelectNone";
            this.buttonSelectNone.Size = new System.Drawing.Size(90, 23);
            this.buttonSelectNone.TabIndex = 7;
            this.buttonSelectNone.Text = "Clear";
            this.buttonSelectNone.UseVisualStyleBackColor = true;
            this.buttonSelectNone.Click += new System.EventHandler(this.OnSelectNoneClick);
            //
            // labelHelp
            //
            this.labelHelp.Location = new System.Drawing.Point(387, 96);
            this.labelHelp.Name = "labelHelp";
            this.labelHelp.Size = new System.Drawing.Size(178, 60);
            this.labelHelp.TabIndex = 8;
            this.labelHelp.Text = "Non-contiguous selection is supported — e.g. tick channels 0 and 3 to record those physical jacks only.";
            //
            // listBoxRecordings
            //
            this.listBoxRecordings.FormattingEnabled = true;
            this.listBoxRecordings.Location = new System.Drawing.Point(17, 235);
            this.listBoxRecordings.Name = "listBoxRecordings";
            this.listBoxRecordings.Size = new System.Drawing.Size(424, 95);
            this.listBoxRecordings.TabIndex = 9;
            //
            // buttonPlay
            //
            this.buttonPlay.Location = new System.Drawing.Point(448, 235);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(75, 23);
            this.buttonPlay.TabIndex = 10;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.OnButtonPlayClick);
            //
            // buttonDelete
            //
            this.buttonDelete.Location = new System.Drawing.Point(448, 264);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(75, 23);
            this.buttonDelete.TabIndex = 10;
            this.buttonDelete.Text = "Delete";
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += new System.EventHandler(this.OnButtonDeleteClick);
            //
            // buttonControlPanel
            //
            this.buttonControlPanel.Location = new System.Drawing.Point(17, 40);
            this.buttonControlPanel.Name = "buttonControlPanel";
            this.buttonControlPanel.Size = new System.Drawing.Size(95, 23);
            this.buttonControlPanel.TabIndex = 2;
            this.buttonControlPanel.Text = "Control Panel";
            this.buttonControlPanel.UseVisualStyleBackColor = true;
            this.buttonControlPanel.Click += new System.EventHandler(this.OnButtonControlPanelClick);
            //
            // AsioRecordingPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelHelp);
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.buttonPlay);
            this.Controls.Add(this.listBoxRecordings);
            this.Controls.Add(this.buttonSelectNone);
            this.Controls.Add(this.buttonSelectAll);
            this.Controls.Add(this.checkedListBoxChannels);
            this.Controls.Add(this.labelChannels);
            this.Controls.Add(this.comboBoxAsioDevice);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonControlPanel);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.label1);
            this.Name = "AsioRecordingPanel";
            this.Size = new System.Drawing.Size(568, 340);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.ComboBox comboBoxAsioDevice;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label labelChannels;
        private System.Windows.Forms.CheckedListBox checkedListBoxChannels;
        private System.Windows.Forms.Button buttonSelectAll;
        private System.Windows.Forms.Button buttonSelectNone;
        private System.Windows.Forms.Label labelHelp;
        private System.Windows.Forms.ListBox listBoxRecordings;
        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.Button buttonDelete;
        private System.Windows.Forms.Button buttonControlPanel;
    }
}
