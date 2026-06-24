namespace NAudioDemo.AudioPlaybackDemo
{
    partial class WaveOutSettingsPanel
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
            this.comboBoxWaveOutDevice = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            //
            // comboBoxWaveOutDevice
            //
            this.comboBoxWaveOutDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxWaveOutDevice.FormattingEnabled = true;
            this.comboBoxWaveOutDevice.Location = new System.Drawing.Point(7, 7);
            this.comboBoxWaveOutDevice.Name = "comboBoxWaveOutDevice";
            this.comboBoxWaveOutDevice.Size = new System.Drawing.Size(232, 21);
            this.comboBoxWaveOutDevice.TabIndex = 19;
            //
            // WaveOutSettingsPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.comboBoxWaveOutDevice);
            this.Name = "WaveOutSettingsPanel";
            this.Size = new System.Drawing.Size(253, 40);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxWaveOutDevice;
    }
}
