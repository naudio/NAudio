namespace NAudioDemo.FadeInOutDemo
{
    partial class FadeInOutPanel
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
            this.buttonPlay = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.volumeSlider1 = new NAudio.Gui.VolumeSlider();
            this.buttonOpen = new System.Windows.Forms.Button();
            this.labelNowTime = new System.Windows.Forms.Label();
            this.labelTotalTime = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.buttonBeginFadeIn = new System.Windows.Forms.Button();
            this.buttonBeginFadeOut = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxFadeDuration = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
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
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // volumeSlider1
            // 
            this.volumeSlider1.Location = new System.Drawing.Point(307, 37);
            this.volumeSlider1.Name = "volumeSlider1";
            this.volumeSlider1.Size = new System.Drawing.Size(96, 16);
            this.volumeSlider1.TabIndex = 5;
            this.volumeSlider1.VolumeChanged += new System.EventHandler(this.volumeSlider1_VolumeChanged);
            // 
            // buttonOpen
            // 
            this.buttonOpen.Location = new System.Drawing.Point(10, 31);
            this.buttonOpen.Name = "buttonOpen";
            this.buttonOpen.Size = new System.Drawing.Size(75, 23);
            this.buttonOpen.TabIndex = 1;
            this.buttonOpen.Text = "Open";
            this.buttonOpen.UseVisualStyleBackColor = true;
            this.buttonOpen.Click += new System.EventHandler(this.buttonOpen_Click);
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
            // buttonBeginFadeIn
            // 
            this.buttonBeginFadeIn.Location = new System.Drawing.Point(10, 79);
            this.buttonBeginFadeIn.Name = "buttonBeginFadeIn";
            this.buttonBeginFadeIn.Size = new System.Drawing.Size(109, 23);
            this.buttonBeginFadeIn.TabIndex = 7;
            this.buttonBeginFadeIn.Text = "Begin Fade In";
            this.buttonBeginFadeIn.UseVisualStyleBackColor = true;
            this.buttonBeginFadeIn.Click += new System.EventHandler(this.buttonBeginFadeIn_Click);
            // 
            // buttonBeginFadeOut
            // 
            this.buttonBeginFadeOut.Location = new System.Drawing.Point(10, 108);
            this.buttonBeginFadeOut.Name = "buttonBeginFadeOut";
            this.buttonBeginFadeOut.Size = new System.Drawing.Size(109, 23);
            this.buttonBeginFadeOut.TabIndex = 7;
            this.buttonBeginFadeOut.Text = "Begin Fade Out";
            this.buttonBeginFadeOut.UseVisualStyleBackColor = true;
            this.buttonBeginFadeOut.Click += new System.EventHandler(this.buttonBeginFadeOut_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(380, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Play a file and use the Begin Fade In or Begin Fade Out buttons to trigger fades";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(148, 84);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(145, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Fade duration in milliseconds:";
            // 
            // textBoxFadeDuration
            // 
            this.textBoxFadeDuration.Location = new System.Drawing.Point(307, 81);
            this.textBoxFadeDuration.Name = "textBoxFadeDuration";
            this.textBoxFadeDuration.Size = new System.Drawing.Size(100, 20);
            this.textBoxFadeDuration.TabIndex = 10;
            this.textBoxFadeDuration.Text = "5000";
            // 
            // FadeInOutPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textBoxFadeDuration);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonBeginFadeOut);
            this.Controls.Add(this.buttonBeginFadeIn);
            this.Controls.Add(this.labelTotalTime);
            this.Controls.Add(this.labelNowTime);
            this.Controls.Add(this.volumeSlider1);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonOpen);
            this.Controls.Add(this.buttonPlay);
            this.Name = "FadeInOutPanel";
            this.Size = new System.Drawing.Size(821, 268);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.Button buttonStop;
        private NAudio.Gui.VolumeSlider volumeSlider1;
        private System.Windows.Forms.Button buttonOpen;
        private System.Windows.Forms.Label labelNowTime;
        private System.Windows.Forms.Label labelTotalTime;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button buttonBeginFadeIn;
        private System.Windows.Forms.Button buttonBeginFadeOut;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxFadeDuration;
    }
}
