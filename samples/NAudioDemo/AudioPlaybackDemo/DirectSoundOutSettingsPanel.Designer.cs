namespace NAudioDemo.AudioPlaybackDemo
{
    partial class DirectSoundOutSettingsPanel
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
            this.comboBoxDirectSound = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // comboBoxDirectSound
            // 
            this.comboBoxDirectSound.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDirectSound.FormattingEnabled = true;
            this.comboBoxDirectSound.Location = new System.Drawing.Point(3, 3);
            this.comboBoxDirectSound.Name = "comboBoxDirectSound";
            this.comboBoxDirectSound.Size = new System.Drawing.Size(232, 21);
            this.comboBoxDirectSound.TabIndex = 18;
            // 
            // DirectSoundOutSettingsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.comboBoxDirectSound);
            this.Name = "DirectSoundOutSettingsPanel";
            this.Size = new System.Drawing.Size(243, 38);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxDirectSound;
    }
}
