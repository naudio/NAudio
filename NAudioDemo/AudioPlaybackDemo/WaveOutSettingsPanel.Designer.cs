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
            this.checkBoxWaveOutWindow = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // comboBoxWaveOutDevice
            // 
            this.comboBoxWaveOutDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxWaveOutDevice.FormattingEnabled = true;
            this.comboBoxWaveOutDevice.Location = new System.Drawing.Point(3, 22);
            this.comboBoxWaveOutDevice.Name = "comboBoxWaveOutDevice";
            this.comboBoxWaveOutDevice.Size = new System.Drawing.Size(232, 21);
            this.comboBoxWaveOutDevice.TabIndex = 19;
            // 
            // checkBoxWaveOutWindow
            // 
            this.checkBoxWaveOutWindow.AutoSize = true;
            this.checkBoxWaveOutWindow.Checked = true;
            this.checkBoxWaveOutWindow.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWaveOutWindow.Location = new System.Drawing.Point(3, 6);
            this.checkBoxWaveOutWindow.Name = "checkBoxWaveOutWindow";
            this.checkBoxWaveOutWindow.Size = new System.Drawing.Size(126, 17);
            this.checkBoxWaveOutWindow.TabIndex = 18;
            this.checkBoxWaveOutWindow.Text = "Callback via Window";
            this.checkBoxWaveOutWindow.UseVisualStyleBackColor = true;
            // 
            // WaveOutSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.comboBoxWaveOutDevice);
            this.Controls.Add(this.checkBoxWaveOutWindow);
            this.Name = "WaveOutSettings";
            this.Size = new System.Drawing.Size(253, 54);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxWaveOutDevice;
        private System.Windows.Forms.CheckBox checkBoxWaveOutWindow;
    }
}
