namespace NAudioDemo.NetworkChatDemo
{
    partial class NetworkChatPanel
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
            this.textBoxRemoteHost = new System.Windows.Forms.TextBox();
            this.textBoxRemotePort = new System.Windows.Forms.TextBox();
            this.textBoxListenPort = new System.Windows.Forms.TextBox();
            this.comboBoxInputDevices = new System.Windows.Forms.ComboBox();
            this.comboBoxCodecs = new System.Windows.Forms.ComboBox();
            this.comboBoxProtocol = new System.Windows.Forms.ComboBox();
            this.buttonStartStreaming = new System.Windows.Forms.Button();
            this.labelRemoteHost = new System.Windows.Forms.Label();
            this.labelRemotePort = new System.Windows.Forms.Label();
            this.labelListenPort = new System.Windows.Forms.Label();
            this.labelInputDevice = new System.Windows.Forms.Label();
            this.labelCodec = new System.Windows.Forms.Label();
            this.labelProtocol = new System.Windows.Forms.Label();
            this.labelHelp = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // textBoxRemoteHost
            //
            this.textBoxRemoteHost.Location = new System.Drawing.Point(130, 4);
            this.textBoxRemoteHost.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxRemoteHost.Name = "textBoxRemoteHost";
            this.textBoxRemoteHost.Size = new System.Drawing.Size(208, 22);
            this.textBoxRemoteHost.TabIndex = 0;
            this.textBoxRemoteHost.Text = "127.0.0.1";
            //
            // textBoxRemotePort
            //
            this.textBoxRemotePort.Location = new System.Drawing.Point(130, 37);
            this.textBoxRemotePort.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxRemotePort.Name = "textBoxRemotePort";
            this.textBoxRemotePort.Size = new System.Drawing.Size(208, 22);
            this.textBoxRemotePort.TabIndex = 1;
            this.textBoxRemotePort.Text = "7080";
            //
            // textBoxListenPort
            //
            this.textBoxListenPort.Location = new System.Drawing.Point(130, 70);
            this.textBoxListenPort.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxListenPort.Name = "textBoxListenPort";
            this.textBoxListenPort.Size = new System.Drawing.Size(208, 22);
            this.textBoxListenPort.TabIndex = 2;
            this.textBoxListenPort.Text = "7080";
            //
            // comboBoxInputDevices
            //
            this.comboBoxInputDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxInputDevices.FormattingEnabled = true;
            this.comboBoxInputDevices.Location = new System.Drawing.Point(130, 103);
            this.comboBoxInputDevices.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBoxInputDevices.Name = "comboBoxInputDevices";
            this.comboBoxInputDevices.Size = new System.Drawing.Size(321, 24);
            this.comboBoxInputDevices.TabIndex = 3;
            //
            // comboBoxCodecs
            //
            this.comboBoxCodecs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCodecs.FormattingEnabled = true;
            this.comboBoxCodecs.Location = new System.Drawing.Point(130, 136);
            this.comboBoxCodecs.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBoxCodecs.Name = "comboBoxCodecs";
            this.comboBoxCodecs.Size = new System.Drawing.Size(321, 24);
            this.comboBoxCodecs.TabIndex = 4;
            //
            // comboBoxProtocol
            //
            this.comboBoxProtocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxProtocol.FormattingEnabled = true;
            this.comboBoxProtocol.Location = new System.Drawing.Point(130, 169);
            this.comboBoxProtocol.Margin = new System.Windows.Forms.Padding(4);
            this.comboBoxProtocol.Name = "comboBoxProtocol";
            this.comboBoxProtocol.Size = new System.Drawing.Size(321, 24);
            this.comboBoxProtocol.TabIndex = 5;
            //
            // buttonStartStreaming
            //
            this.buttonStartStreaming.Location = new System.Drawing.Point(130, 205);
            this.buttonStartStreaming.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonStartStreaming.Name = "buttonStartStreaming";
            this.buttonStartStreaming.Size = new System.Drawing.Size(172, 28);
            this.buttonStartStreaming.TabIndex = 6;
            this.buttonStartStreaming.Text = "Start Streaming";
            this.buttonStartStreaming.UseVisualStyleBackColor = true;
            this.buttonStartStreaming.Click += new System.EventHandler(this.buttonStartStreaming_Click);
            //
            // labelRemoteHost
            //
            this.labelRemoteHost.AutoSize = true;
            this.labelRemoteHost.Location = new System.Drawing.Point(4, 8);
            this.labelRemoteHost.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelRemoteHost.Name = "labelRemoteHost";
            this.labelRemoteHost.Size = new System.Drawing.Size(90, 17);
            this.labelRemoteHost.TabIndex = 7;
            this.labelRemoteHost.Text = "Remote host:";
            //
            // labelRemotePort
            //
            this.labelRemotePort.AutoSize = true;
            this.labelRemotePort.Location = new System.Drawing.Point(4, 41);
            this.labelRemotePort.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelRemotePort.Name = "labelRemotePort";
            this.labelRemotePort.Size = new System.Drawing.Size(85, 17);
            this.labelRemotePort.TabIndex = 8;
            this.labelRemotePort.Text = "Remote port:";
            //
            // labelListenPort
            //
            this.labelListenPort.AutoSize = true;
            this.labelListenPort.Location = new System.Drawing.Point(4, 74);
            this.labelListenPort.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelListenPort.Name = "labelListenPort";
            this.labelListenPort.Size = new System.Drawing.Size(75, 17);
            this.labelListenPort.TabIndex = 9;
            this.labelListenPort.Text = "Listen port:";
            //
            // labelInputDevice
            //
            this.labelInputDevice.AutoSize = true;
            this.labelInputDevice.Location = new System.Drawing.Point(4, 107);
            this.labelInputDevice.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelInputDevice.Name = "labelInputDevice";
            this.labelInputDevice.Size = new System.Drawing.Size(90, 17);
            this.labelInputDevice.TabIndex = 10;
            this.labelInputDevice.Text = "Input device:";
            //
            // labelCodec
            //
            this.labelCodec.AutoSize = true;
            this.labelCodec.Location = new System.Drawing.Point(4, 140);
            this.labelCodec.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelCodec.Name = "labelCodec";
            this.labelCodec.Size = new System.Drawing.Size(50, 17);
            this.labelCodec.TabIndex = 11;
            this.labelCodec.Text = "Codec:";
            //
            // labelProtocol
            //
            this.labelProtocol.AutoSize = true;
            this.labelProtocol.Location = new System.Drawing.Point(4, 173);
            this.labelProtocol.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelProtocol.Name = "labelProtocol";
            this.labelProtocol.Size = new System.Drawing.Size(60, 17);
            this.labelProtocol.TabIndex = 12;
            this.labelProtocol.Text = "Protocol:";
            //
            // labelHelp
            //
            this.labelHelp.AutoSize = true;
            this.labelHelp.ForeColor = System.Drawing.SystemColors.GrayText;
            this.labelHelp.Location = new System.Drawing.Point(4, 245);
            this.labelHelp.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelHelp.Name = "labelHelp";
            this.labelHelp.Size = new System.Drawing.Size(0, 17);
            this.labelHelp.TabIndex = 13;
            this.labelHelp.Text = "Two PCs: set Remote host to the other machine and use the same port on both.\r\n" +
                "One PC: run two instances and swap the ports (e.g. A listens 7080 / sends 7081, B listens 7081 / sends 7080).\r\n" +
                "UDP is recommended for live audio; you will hear your own voice if you point Remote host back at yourself.";
            //
            // NetworkChatPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelHelp);
            this.Controls.Add(this.labelProtocol);
            this.Controls.Add(this.labelCodec);
            this.Controls.Add(this.labelInputDevice);
            this.Controls.Add(this.labelListenPort);
            this.Controls.Add(this.labelRemotePort);
            this.Controls.Add(this.labelRemoteHost);
            this.Controls.Add(this.buttonStartStreaming);
            this.Controls.Add(this.comboBoxProtocol);
            this.Controls.Add(this.comboBoxCodecs);
            this.Controls.Add(this.comboBoxInputDevices);
            this.Controls.Add(this.textBoxListenPort);
            this.Controls.Add(this.textBoxRemotePort);
            this.Controls.Add(this.textBoxRemoteHost);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "NetworkChatPanel";
            this.Size = new System.Drawing.Size(791, 320);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxRemoteHost;
        private System.Windows.Forms.TextBox textBoxRemotePort;
        private System.Windows.Forms.TextBox textBoxListenPort;
        private System.Windows.Forms.ComboBox comboBoxInputDevices;
        private System.Windows.Forms.ComboBox comboBoxCodecs;
        private System.Windows.Forms.ComboBox comboBoxProtocol;
        private System.Windows.Forms.Button buttonStartStreaming;
        private System.Windows.Forms.Label labelRemoteHost;
        private System.Windows.Forms.Label labelRemotePort;
        private System.Windows.Forms.Label labelListenPort;
        private System.Windows.Forms.Label labelInputDevice;
        private System.Windows.Forms.Label labelCodec;
        private System.Windows.Forms.Label labelProtocol;
        private System.Windows.Forms.Label labelHelp;
    }
}
