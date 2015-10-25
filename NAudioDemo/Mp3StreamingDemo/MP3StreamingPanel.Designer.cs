namespace NAudioDemo.Mp3StreamingDemo
{
    partial class Mp3StreamingPanel
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
            this.buttonPlay = new System.Windows.Forms.Button();
            this.textBoxStreamingUrl = new System.Windows.Forms.TextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.progressBarBuffer = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonPause = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.labelBuffered = new System.Windows.Forms.Label();
            this.labelVolume = new System.Windows.Forms.Label();
            this.volumeSlider1 = new NAudio.Gui.VolumeSlider();
            this.SuspendLayout();
            // 
            // buttonPlay
            // 
            this.buttonPlay.Location = new System.Drawing.Point(12, 98);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(75, 23);
            this.buttonPlay.TabIndex = 0;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // textBoxStreamingUrl
            // 
            this.textBoxStreamingUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxStreamingUrl.Location = new System.Drawing.Point(97, 12);
            this.textBoxStreamingUrl.Name = "textBoxStreamingUrl";
            this.textBoxStreamingUrl.Size = new System.Drawing.Size(228, 20);
            this.textBoxStreamingUrl.TabIndex = 1;
            this.textBoxStreamingUrl.Text = "http://radio.reaper.fm/stream/";
            // 
            // timer1
            // 
            this.timer1.Interval = 250;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Streaming URL:";
            // 
            // progressBarBuffer
            // 
            this.progressBarBuffer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarBuffer.Location = new System.Drawing.Point(97, 39);
            this.progressBarBuffer.Name = "progressBarBuffer";
            this.progressBarBuffer.Size = new System.Drawing.Size(195, 23);
            this.progressBarBuffer.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Buffered:";
            // 
            // buttonPause
            // 
            this.buttonPause.Location = new System.Drawing.Point(94, 98);
            this.buttonPause.Name = "buttonPause";
            this.buttonPause.Size = new System.Drawing.Size(75, 23);
            this.buttonPause.TabIndex = 5;
            this.buttonPause.Text = "Pause";
            this.buttonPause.UseVisualStyleBackColor = true;
            this.buttonPause.Click += new System.EventHandler(this.buttonPause_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(176, 98);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 6;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // labelBuffered
            // 
            this.labelBuffered.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelBuffered.AutoSize = true;
            this.labelBuffered.Location = new System.Drawing.Point(298, 45);
            this.labelBuffered.Name = "labelBuffered";
            this.labelBuffered.Size = new System.Drawing.Size(27, 13);
            this.labelBuffered.TabIndex = 7;
            this.labelBuffered.Text = "0.0s";
            // 
            // labelVolume
            // 
            this.labelVolume.AutoSize = true;
            this.labelVolume.Location = new System.Drawing.Point(12, 73);
            this.labelVolume.Name = "labelVolume";
            this.labelVolume.Size = new System.Drawing.Size(45, 13);
            this.labelVolume.TabIndex = 8;
            this.labelVolume.Text = "Volume:";
            // 
            // volumeSlider1
            // 
            this.volumeSlider1.Location = new System.Drawing.Point(97, 69);
            this.volumeSlider1.Name = "volumeSlider1";
            this.volumeSlider1.Size = new System.Drawing.Size(109, 19);
            this.volumeSlider1.TabIndex = 9;
            this.volumeSlider1.Volume = 1F;
            // 
            // MP3StreamingPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.volumeSlider1);
            this.Controls.Add(this.labelVolume);
            this.Controls.Add(this.labelBuffered);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonPause);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.progressBarBuffer);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxStreamingUrl);
            this.Controls.Add(this.buttonPlay);
            this.Name = "Mp3StreamingPanel";
            this.Size = new System.Drawing.Size(337, 133);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.TextBox textBoxStreamingUrl;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ProgressBar progressBarBuffer;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonPause;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Label labelBuffered;
        private System.Windows.Forms.Label labelVolume;
        private NAudio.Gui.VolumeSlider volumeSlider1;
    }
}