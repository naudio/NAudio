namespace NAudioDemo
{
    partial class MidiInPanel
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.comboBoxMidiInDevices = new System.Windows.Forms.ComboBox();
            this.labelDevice = new System.Windows.Forms.Label();
            this.buttonMonitor = new System.Windows.Forms.Button();
            this.checkBoxFilterAutoSensing = new System.Windows.Forms.CheckBox();
            this.progressLog1 = new NAudio.Utils.ProgressLog();
            this.buttonClearLog = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBoxMidiOutMessages = new System.Windows.Forms.CheckBox();
            this.comboBoxMidiOutDevices = new System.Windows.Forms.ComboBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBoxMidiInDevices
            // 
            this.comboBoxMidiInDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMidiInDevices.FormattingEnabled = true;
            this.comboBoxMidiInDevices.Location = new System.Drawing.Point(94, 12);
            this.comboBoxMidiInDevices.Name = "comboBoxMidiInDevices";
            this.comboBoxMidiInDevices.Size = new System.Drawing.Size(178, 21);
            this.comboBoxMidiInDevices.TabIndex = 0;
            // 
            // labelDevice
            // 
            this.labelDevice.AutoSize = true;
            this.labelDevice.Location = new System.Drawing.Point(12, 16);
            this.labelDevice.Name = "labelDevice";
            this.labelDevice.Size = new System.Drawing.Size(41, 13);
            this.labelDevice.TabIndex = 1;
            this.labelDevice.Text = "Device";
            // 
            // buttonMonitor
            // 
            this.buttonMonitor.Location = new System.Drawing.Point(12, 41);
            this.buttonMonitor.Name = "buttonMonitor";
            this.buttonMonitor.Size = new System.Drawing.Size(75, 23);
            this.buttonMonitor.TabIndex = 2;
            this.buttonMonitor.Text = "Monitor";
            this.buttonMonitor.UseVisualStyleBackColor = true;
            this.buttonMonitor.Click += new System.EventHandler(this.buttonMonitor_Click);
            // 
            // checkBoxFilterAutoSensing
            // 
            this.checkBoxFilterAutoSensing.AutoSize = true;
            this.checkBoxFilterAutoSensing.Location = new System.Drawing.Point(384, 47);
            this.checkBoxFilterAutoSensing.Name = "checkBoxFilterAutoSensing";
            this.checkBoxFilterAutoSensing.Size = new System.Drawing.Size(114, 17);
            this.checkBoxFilterAutoSensing.TabIndex = 4;
            this.checkBoxFilterAutoSensing.Text = "Filter Auto-Sensing";
            this.checkBoxFilterAutoSensing.UseVisualStyleBackColor = true;
            // 
            // progressLog1
            // 
            this.progressLog1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressLog1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.progressLog1.Location = new System.Drawing.Point(12, 70);
            this.progressLog1.Name = "progressLog1";
            this.progressLog1.Padding = new System.Windows.Forms.Padding(1);
            this.progressLog1.Size = new System.Drawing.Size(486, 258);
            this.progressLog1.TabIndex = 3;
            // 
            // buttonClearLog
            // 
            this.buttonClearLog.Location = new System.Drawing.Point(94, 41);
            this.buttonClearLog.Name = "buttonClearLog";
            this.buttonClearLog.Size = new System.Drawing.Size(75, 23);
            this.buttonClearLog.TabIndex = 5;
            this.buttonClearLog.Text = "Clear Log";
            this.buttonClearLog.UseVisualStyleBackColor = true;
            this.buttonClearLog.Click += new System.EventHandler(this.buttonClearLog_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.checkBoxMidiOutMessages);
            this.groupBox1.Controls.Add(this.comboBoxMidiOutDevices);
            this.groupBox1.Location = new System.Drawing.Point(13, 343);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(484, 45);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "MIDI Out";
            // 
            // checkBoxMidiOutMessages
            // 
            this.checkBoxMidiOutMessages.AutoSize = true;
            this.checkBoxMidiOutMessages.Location = new System.Drawing.Point(6, 19);
            this.checkBoxMidiOutMessages.Name = "checkBoxMidiOutMessages";
            this.checkBoxMidiOutMessages.Size = new System.Drawing.Size(172, 17);
            this.checkBoxMidiOutMessages.TabIndex = 0;
            this.checkBoxMidiOutMessages.Text = "Send Test MIDI Out Messages";
            this.checkBoxMidiOutMessages.UseVisualStyleBackColor = true;
            // 
            // comboBoxMidiOutDevices
            // 
            this.comboBoxMidiOutDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMidiOutDevices.FormattingEnabled = true;
            this.comboBoxMidiOutDevices.Location = new System.Drawing.Point(196, 17);
            this.comboBoxMidiOutDevices.Name = "comboBoxMidiOutDevices";
            this.comboBoxMidiOutDevices.Size = new System.Drawing.Size(178, 21);
            this.comboBoxMidiOutDevices.TabIndex = 0;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // MidiInForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(510, 399);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.buttonClearLog);
            this.Controls.Add(this.checkBoxFilterAutoSensing);
            this.Controls.Add(this.progressLog1);
            this.Controls.Add(this.buttonMonitor);
            this.Controls.Add(this.labelDevice);
            this.Controls.Add(this.comboBoxMidiInDevices);
            this.Name = "MidiInForm";
            this.Text = "MIDI In Sample";
            this.Disposed += this.MidiInPanel_Disposed;
            this.Load += new System.EventHandler(this.MidiInForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxMidiInDevices;
        private System.Windows.Forms.Label labelDevice;
        private System.Windows.Forms.Button buttonMonitor;
        private NAudio.Utils.ProgressLog progressLog1;
        private System.Windows.Forms.CheckBox checkBoxFilterAutoSensing;
        private System.Windows.Forms.Button buttonClearLog;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox checkBoxMidiOutMessages;
        private System.Windows.Forms.ComboBox comboBoxMidiOutDevices;
        private System.Windows.Forms.Timer timer1;
    }
}