namespace NAudioDemo.DeviceNotificationsDemo
{
    partial class DeviceNotificationsPanel
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

        private void InitializeComponent()
        {
            this.labelDevices = new System.Windows.Forms.Label();
            this.listDevices = new System.Windows.Forms.ListBox();
            this.labelEvents = new System.Windows.Forms.Label();
            this.listEvents = new System.Windows.Forms.ListBox();
            this.buttonClear = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // labelDevices
            //
            this.labelDevices.AutoSize = true;
            this.labelDevices.Location = new System.Drawing.Point(12, 9);
            this.labelDevices.Text = "Devices (live):";
            //
            // listDevices
            //
            this.listDevices.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.listDevices.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.listDevices.Location = new System.Drawing.Point(12, 28);
            this.listDevices.Size = new System.Drawing.Size(620, 95);
            this.listDevices.IntegralHeight = false;
            //
            // labelEvents
            //
            this.labelEvents.AutoSize = true;
            this.labelEvents.Location = new System.Drawing.Point(12, 138);
            this.labelEvents.Text = "Notifications (newest first):";
            //
            // buttonClear
            //
            this.buttonClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClear.Location = new System.Drawing.Point(557, 134);
            this.buttonClear.Size = new System.Drawing.Size(75, 23);
            this.buttonClear.Text = "Clear";
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.OnClearClick);
            //
            // listEvents
            //
            this.listEvents.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.listEvents.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.listEvents.Location = new System.Drawing.Point(12, 163);
            this.listEvents.Size = new System.Drawing.Size(620, 250);
            this.listEvents.IntegralHeight = false;
            //
            // DeviceNotificationsPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelDevices);
            this.Controls.Add(this.listDevices);
            this.Controls.Add(this.labelEvents);
            this.Controls.Add(this.buttonClear);
            this.Controls.Add(this.listEvents);
            this.Name = "DeviceNotificationsPanel";
            this.Size = new System.Drawing.Size(644, 425);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label labelDevices;
        private System.Windows.Forms.ListBox listDevices;
        private System.Windows.Forms.Label labelEvents;
        private System.Windows.Forms.ListBox listEvents;
        private System.Windows.Forms.Button buttonClear;
    }
}
