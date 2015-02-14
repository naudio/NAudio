namespace NAudioDemo.AudioPlaybackDemo
{
    partial class AsioOutSettingsPanel
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
            this.comboBoxAsioDriver = new System.Windows.Forms.ComboBox();
            this.buttonControlPanel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // comboBoxAsioDriver
            // 
            this.comboBoxAsioDriver.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxAsioDriver.FormattingEnabled = true;
            this.comboBoxAsioDriver.Location = new System.Drawing.Point(3, 3);
            this.comboBoxAsioDriver.Name = "comboBoxAsioDriver";
            this.comboBoxAsioDriver.Size = new System.Drawing.Size(232, 21);
            this.comboBoxAsioDriver.TabIndex = 18;
            // 
            // buttonControlPanel
            // 
            this.buttonControlPanel.Location = new System.Drawing.Point(3, 30);
            this.buttonControlPanel.Name = "buttonControlPanel";
            this.buttonControlPanel.Size = new System.Drawing.Size(135, 23);
            this.buttonControlPanel.TabIndex = 17;
            this.buttonControlPanel.Text = "ASIO Control Panel";
            this.buttonControlPanel.UseVisualStyleBackColor = true;
            this.buttonControlPanel.Click += new System.EventHandler(this.buttonControlPanel_Click);
            // 
            // AsioOutSettingsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.comboBoxAsioDriver);
            this.Controls.Add(this.buttonControlPanel);
            this.Name = "AsioOutSettingsPanel";
            this.Size = new System.Drawing.Size(242, 62);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxAsioDriver;
        private System.Windows.Forms.Button buttonControlPanel;
    }
}
