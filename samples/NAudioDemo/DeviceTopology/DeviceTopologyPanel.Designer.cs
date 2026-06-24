namespace NAudioDemo.DeviceTopology
{
    partial class DeviceTopologyPanel
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
            this.lblSelectDevice = new System.Windows.Forms.Label();
            this.cbDevices = new System.Windows.Forms.ComboBox();
            this.tbTopology = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lblSelectDevice
            // 
            this.lblSelectDevice.AutoSize = true;
            this.lblSelectDevice.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSelectDevice.Location = new System.Drawing.Point(0, 0);
            this.lblSelectDevice.Name = "lblSelectDevice";
            this.lblSelectDevice.Size = new System.Drawing.Size(111, 15);
            this.lblSelectDevice.TabIndex = 0;
            this.lblSelectDevice.Text = "Select audio device:";
            // 
            // cbDevices
            // 
            this.cbDevices.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDevices.FormattingEnabled = true;
            this.cbDevices.Location = new System.Drawing.Point(0, 15);
            this.cbDevices.Name = "cbDevices";
            this.cbDevices.Size = new System.Drawing.Size(1013, 23);
            this.cbDevices.TabIndex = 1;
            this.cbDevices.SelectedValueChanged += new System.EventHandler(this.cbDevices_SelectedValueChanged);
            // 
            // tbTopology
            // 
            this.tbTopology.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbTopology.Location = new System.Drawing.Point(0, 38);
            this.tbTopology.Multiline = true;
            this.tbTopology.Name = "tbTopology";
            this.tbTopology.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbTopology.Size = new System.Drawing.Size(1013, 483);
            this.tbTopology.TabIndex = 2;
            // 
            // DeviceTopologyPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tbTopology);
            this.Controls.Add(this.cbDevices);
            this.Controls.Add(this.lblSelectDevice);
            this.Name = "DeviceTopologyPanel";
            this.Size = new System.Drawing.Size(1013, 521);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblSelectDevice;
        private System.Windows.Forms.ComboBox cbDevices;
        private System.Windows.Forms.TextBox tbTopology;
    }
}
