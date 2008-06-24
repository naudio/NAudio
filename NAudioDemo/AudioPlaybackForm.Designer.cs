namespace NAudioDemo
{
    partial class AudioPlaybackForm
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
            this.components = new System.ComponentModel.Container();
            this.comboBoxLatency = new System.Windows.Forms.ComboBox();
            this.radioButtonDirectSound = new System.Windows.Forms.RadioButton();
            this.radioButtonWaveOut = new System.Windows.Forms.RadioButton();
            this.radioButtonAsio = new System.Windows.Forms.RadioButton();
            this.buttonControlPanel = new System.Windows.Forms.Button();
            this.groupBoxDriverModel = new System.Windows.Forms.GroupBox();
            this.comboBoxAsioDriver = new System.Windows.Forms.ComboBox();
            this.labelAsioSelectDriver = new System.Windows.Forms.Label();
            this.checkBoxWasapiEventCallback = new System.Windows.Forms.CheckBox();
            this.checkBoxWasapiExclusiveMode = new System.Windows.Forms.CheckBox();
            this.checkBoxWaveOutWindow = new System.Windows.Forms.CheckBox();
            this.radioButtonWasapi = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonOpenFile = new System.Windows.Forms.ToolStripButton();
            this.buttonPlay = new System.Windows.Forms.ToolStripButton();
            this.buttonPause = new System.Windows.Forms.ToolStripButton();
            this.buttonStop = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.labelCurrentTime = new System.Windows.Forms.ToolStripLabel();
            this.toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
            this.labelTotalTime = new System.Windows.Forms.ToolStripLabel();
            this.trackBarPosition = new System.Windows.Forms.TrackBar();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.volumeSlider1 = new NAudio.Gui.VolumeSlider();
            this.groupBoxDriverModel.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarPosition)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBoxLatency
            // 
            this.comboBoxLatency.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLatency.FormattingEnabled = true;
            this.comboBoxLatency.Location = new System.Drawing.Point(362, 27);
            this.comboBoxLatency.Name = "comboBoxLatency";
            this.comboBoxLatency.Size = new System.Drawing.Size(75, 21);
            this.comboBoxLatency.TabIndex = 10;
            // 
            // radioButtonDirectSound
            // 
            this.radioButtonDirectSound.AutoSize = true;
            this.radioButtonDirectSound.Location = new System.Drawing.Point(6, 62);
            this.radioButtonDirectSound.Name = "radioButtonDirectSound";
            this.radioButtonDirectSound.Size = new System.Drawing.Size(84, 17);
            this.radioButtonDirectSound.TabIndex = 9;
            this.radioButtonDirectSound.TabStop = true;
            this.radioButtonDirectSound.Text = "DirectSound";
            this.radioButtonDirectSound.UseVisualStyleBackColor = true;
            // 
            // radioButtonWaveOut
            // 
            this.radioButtonWaveOut.AutoSize = true;
            this.radioButtonWaveOut.Checked = true;
            this.radioButtonWaveOut.Location = new System.Drawing.Point(6, 19);
            this.radioButtonWaveOut.Name = "radioButtonWaveOut";
            this.radioButtonWaveOut.Size = new System.Drawing.Size(71, 17);
            this.radioButtonWaveOut.TabIndex = 7;
            this.radioButtonWaveOut.TabStop = true;
            this.radioButtonWaveOut.Text = "WaveOut";
            this.radioButtonWaveOut.UseVisualStyleBackColor = true;
            // 
            // radioButtonAsio
            // 
            this.radioButtonAsio.AutoSize = true;
            this.radioButtonAsio.Location = new System.Drawing.Point(6, 154);
            this.radioButtonAsio.Name = "radioButtonAsio";
            this.radioButtonAsio.Size = new System.Drawing.Size(50, 17);
            this.radioButtonAsio.TabIndex = 9;
            this.radioButtonAsio.TabStop = true;
            this.radioButtonAsio.Text = "ASIO";
            this.radioButtonAsio.UseVisualStyleBackColor = true;
            // 
            // buttonControlPanel
            // 
            this.buttonControlPanel.Location = new System.Drawing.Point(25, 202);
            this.buttonControlPanel.Name = "buttonControlPanel";
            this.buttonControlPanel.Size = new System.Drawing.Size(135, 23);
            this.buttonControlPanel.TabIndex = 12;
            this.buttonControlPanel.Text = "ASIO Control Panel";
            this.buttonControlPanel.UseVisualStyleBackColor = true;
            this.buttonControlPanel.Click += new System.EventHandler(this.buttonControlPanel_Click);
            // 
            // groupBoxDriverModel
            // 
            this.groupBoxDriverModel.Controls.Add(this.comboBoxAsioDriver);
            this.groupBoxDriverModel.Controls.Add(this.labelAsioSelectDriver);
            this.groupBoxDriverModel.Controls.Add(this.checkBoxWasapiEventCallback);
            this.groupBoxDriverModel.Controls.Add(this.checkBoxWasapiExclusiveMode);
            this.groupBoxDriverModel.Controls.Add(this.checkBoxWaveOutWindow);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonWaveOut);
            this.groupBoxDriverModel.Controls.Add(this.buttonControlPanel);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonDirectSound);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonWasapi);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonAsio);
            this.groupBoxDriverModel.Location = new System.Drawing.Point(12, 30);
            this.groupBoxDriverModel.Name = "groupBoxDriverModel";
            this.groupBoxDriverModel.Size = new System.Drawing.Size(235, 239);
            this.groupBoxDriverModel.TabIndex = 13;
            this.groupBoxDriverModel.TabStop = false;
            this.groupBoxDriverModel.Text = "Output Driver";
            // 
            // comboBoxAsioDriver
            // 
            this.comboBoxAsioDriver.FormattingEnabled = true;
            this.comboBoxAsioDriver.Location = new System.Drawing.Point(105, 175);
            this.comboBoxAsioDriver.Name = "comboBoxAsioDriver";
            this.comboBoxAsioDriver.Size = new System.Drawing.Size(124, 21);
            this.comboBoxAsioDriver.TabIndex = 16;
            // 
            // labelAsioSelectDriver
            // 
            this.labelAsioSelectDriver.AutoSize = true;
            this.labelAsioSelectDriver.Location = new System.Drawing.Point(25, 178);
            this.labelAsioSelectDriver.Name = "labelAsioSelectDriver";
            this.labelAsioSelectDriver.Size = new System.Drawing.Size(74, 13);
            this.labelAsioSelectDriver.TabIndex = 15;
            this.labelAsioSelectDriver.Text = "Select Driver :";
            // 
            // checkBoxWasapiEventCallback
            // 
            this.checkBoxWasapiEventCallback.AutoSize = true;
            this.checkBoxWasapiEventCallback.Location = new System.Drawing.Point(25, 131);
            this.checkBoxWasapiEventCallback.Name = "checkBoxWasapiEventCallback";
            this.checkBoxWasapiEventCallback.Size = new System.Drawing.Size(98, 17);
            this.checkBoxWasapiEventCallback.TabIndex = 14;
            this.checkBoxWasapiEventCallback.Text = "Event Callback";
            this.checkBoxWasapiEventCallback.UseVisualStyleBackColor = true;
            // 
            // checkBoxWasapiExclusiveMode
            // 
            this.checkBoxWasapiExclusiveMode.AutoSize = true;
            this.checkBoxWasapiExclusiveMode.Location = new System.Drawing.Point(25, 108);
            this.checkBoxWasapiExclusiveMode.Name = "checkBoxWasapiExclusiveMode";
            this.checkBoxWasapiExclusiveMode.Size = new System.Drawing.Size(101, 17);
            this.checkBoxWasapiExclusiveMode.TabIndex = 14;
            this.checkBoxWasapiExclusiveMode.Text = "Exclusive Mode";
            this.checkBoxWasapiExclusiveMode.UseVisualStyleBackColor = true;
            // 
            // checkBoxWaveOutWindow
            // 
            this.checkBoxWaveOutWindow.AutoSize = true;
            this.checkBoxWaveOutWindow.Location = new System.Drawing.Point(25, 39);
            this.checkBoxWaveOutWindow.Name = "checkBoxWaveOutWindow";
            this.checkBoxWaveOutWindow.Size = new System.Drawing.Size(126, 17);
            this.checkBoxWaveOutWindow.TabIndex = 13;
            this.checkBoxWaveOutWindow.Text = "Callback via Window";
            this.checkBoxWaveOutWindow.UseVisualStyleBackColor = true;
            // 
            // radioButtonWasapi
            // 
            this.radioButtonWasapi.AutoSize = true;
            this.radioButtonWasapi.Location = new System.Drawing.Point(6, 85);
            this.radioButtonWasapi.Name = "radioButtonWasapi";
            this.radioButtonWasapi.Size = new System.Drawing.Size(67, 17);
            this.radioButtonWasapi.TabIndex = 9;
            this.radioButtonWasapi.TabStop = true;
            this.radioButtonWasapi.Text = "WASAPI";
            this.radioButtonWasapi.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(443, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(20, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "ms";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonOpenFile,
            this.buttonPlay,
            this.buttonPause,
            this.buttonStop,
            this.toolStripLabel1,
            this.labelCurrentTime,
            this.toolStripLabel3,
            this.labelTotalTime});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(513, 25);
            this.toolStrip1.TabIndex = 15;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonOpenFile
            // 
            this.toolStripButtonOpenFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonOpenFile.Image = global::NAudioDemo.Images.Open;
            this.toolStripButtonOpenFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonOpenFile.Name = "toolStripButtonOpenFile";
            this.toolStripButtonOpenFile.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonOpenFile.Text = "Open File";
            this.toolStripButtonOpenFile.Click += new System.EventHandler(this.toolStripButtonOpenFile_Click);
            // 
            // buttonPlay
            // 
            this.buttonPlay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.buttonPlay.Image = global::NAudioDemo.Images.Play;
            this.buttonPlay.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(23, 22);
            this.buttonPlay.Text = "Play";
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // buttonPause
            // 
            this.buttonPause.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.buttonPause.Image = global::NAudioDemo.Images.Pause;
            this.buttonPause.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.buttonPause.Name = "buttonPause";
            this.buttonPause.Size = new System.Drawing.Size(23, 22);
            this.buttonPause.Text = "Pause";
            this.buttonPause.Click += new System.EventHandler(this.buttonPause_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.buttonStop.Image = global::NAudioDemo.Images.Stop;
            this.buttonStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(23, 22);
            this.buttonStop.Text = "Stop";
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(80, 22);
            this.toolStripLabel1.Text = "Current Time:";
            // 
            // labelCurrentTime
            // 
            this.labelCurrentTime.Name = "labelCurrentTime";
            this.labelCurrentTime.Size = new System.Drawing.Size(34, 22);
            this.labelCurrentTime.Text = "00:00";
            // 
            // toolStripLabel3
            // 
            this.toolStripLabel3.Name = "toolStripLabel3";
            this.toolStripLabel3.Size = new System.Drawing.Size(67, 22);
            this.toolStripLabel3.Text = "Total Time:";
            // 
            // labelTotalTime
            // 
            this.labelTotalTime.Name = "labelTotalTime";
            this.labelTotalTime.Size = new System.Drawing.Size(34, 22);
            this.labelTotalTime.Text = "00:00";
            // 
            // trackBarPosition
            // 
            this.trackBarPosition.Location = new System.Drawing.Point(18, 275);
            this.trackBarPosition.Name = "trackBarPosition";
            this.trackBarPosition.Size = new System.Drawing.Size(489, 45);
            this.trackBarPosition.TabIndex = 16;
            this.trackBarPosition.Scroll += new System.EventHandler(this.trackBarPosition_Scroll);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(253, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 13);
            this.label2.TabIndex = 17;
            this.label2.Text = "Requested Latency:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(253, 57);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 13);
            this.label3.TabIndex = 17;
            this.label3.Text = "Volume:";
            // 
            // volumeSlider1
            // 
            this.volumeSlider1.Location = new System.Drawing.Point(362, 54);
            this.volumeSlider1.Name = "volumeSlider1";
            this.volumeSlider1.Size = new System.Drawing.Size(96, 16);
            this.volumeSlider1.TabIndex = 11;
            this.volumeSlider1.Volume = 1F;
            this.volumeSlider1.Load += new System.EventHandler(this.volumeSlider1_Load);
            this.volumeSlider1.VolumeChanged += new System.EventHandler(this.volumeSlider1_VolumeChanged);
            // 
            // AudioPlaybackForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(513, 328);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.trackBarPosition);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.groupBoxDriverModel);
            this.Controls.Add(this.comboBoxLatency);
            this.Controls.Add(this.volumeSlider1);
            this.Name = "AudioPlaybackForm";
            this.ShowInTaskbar = false;
            this.Text = "AudioPlaybackForm";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.groupBoxDriverModel.ResumeLayout(false);
            this.groupBoxDriverModel.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarPosition)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxLatency;
        private System.Windows.Forms.RadioButton radioButtonDirectSound;
        private System.Windows.Forms.RadioButton radioButtonWaveOut;
        private NAudio.Gui.VolumeSlider volumeSlider1;
        private System.Windows.Forms.RadioButton radioButtonAsio;
        private System.Windows.Forms.Button buttonControlPanel;
        private System.Windows.Forms.GroupBox groupBoxDriverModel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radioButtonWasapi;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton buttonPlay;
        private System.Windows.Forms.ToolStripButton buttonPause;
        private System.Windows.Forms.ToolStripButton buttonStop;
        private System.Windows.Forms.TrackBar trackBarPosition;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolStripButton toolStripButtonOpenFile;
        private System.Windows.Forms.CheckBox checkBoxWaveOutWindow;
        private System.Windows.Forms.CheckBox checkBoxWasapiEventCallback;
        private System.Windows.Forms.CheckBox checkBoxWasapiExclusiveMode;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripLabel labelCurrentTime;
        private System.Windows.Forms.ToolStripLabel toolStripLabel3;
        private System.Windows.Forms.ToolStripLabel labelTotalTime;
        private System.Windows.Forms.ComboBox comboBoxAsioDriver;
        private System.Windows.Forms.Label labelAsioSelectDriver;
        private System.Windows.Forms.Label label3;
    }
}