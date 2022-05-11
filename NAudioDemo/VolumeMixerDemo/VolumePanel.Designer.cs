namespace NAudioDemo.VolumeMixerDemo
{
    partial class VolumePanel
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VolumePanel));
            this.tbVolume = new System.Windows.Forms.TrackBar();
            this.btnMuteUnmute = new System.Windows.Forms.Button();
            this.ilMuteUnmute = new System.Windows.Forms.ImageList(this.components);
            this.lblName = new System.Windows.Forms.Label();
            this.cmbDevice = new System.Windows.Forms.ComboBox();
            this.btnSoundProperties = new System.Windows.Forms.Button();
            this.tooltip = new System.Windows.Forms.ToolTip(this.components);
            this.pbProcessIcon = new System.Windows.Forms.PictureBox();
            this.buttonRecord = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.tbVolume)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbProcessIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // tbVolume
            // 
            this.tbVolume.Location = new System.Drawing.Point(68, 134);
            this.tbVolume.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tbVolume.Maximum = 100;
            this.tbVolume.Name = "tbVolume";
            this.tbVolume.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.tbVolume.Size = new System.Drawing.Size(56, 286);
            this.tbVolume.TabIndex = 3;
            this.tbVolume.TickStyle = System.Windows.Forms.TickStyle.None;
            this.tbVolume.Scroll += new System.EventHandler(this.tbVolume_Scroll);
            this.tbVolume.MouseCaptureChanged += new System.EventHandler(this.tbVolume_MouseCaptureChanged);
            // 
            // btnMuteUnmute
            // 
            this.btnMuteUnmute.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btnMuteUnmute.ImageList = this.ilMuteUnmute;
            this.btnMuteUnmute.Location = new System.Drawing.Point(63, 431);
            this.btnMuteUnmute.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnMuteUnmute.Name = "btnMuteUnmute";
            this.btnMuteUnmute.Size = new System.Drawing.Size(37, 43);
            this.btnMuteUnmute.TabIndex = 4;
            this.btnMuteUnmute.UseVisualStyleBackColor = true;
            this.btnMuteUnmute.Click += new System.EventHandler(this.btnMuteUnmute_Click);
            // 
            // ilMuteUnmute
            // 
            this.ilMuteUnmute.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.ilMuteUnmute.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilMuteUnmute.ImageStream")));
            this.ilMuteUnmute.TransparentColor = System.Drawing.Color.Transparent;
            this.ilMuteUnmute.Images.SetKeyName(0, "Mute.png");
            this.ilMuteUnmute.Images.SetKeyName(1, "Unmute.png");
            // 
            // lblName
            // 
            this.lblName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblName.Location = new System.Drawing.Point(20, 91);
            this.lblName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(127, 20);
            this.lblName.TabIndex = 5;
            this.lblName.Text = "lblAppName";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // cmbDevice
            // 
            this.cmbDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDevice.FormattingEnabled = true;
            this.cmbDevice.Location = new System.Drawing.Point(13, 86);
            this.cmbDevice.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cmbDevice.Name = "cmbDevice";
            this.cmbDevice.Size = new System.Drawing.Size(143, 28);
            this.cmbDevice.TabIndex = 7;
            this.cmbDevice.SelectedIndexChanged += new System.EventHandler(this.cmbDevice_SelectedIndexChanged);
            // 
            // btnSoundProperties
            // 
            this.btnSoundProperties.Image = global::NAudioDemo.Properties.Resources.Audiosrv;
            this.btnSoundProperties.Location = new System.Drawing.Point(53, 14);
            this.btnSoundProperties.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnSoundProperties.Name = "btnSoundProperties";
            this.btnSoundProperties.Size = new System.Drawing.Size(56, 65);
            this.btnSoundProperties.TabIndex = 8;
            this.btnSoundProperties.UseVisualStyleBackColor = true;
            this.btnSoundProperties.Click += new System.EventHandler(this.btnSoundProperties_Click);
            // 
            // pbProcessIcon
            // 
            this.pbProcessIcon.Location = new System.Drawing.Point(60, 18);
            this.pbProcessIcon.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pbProcessIcon.Name = "pbProcessIcon";
            this.pbProcessIcon.Size = new System.Drawing.Size(43, 49);
            this.pbProcessIcon.TabIndex = 6;
            this.pbProcessIcon.TabStop = false;
            // 
            // buttonRecord
            // 
            this.buttonRecord.Location = new System.Drawing.Point(60, 482);
            this.buttonRecord.Name = "buttonRecord";
            this.buttonRecord.Size = new System.Drawing.Size(47, 37);
            this.buttonRecord.TabIndex = 9;
            this.buttonRecord.Text = "REC";
            this.buttonRecord.UseVisualStyleBackColor = true;
            this.buttonRecord.Click += new System.EventHandler(this.OnRecordClick);
            // 
            // VolumePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.buttonRecord);
            this.Controls.Add(this.btnSoundProperties);
            this.Controls.Add(this.cmbDevice);
            this.Controls.Add(this.pbProcessIcon);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.btnMuteUnmute);
            this.Controls.Add(this.tbVolume);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "VolumePanel";
            this.Size = new System.Drawing.Size(172, 533);
            this.Load += new System.EventHandler(this.VolumePanel_Load);
            ((System.ComponentModel.ISupportInitialize)(this.tbVolume)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbProcessIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnMuteUnmute;
        private System.Windows.Forms.TrackBar tbVolume;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.ImageList ilMuteUnmute;
        private System.Windows.Forms.ComboBox cmbDevice;
        private System.Windows.Forms.Button btnSoundProperties;
        private System.Windows.Forms.PictureBox pbProcessIcon;
        private System.Windows.Forms.ToolTip tooltip;
        private System.Windows.Forms.Button buttonRecord;
    }
}