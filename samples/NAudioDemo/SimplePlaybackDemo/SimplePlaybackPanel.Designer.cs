namespace NAudioDemo.SimplePlaybackDemo
{
    partial class SimplePlaybackPanel
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
            this.buttonPlay = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.comboBoxOutputDriver = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonOpen = new System.Windows.Forms.Button();
            this.labelNowTime = new System.Windows.Forms.Label();
            this.labelTotalTime = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.volumeSlider1 = new NAudio.Gui.VolumeSlider();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(652, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "This is an experimental WORK IN PROGRESS. Will demo use of new AudioFileReader cl" +
    "ass and used to debug PlaybackStopped event";
            // 
            // buttonPlay
            // 
            this.buttonPlay.Location = new System.Drawing.Point(91, 31);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(75, 23);
            this.buttonPlay.TabIndex = 1;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(173, 30);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 2;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.OnButtonStopClick);
            // 
            // comboBoxOutputDriver
            // 
            this.comboBoxOutputDriver.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOutputDriver.FormattingEnabled = true;
            this.comboBoxOutputDriver.Location = new System.Drawing.Point(128, 80);
            this.comboBoxOutputDriver.Name = "comboBoxOutputDriver";
            this.comboBoxOutputDriver.Size = new System.Drawing.Size(227, 21);
            this.comboBoxOutputDriver.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "For Debug Purposes";
            // 
            // buttonOpen
            // 
            this.buttonOpen.Location = new System.Drawing.Point(10, 31);
            this.buttonOpen.Name = "buttonOpen";
            this.buttonOpen.Size = new System.Drawing.Size(75, 23);
            this.buttonOpen.TabIndex = 1;
            this.buttonOpen.Text = "Open";
            this.buttonOpen.UseVisualStyleBackColor = true;
            this.buttonOpen.Click += new System.EventHandler(this.OnButtonOpenClick);
            // 
            // labelNowTime
            // 
            this.labelNowTime.AutoSize = true;
            this.labelNowTime.Location = new System.Drawing.Point(436, 40);
            this.labelNowTime.Name = "labelNowTime";
            this.labelNowTime.Size = new System.Drawing.Size(34, 13);
            this.labelNowTime.TabIndex = 6;
            this.labelNowTime.Text = "00:00";
            // 
            // labelTotalTime
            // 
            this.labelTotalTime.AutoSize = true;
            this.labelTotalTime.Location = new System.Drawing.Point(476, 40);
            this.labelTotalTime.Name = "labelTotalTime";
            this.labelTotalTime.Size = new System.Drawing.Size(34, 13);
            this.labelTotalTime.TabIndex = 6;
            this.labelTotalTime.Text = "00:00";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(10, 123);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(101, 23);
            this.button1.TabIndex = 7;
            this.button1.Text = "MP3 Reposition";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.OnMp3RepositionTestClick);
            // 
            // volumeSlider1
            // 
            this.volumeSlider1.Location = new System.Drawing.Point(307, 37);
            this.volumeSlider1.Name = "volumeSlider1";
            this.volumeSlider1.Size = new System.Drawing.Size(96, 16);
            this.volumeSlider1.TabIndex = 5;
            this.volumeSlider1.VolumeChanged += new System.EventHandler(this.OnVolumeSliderChanged);
            // 
            // SimplePlaybackPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.button1);
            this.Controls.Add(this.labelTotalTime);
            this.Controls.Add(this.labelNowTime);
            this.Controls.Add(this.volumeSlider1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBoxOutputDriver);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonOpen);
            this.Controls.Add(this.buttonPlay);
            this.Controls.Add(this.label1);
            this.Name = "SimplePlaybackPanel";
            this.Size = new System.Drawing.Size(821, 268);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.ComboBox comboBoxOutputDriver;
        private System.Windows.Forms.Label label2;
        private NAudio.Gui.VolumeSlider volumeSlider1;
        private System.Windows.Forms.Button buttonOpen;
        private System.Windows.Forms.Label labelNowTime;
        private System.Windows.Forms.Label labelTotalTime;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button button1;
    }
}
