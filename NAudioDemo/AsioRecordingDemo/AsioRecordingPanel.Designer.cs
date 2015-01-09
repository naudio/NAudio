namespace NAudioDemo
{
    partial class AsioRecordingPanel
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
            this.label1 = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.comboBoxAsioDevice = new System.Windows.Forms.ComboBox();
            this.textBoxChannelOffset = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.textBoxChannelCount = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.listBoxRecordings = new System.Windows.Forms.ListBox();
            this.buttonPlay = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.buttonControlPanel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(4, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(561, 33);
            this.label1.TabIndex = 0;
            this.label1.Text = "This is for testing ASIO Recording";
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(409, 40);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 2;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(490, 40);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 2;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // comboBoxAsioDevice
            // 
            this.comboBoxAsioDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxAsioDevice.FormattingEnabled = true;
            this.comboBoxAsioDevice.Location = new System.Drawing.Point(118, 42);
            this.comboBoxAsioDevice.Name = "comboBoxAsioDevice";
            this.comboBoxAsioDevice.Size = new System.Drawing.Size(285, 21);
            this.comboBoxAsioDevice.TabIndex = 3;
            // 
            // textBoxChannelOffset
            // 
            this.textBoxChannelOffset.Location = new System.Drawing.Point(100, 104);
            this.textBoxChannelOffset.Name = "textBoxChannelOffset";
            this.textBoxChannelOffset.Size = new System.Drawing.Size(46, 20);
            this.textBoxChannelOffset.TabIndex = 4;
            this.textBoxChannelOffset.Text = "0";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 107);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Channel Offset:";
            // 
            // timer1
            // 
            this.timer1.Interval = 250;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // textBoxChannelCount
            // 
            this.textBoxChannelCount.Location = new System.Drawing.Point(100, 74);
            this.textBoxChannelCount.Name = "textBoxChannelCount";
            this.textBoxChannelCount.Size = new System.Drawing.Size(46, 20);
            this.textBoxChannelCount.TabIndex = 4;
            this.textBoxChannelCount.Text = "1";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Channel Count:";
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(152, 77);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(413, 30);
            this.label4.TabIndex = 5;
            this.label4.Text = "This is the number of channels to record. Can\'t be set to more inputs than your d" +
    "evice has available";
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(152, 107);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(413, 30);
            this.label5.TabIndex = 5;
            this.label5.Text = "Use channel offset to skip over a number of input channels and select the one you" +
    " want (normally channel count would be set to 1 in this case)";
            // 
            // listBoxRecordings
            // 
            this.listBoxRecordings.FormattingEnabled = true;
            this.listBoxRecordings.Location = new System.Drawing.Point(17, 144);
            this.listBoxRecordings.Name = "listBoxRecordings";
            this.listBoxRecordings.Size = new System.Drawing.Size(424, 95);
            this.listBoxRecordings.TabIndex = 6;
            // 
            // buttonPlay
            // 
            this.buttonPlay.Location = new System.Drawing.Point(448, 144);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(75, 23);
            this.buttonPlay.TabIndex = 7;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // buttonDelete
            // 
            this.buttonDelete.Location = new System.Drawing.Point(448, 173);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(75, 23);
            this.buttonDelete.TabIndex = 7;
            this.buttonDelete.Text = "Delete";
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
            // 
            // buttonControlPanel
            // 
            this.buttonControlPanel.Location = new System.Drawing.Point(17, 40);
            this.buttonControlPanel.Name = "buttonControlPanel";
            this.buttonControlPanel.Size = new System.Drawing.Size(95, 23);
            this.buttonControlPanel.TabIndex = 2;
            this.buttonControlPanel.Text = "Control Panel";
            this.buttonControlPanel.UseVisualStyleBackColor = true;
            this.buttonControlPanel.Click += new System.EventHandler(this.buttonControlPanel_Click);
            // 
            // AsioRecordingPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.buttonPlay);
            this.Controls.Add(this.listBoxRecordings);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxChannelCount);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxChannelOffset);
            this.Controls.Add(this.comboBoxAsioDevice);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonControlPanel);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.label1);
            this.Name = "AsioRecordingPanel";
            this.Size = new System.Drawing.Size(568, 253);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.ComboBox comboBoxAsioDevice;
        private System.Windows.Forms.TextBox textBoxChannelOffset;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TextBox textBoxChannelCount;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox listBoxRecordings;
        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.Button buttonDelete;
        private System.Windows.Forms.Button buttonControlPanel;
    }
}
