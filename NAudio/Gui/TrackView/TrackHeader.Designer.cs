namespace NAudio.Gui.TrackView
{
    partial class TrackHeader
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
            this.textBoxTrackName = new System.Windows.Forms.TextBox();
            this.volumeSlider1 = new NAudio.Gui.VolumeSlider();
            this.panSlider1 = new NAudio.Gui.PanSlider();
            this.SuspendLayout();
            // 
            // textBoxTrackName
            // 
            this.textBoxTrackName.Location = new System.Drawing.Point(4, 0);
            this.textBoxTrackName.Name = "textBoxTrackName";
            this.textBoxTrackName.Size = new System.Drawing.Size(148, 20);
            this.textBoxTrackName.TabIndex = 0;
            // 
            // volumeSlider1
            // 
            this.volumeSlider1.Location = new System.Drawing.Point(4, 27);
            this.volumeSlider1.Name = "volumeSlider1";
            this.volumeSlider1.Size = new System.Drawing.Size(71, 16);
            this.volumeSlider1.TabIndex = 1;
            this.volumeSlider1.Volume = 1F;
            this.volumeSlider1.VolumeChanged += new System.EventHandler(this.volumeSlider1_VolumeChanged);
            // 
            // panSlider1
            // 
            this.panSlider1.Location = new System.Drawing.Point(81, 27);
            this.panSlider1.Name = "panSlider1";
            this.panSlider1.Pan = 0F;
            this.panSlider1.Size = new System.Drawing.Size(71, 16);
            this.panSlider1.TabIndex = 2;
            this.panSlider1.PanChanged += new System.EventHandler(this.panSlider1_PanChanged);
            // 
            // TrackHeader
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panSlider1);
            this.Controls.Add(this.volumeSlider1);
            this.Controls.Add(this.textBoxTrackName);
            this.Name = "TrackHeader";
            this.Size = new System.Drawing.Size(163, 52);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxTrackName;
        private VolumeSlider volumeSlider1;
        private PanSlider panSlider1;
    }
}
