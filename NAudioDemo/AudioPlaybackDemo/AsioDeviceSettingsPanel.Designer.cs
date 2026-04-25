namespace NAudioDemo.AudioPlaybackDemo
{
    partial class AsioDeviceSettingsPanel
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.comboBoxAsioDriver = new System.Windows.Forms.ComboBox();
            this.buttonControlPanel = new System.Windows.Forms.Button();
            this.labelOffset = new System.Windows.Forms.Label();
            this.textBoxChannelOffset = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            //
            // comboBoxAsioDriver
            //
            this.comboBoxAsioDriver.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxAsioDriver.FormattingEnabled = true;
            this.comboBoxAsioDriver.Location = new System.Drawing.Point(3, 3);
            this.comboBoxAsioDriver.Name = "comboBoxAsioDriver";
            this.comboBoxAsioDriver.Size = new System.Drawing.Size(232, 21);
            this.comboBoxAsioDriver.TabIndex = 0;
            //
            // buttonControlPanel
            //
            this.buttonControlPanel.Location = new System.Drawing.Point(3, 30);
            this.buttonControlPanel.Name = "buttonControlPanel";
            this.buttonControlPanel.Size = new System.Drawing.Size(135, 23);
            this.buttonControlPanel.TabIndex = 1;
            this.buttonControlPanel.Text = "ASIO Control Panel";
            this.buttonControlPanel.UseVisualStyleBackColor = true;
            this.buttonControlPanel.Click += new System.EventHandler(this.OnButtonControlPanelClick);
            //
            // labelOffset
            //
            this.labelOffset.AutoSize = true;
            this.labelOffset.Location = new System.Drawing.Point(3, 62);
            this.labelOffset.Name = "labelOffset";
            this.labelOffset.Size = new System.Drawing.Size(125, 13);
            this.labelOffset.TabIndex = 2;
            this.labelOffset.Text = "Starting output channel:";
            //
            // textBoxChannelOffset
            //
            this.textBoxChannelOffset.Location = new System.Drawing.Point(135, 59);
            this.textBoxChannelOffset.Name = "textBoxChannelOffset";
            this.textBoxChannelOffset.Size = new System.Drawing.Size(40, 20);
            this.textBoxChannelOffset.TabIndex = 3;
            this.textBoxChannelOffset.Text = "0";
            //
            // AsioDeviceSettingsPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textBoxChannelOffset);
            this.Controls.Add(this.labelOffset);
            this.Controls.Add(this.comboBoxAsioDriver);
            this.Controls.Add(this.buttonControlPanel);
            this.Name = "AsioDeviceSettingsPanel";
            this.Size = new System.Drawing.Size(242, 90);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxAsioDriver;
        private System.Windows.Forms.Button buttonControlPanel;
        private System.Windows.Forms.Label labelOffset;
        private System.Windows.Forms.TextBox textBoxChannelOffset;
    }
}
