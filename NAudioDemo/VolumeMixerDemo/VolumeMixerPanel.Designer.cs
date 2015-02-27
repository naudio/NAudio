namespace NAudioDemo.VolumeMixerDemo
{
    partial class VolumeMixerPanel
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanelDevice = new System.Windows.Forms.FlowLayoutPanel();
            this.gbApplications = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanelApps = new System.Windows.Forms.FlowLayoutPanel();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.gbApplications.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.flowLayoutPanelDevice);
            this.groupBox1.Location = new System.Drawing.Point(12, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(143, 363);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Device";
            // 
            // flowLayoutPanelDevice
            // 
            this.flowLayoutPanelDevice.AutoScroll = true;
            this.flowLayoutPanelDevice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelDevice.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelDevice.Location = new System.Drawing.Point(3, 16);
            this.flowLayoutPanelDevice.Name = "flowLayoutPanelDevice";
            this.flowLayoutPanelDevice.Size = new System.Drawing.Size(137, 344);
            this.flowLayoutPanelDevice.TabIndex = 1;
            // 
            // gbApplications
            // 
            this.gbApplications.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbApplications.Controls.Add(this.flowLayoutPanelApps);
            this.gbApplications.Location = new System.Drawing.Point(158, 3);
            this.gbApplications.Name = "gbApplications";
            this.gbApplications.Size = new System.Drawing.Size(538, 363);
            this.gbApplications.TabIndex = 2;
            this.gbApplications.TabStop = false;
            this.gbApplications.Text = "Applications";
            // 
            // flowLayoutPanelApps
            // 
            this.flowLayoutPanelApps.AutoScroll = true;
            this.flowLayoutPanelApps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelApps.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelApps.Location = new System.Drawing.Point(3, 16);
            this.flowLayoutPanelApps.Name = "flowLayoutPanelApps";
            this.flowLayoutPanelApps.Size = new System.Drawing.Size(532, 344);
            this.flowLayoutPanelApps.TabIndex = 0;
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(14, 375);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(137, 23);
            this.btnUpdate.TabIndex = 3;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // VolumeMixerPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.gbApplications);
            this.Controls.Add(this.groupBox1);
            this.Name = "VolumeMixerPanel";
            this.Size = new System.Drawing.Size(705, 417);
            this.groupBox1.ResumeLayout(false);
            this.gbApplications.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox gbApplications;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelApps;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelDevice;
        private System.Windows.Forms.Button btnUpdate;

    }
}