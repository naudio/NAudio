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
            this.comboBoxLatency = new System.Windows.Forms.ComboBox();
            this.radioButtonDirectSound = new System.Windows.Forms.RadioButton();
            this.radioButtonWaveOutWindow = new System.Windows.Forms.RadioButton();
            this.radioButtonWaveOut = new System.Windows.Forms.RadioButton();
            this.buttonPause = new System.Windows.Forms.Button();
            this.buttonPlay = new System.Windows.Forms.Button();
            this.radioButtonAsio = new System.Windows.Forms.RadioButton();
            this.volumeSlider1 = new NAudio.Gui.VolumeSlider();
            this.buttonControlPanel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonInputFolder = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
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
            // buttonPause
            // 
            this.buttonPause.Location = new System.Drawing.Point(93, 231);
            this.buttonPause.Name = "buttonPause";
            this.buttonPause.Size = new System.Drawing.Size(75, 23);
            this.buttonPause.TabIndex = 5;
            this.buttonPause.Text = "Pause";
            this.buttonPause.UseVisualStyleBackColor = true;
            this.buttonPause.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // buttonPlay
            // 
            this.buttonPlay.Location = new System.Drawing.Point(12, 231);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(75, 23);
            this.buttonPlay.TabIndex = 6;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // radioButtonAsio
            // 
            this.radioButtonAsio.AutoSize = true;
            this.radioButtonAsio.Location = new System.Drawing.Point(6, 90);
            this.radioButtonAsio.Name = "radioButtonAsio";
            this.radioButtonAsio.Size = new System.Drawing.Size(50, 17);
            this.radioButtonAsio.TabIndex = 9;
            this.radioButtonAsio.TabStop = true;
            this.radioButtonAsio.Text = "ASIO";
            this.radioButtonAsio.UseVisualStyleBackColor = true;
            // 
            // volumeSlider1
            // 
            this.volumeSlider1.Location = new System.Drawing.Point(405, 238);
            this.volumeSlider1.Name = "volumeSlider1";
            this.volumeSlider1.Size = new System.Drawing.Size(96, 16);
            this.volumeSlider1.TabIndex = 11;
            this.volumeSlider1.Volume = 1F;
            this.volumeSlider1.Load += new System.EventHandler(this.volumeSlider1_Load);
            this.volumeSlider1.VolumeChanged += new System.EventHandler(this.volumeSlider1_VolumeChanged);
            // 
            // buttonControlPanel
            // 
            this.buttonControlPanel.Location = new System.Drawing.Point(70, 87);
            this.buttonControlPanel.Name = "buttonControlPanel";
            this.buttonControlPanel.Size = new System.Drawing.Size(135, 23);
            this.buttonControlPanel.TabIndex = 12;
            this.buttonControlPanel.Text = "ASIO Control Panel";
            this.buttonControlPanel.UseVisualStyleBackColor = true;
            this.buttonControlPanel.Click += new System.EventHandler(this.buttonControlPanel_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.radioButtonWaveOut);
            this.groupBox1.Controls.Add(this.buttonControlPanel);
            this.groupBox1.Controls.Add(this.comboBoxLatency);
            this.groupBox1.Controls.Add(this.radioButtonWaveOutWindow);
            this.groupBox1.Controls.Add(this.radioButtonDirectSound);
            this.groupBox1.Controls.Add(this.radioButtonAsio);
            this.groupBox1.Location = new System.Drawing.Point(12, 11);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(211, 129);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Output Driver";
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
            // buttonInputFolder
            // 
            this.buttonInputFolder.Location = new System.Drawing.Point(249, 30);
            this.buttonInputFolder.Name = "buttonInputFolder";
            this.buttonInputFolder.Size = new System.Drawing.Size(75, 23);
            this.buttonInputFolder.TabIndex = 14;
            this.buttonInputFolder.Text = "Input Folder";
            this.buttonInputFolder.UseVisualStyleBackColor = true;
            this.buttonInputFolder.Click += new System.EventHandler(this.buttonInputFolder_Click);
            // 
            // AudioPlaybackForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(513, 266);
            this.Controls.Add(this.buttonInputFolder);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.volumeSlider1);
            this.Controls.Add(this.buttonPause);
            this.Controls.Add(this.buttonPlay);
            this.Name = "AudioPlaybackForm";
            this.Text = "AudioPlaybackForm";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxLatency;
        private System.Windows.Forms.RadioButton radioButtonDirectSound;
        private System.Windows.Forms.RadioButton radioButtonWaveOutWindow;
        private System.Windows.Forms.RadioButton radioButtonWaveOut;
        private System.Windows.Forms.Button buttonPause;
        private System.Windows.Forms.Button buttonPlay;
        private NAudio.Gui.VolumeSlider volumeSlider1;
        private System.Windows.Forms.RadioButton radioButtonAsio;
        private System.Windows.Forms.Button buttonControlPanel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonInputFolder;
    }
}