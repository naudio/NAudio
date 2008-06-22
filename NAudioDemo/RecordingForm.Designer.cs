namespace NAudioDemo
{
    partial class RecordingForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RecordingForm));
            this.buttonStartRecording = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonStopRecording = new System.Windows.Forms.Button();
            this.checkBoxAutoPlay = new System.Windows.Forms.CheckBox();
            this.buttonSelectOutputFile = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonStartRecording
            // 
            this.buttonStartRecording.Location = new System.Drawing.Point(15, 80);
            this.buttonStartRecording.Name = "buttonStartRecording";
            this.buttonStartRecording.Size = new System.Drawing.Size(105, 23);
            this.buttonStartRecording.TabIndex = 0;
            this.buttonStartRecording.Text = "Start Recording";
            this.buttonStartRecording.UseVisualStyleBackColor = true;
            this.buttonStartRecording.Click += new System.EventHandler(this.buttonStartRecording_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(260, 61);
            this.label1.TabIndex = 1;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // buttonStopRecording
            // 
            this.buttonStopRecording.Location = new System.Drawing.Point(126, 80);
            this.buttonStopRecording.Name = "buttonStopRecording";
            this.buttonStopRecording.Size = new System.Drawing.Size(105, 23);
            this.buttonStopRecording.TabIndex = 0;
            this.buttonStopRecording.Text = "Stop Recording";
            this.buttonStopRecording.UseVisualStyleBackColor = true;
            this.buttonStopRecording.Click += new System.EventHandler(this.buttonStopRecording_Click);
            // 
            // checkBoxAutoPlay
            // 
            this.checkBoxAutoPlay.AutoSize = true;
            this.checkBoxAutoPlay.Checked = true;
            this.checkBoxAutoPlay.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAutoPlay.Location = new System.Drawing.Point(15, 109);
            this.checkBoxAutoPlay.Name = "checkBoxAutoPlay";
            this.checkBoxAutoPlay.Size = new System.Drawing.Size(107, 17);
            this.checkBoxAutoPlay.TabIndex = 2;
            this.checkBoxAutoPlay.Text = "Play recorded file";
            this.checkBoxAutoPlay.UseVisualStyleBackColor = true;
            // 
            // buttonSelectOutputFile
            // 
            this.buttonSelectOutputFile.Location = new System.Drawing.Point(15, 132);
            this.buttonSelectOutputFile.Name = "buttonSelectOutputFile";
            this.buttonSelectOutputFile.Size = new System.Drawing.Size(118, 23);
            this.buttonSelectOutputFile.TabIndex = 3;
            this.buttonSelectOutputFile.Text = "Select Output File...";
            this.buttonSelectOutputFile.UseVisualStyleBackColor = true;
            this.buttonSelectOutputFile.Click += new System.EventHandler(this.buttonSelectOutputFile_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(15, 178);
            this.progressBar1.Maximum = 30;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(257, 23);
            this.progressBar1.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 162);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Recording Progress:";
            // 
            // RecordingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(279, 264);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.buttonSelectOutputFile);
            this.Controls.Add(this.checkBoxAutoPlay);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonStopRecording);
            this.Controls.Add(this.buttonStartRecording);
            this.Name = "RecordingForm";
            this.ShowInTaskbar = false;
            this.Text = "RecordingForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStartRecording;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonStopRecording;
        private System.Windows.Forms.CheckBox checkBoxAutoPlay;
        private System.Windows.Forms.Button buttonSelectOutputFile;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label2;
    }
}