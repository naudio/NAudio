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
            this.radioButtonWaveOutWindow = new System.Windows.Forms.RadioButton();
            this.radioButtonWaveOut = new System.Windows.Forms.RadioButton();
            this.radioButtonAsio = new System.Windows.Forms.RadioButton();
            this.buttonControlPanel = new System.Windows.Forms.Button();
            this.groupBoxDriverModel = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.radioButtonDirectSoundNative = new System.Windows.Forms.RadioButton();
            this.radioButtonWasapiExclusive = new System.Windows.Forms.RadioButton();
            this.radioButtonWasapi = new System.Windows.Forms.RadioButton();
            this.buttonLoad = new System.Windows.Forms.Button();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.buttonPlay = new System.Windows.Forms.ToolStripButton();
            this.buttonPause = new System.Windows.Forms.ToolStripButton();
            this.buttonStop = new System.Windows.Forms.ToolStripButton();
            this.volumeSlider1 = new NAudio.Gui.VolumeSlider();
            this.trackBarPosition = new System.Windows.Forms.TrackBar();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBoxDriverModel.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarPosition)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBoxLatency
            // 
            this.comboBoxLatency.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLatency.FormattingEnabled = true;
            this.comboBoxLatency.Location = new System.Drawing.Point(105, 18);
            this.comboBoxLatency.Name = "comboBoxLatency";
            this.comboBoxLatency.Size = new System.Drawing.Size(75, 21);
            this.comboBoxLatency.TabIndex = 10;
            // 
            // radioButtonDirectSound
            // 
            this.radioButtonDirectSound.AutoSize = true;
            this.radioButtonDirectSound.Location = new System.Drawing.Point(6, 67);
            this.radioButtonDirectSound.Name = "radioButtonDirectSound";
            this.radioButtonDirectSound.Size = new System.Drawing.Size(84, 17);
            this.radioButtonDirectSound.TabIndex = 9;
            this.radioButtonDirectSound.TabStop = true;
            this.radioButtonDirectSound.Text = "DirectSound";
            this.radioButtonDirectSound.UseVisualStyleBackColor = true;
            // 
            // radioButtonWaveOutWindow
            // 
            this.radioButtonWaveOutWindow.AutoSize = true;
            this.radioButtonWaveOutWindow.Location = new System.Drawing.Point(6, 43);
            this.radioButtonWaveOutWindow.Name = "radioButtonWaveOutWindow";
            this.radioButtonWaveOutWindow.Size = new System.Drawing.Size(113, 17);
            this.radioButtonWaveOutWindow.TabIndex = 8;
            this.radioButtonWaveOutWindow.TabStop = true;
            this.radioButtonWaveOutWindow.Text = "WaveOut Window";
            this.radioButtonWaveOutWindow.UseVisualStyleBackColor = true;
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
            this.radioButtonAsio.Location = new System.Drawing.Point(6, 158);
            this.radioButtonAsio.Name = "radioButtonAsio";
            this.radioButtonAsio.Size = new System.Drawing.Size(50, 17);
            this.radioButtonAsio.TabIndex = 9;
            this.radioButtonAsio.TabStop = true;
            this.radioButtonAsio.Text = "ASIO";
            this.radioButtonAsio.UseVisualStyleBackColor = true;
            // 
            // buttonControlPanel
            // 
            this.buttonControlPanel.Location = new System.Drawing.Point(71, 158);
            this.buttonControlPanel.Name = "buttonControlPanel";
            this.buttonControlPanel.Size = new System.Drawing.Size(135, 23);
            this.buttonControlPanel.TabIndex = 12;
            this.buttonControlPanel.Text = "ASIO Control Panel";
            this.buttonControlPanel.UseVisualStyleBackColor = true;
            this.buttonControlPanel.Click += new System.EventHandler(this.buttonControlPanel_Click);
            // 
            // groupBoxDriverModel
            // 
            this.groupBoxDriverModel.Controls.Add(this.label1);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonWaveOut);
            this.groupBoxDriverModel.Controls.Add(this.buttonControlPanel);
            this.groupBoxDriverModel.Controls.Add(this.comboBoxLatency);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonWaveOutWindow);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonDirectSoundNative);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonDirectSound);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonWasapiExclusive);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonWasapi);
            this.groupBoxDriverModel.Controls.Add(this.radioButtonAsio);
            this.groupBoxDriverModel.Location = new System.Drawing.Point(12, 30);
            this.groupBoxDriverModel.Name = "groupBoxDriverModel";
            this.groupBoxDriverModel.Size = new System.Drawing.Size(211, 187);
            this.groupBoxDriverModel.TabIndex = 13;
            this.groupBoxDriverModel.TabStop = false;
            this.groupBoxDriverModel.Text = "Output Driver";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(186, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(20, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "ms";
            // 
            // radioButtonDirectSoundNative
            // 
            this.radioButtonDirectSoundNative.AutoSize = true;
            this.radioButtonDirectSoundNative.Location = new System.Drawing.Point(6, 90);
            this.radioButtonDirectSoundNative.Name = "radioButtonDirectSoundNative";
            this.radioButtonDirectSoundNative.Size = new System.Drawing.Size(118, 17);
            this.radioButtonDirectSoundNative.TabIndex = 9;
            this.radioButtonDirectSoundNative.TabStop = true;
            this.radioButtonDirectSoundNative.Text = "DirectSound Native";
            this.radioButtonDirectSoundNative.UseVisualStyleBackColor = true;
            // 
            // radioButtonWasapiExclusive
            // 
            this.radioButtonWasapiExclusive.AutoSize = true;
            this.radioButtonWasapiExclusive.Location = new System.Drawing.Point(6, 135);
            this.radioButtonWasapiExclusive.Name = "radioButtonWasapiExclusive";
            this.radioButtonWasapiExclusive.Size = new System.Drawing.Size(145, 17);
            this.radioButtonWasapiExclusive.TabIndex = 9;
            this.radioButtonWasapiExclusive.TabStop = true;
            this.radioButtonWasapiExclusive.Text = "WASAPI Exclusive Mode";
            this.radioButtonWasapiExclusive.UseVisualStyleBackColor = true;
            // 
            // radioButtonWasapi
            // 
            this.radioButtonWasapi.AutoSize = true;
            this.radioButtonWasapi.Location = new System.Drawing.Point(6, 113);
            this.radioButtonWasapi.Name = "radioButtonWasapi";
            this.radioButtonWasapi.Size = new System.Drawing.Size(134, 17);
            this.radioButtonWasapi.TabIndex = 9;
            this.radioButtonWasapi.TabStop = true;
            this.radioButtonWasapi.Text = "WASAPI Shared Mode";
            this.radioButtonWasapi.UseVisualStyleBackColor = true;
            // 
            // buttonLoad
            // 
            this.buttonLoad.Location = new System.Drawing.Point(249, 30);
            this.buttonLoad.Name = "buttonLoad";
            this.buttonLoad.Size = new System.Drawing.Size(75, 23);
            this.buttonLoad.TabIndex = 14;
            this.buttonLoad.Text = "Load";
            this.buttonLoad.UseVisualStyleBackColor = true;
            this.buttonLoad.Click += new System.EventHandler(this.buttonLoad_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.buttonPlay,
            this.buttonPause,
            this.buttonStop});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(513, 25);
            this.toolStrip1.TabIndex = 15;
            this.toolStrip1.Text = "toolStrip1";
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
            // volumeSlider1
            // 
            this.volumeSlider1.Location = new System.Drawing.Point(229, 79);
            this.volumeSlider1.Name = "volumeSlider1";
            this.volumeSlider1.Size = new System.Drawing.Size(96, 16);
            this.volumeSlider1.TabIndex = 11;
            this.volumeSlider1.Volume = 1F;
            this.volumeSlider1.VolumeChanged += new System.EventHandler(this.volumeSlider1_VolumeChanged);
            // 
            // trackBarPosition
            // 
            this.trackBarPosition.Location = new System.Drawing.Point(12, 223);
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
            // AudioPlaybackForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(513, 266);
            this.Controls.Add(this.trackBarPosition);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.buttonLoad);
            this.Controls.Add(this.groupBoxDriverModel);
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
        private System.Windows.Forms.RadioButton radioButtonWaveOutWindow;
        private System.Windows.Forms.RadioButton radioButtonWaveOut;
        private NAudio.Gui.VolumeSlider volumeSlider1;
        private System.Windows.Forms.RadioButton radioButtonAsio;
        private System.Windows.Forms.Button buttonControlPanel;
        private System.Windows.Forms.GroupBox groupBoxDriverModel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radioButtonWasapi;
        private System.Windows.Forms.Button buttonLoad;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton buttonPlay;
        private System.Windows.Forms.ToolStripButton buttonPause;
        private System.Windows.Forms.ToolStripButton buttonStop;
        private System.Windows.Forms.RadioButton radioButtonDirectSoundNative;
        private System.Windows.Forms.RadioButton radioButtonWasapiExclusive;
        private System.Windows.Forms.TrackBar trackBarPosition;
        private System.Windows.Forms.Timer timer1;
    }
}