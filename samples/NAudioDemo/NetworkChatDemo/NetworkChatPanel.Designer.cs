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
            this.textBoxIPAddress = new System.Windows.Forms.TextBox();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.comboBoxInputDevices = new System.Windows.Forms.ComboBox();
            this.comboBoxCodecs = new System.Windows.Forms.ComboBox();
            this.buttonStartStreaming = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.comboBoxProtocol = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // textBoxIPAddress
            // 
            this.textBoxIPAddress.Location = new System.Drawing.Point(107, 4);
            this.textBoxIPAddress.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxIPAddress.Name = "textBoxIPAddress";
            this.textBoxIPAddress.Size = new System.Drawing.Size(208, 22);
            this.textBoxIPAddress.TabIndex = 0;
            this.textBoxIPAddress.Text = "127.0.0.1";
            // 
            // textBoxPort
            // 
            this.textBoxPort.Location = new System.Drawing.Point(107, 36);
            this.textBoxPort.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(208, 22);
            this.textBoxPort.TabIndex = 1;
            this.textBoxPort.Text = "7080";
            // 
            // comboBoxInputDevices
            // 
            this.comboBoxInputDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxInputDevices.FormattingEnabled = true;
            this.comboBoxInputDevices.Location = new System.Drawing.Point(107, 73);
            this.comboBoxInputDevices.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBoxInputDevices.Name = "comboBoxInputDevices";
            this.comboBoxInputDevices.Size = new System.Drawing.Size(321, 24);
            this.comboBoxInputDevices.TabIndex = 2;
            // 
            // comboBoxCodecs
            // 
            this.comboBoxCodecs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCodecs.FormattingEnabled = true;
            this.comboBoxCodecs.Location = new System.Drawing.Point(107, 106);
            this.comboBoxCodecs.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBoxCodecs.Name = "comboBoxCodecs";
            this.comboBoxCodecs.Size = new System.Drawing.Size(321, 24);
            this.comboBoxCodecs.TabIndex = 3;
            // 
            // buttonStartStreaming
            // 
            this.buttonStartStreaming.Location = new System.Drawing.Point(7, 207);
            this.buttonStartStreaming.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonStartStreaming.Name = "buttonStartStreaming";
            this.buttonStartStreaming.Size = new System.Drawing.Size(172, 28);
            this.buttonStartStreaming.TabIndex = 4;
            this.buttonStartStreaming.Text = "Start Streaming";
            this.buttonStartStreaming.UseVisualStyleBackColor = true;
            this.buttonStartStreaming.Click += new System.EventHandler(this.buttonStartStreaming_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 6);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 17);
            this.label1.TabIndex = 6;
            this.label1.Text = "IP Address:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 39);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 17);
            this.label2.TabIndex = 6;
            this.label2.Text = "Port";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 76);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 17);
            this.label3.TabIndex = 7;
            this.label3.Text = "Input Device:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 110);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(90, 17);
            this.label4.TabIndex = 8;
            this.label4.Text = "Compression";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(4, 144);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(60, 17);
            this.label5.TabIndex = 10;
            this.label5.Text = "Protocol";
            // 
            // comboBoxProtocol
            // 
            this.comboBoxProtocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxProtocol.FormattingEnabled = true;
            this.comboBoxProtocol.Location = new System.Drawing.Point(107, 140);
            this.comboBoxProtocol.Margin = new System.Windows.Forms.Padding(4);
            this.comboBoxProtocol.Name = "comboBoxProtocol";
            this.comboBoxProtocol.Size = new System.Drawing.Size(321, 24);
            this.comboBoxProtocol.TabIndex = 9;
            // 
            // NetworkChatPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label5);
            this.Controls.Add(this.comboBoxProtocol);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonStartStreaming);
            this.Controls.Add(this.comboBoxCodecs);
            this.Controls.Add(this.comboBoxInputDevices);
            this.Controls.Add(this.textBoxPort);
            this.Controls.Add(this.textBoxIPAddress);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "NetworkChatPanel";
            this.Size = new System.Drawing.Size(791, 281);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxIPAddress;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.ComboBox comboBoxInputDevices;
        private System.Windows.Forms.ComboBox comboBoxCodecs;
        private System.Windows.Forms.Button buttonStartStreaming;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox comboBoxProtocol;
    }
}
