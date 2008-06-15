namespace NAudioDemo
{
    partial class AcmForm
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
            this.listBoxAcmDrivers = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonEncode = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBoxAutoLaunchEncodedFile = new System.Windows.Forms.CheckBox();
            this.radioButton5 = new System.Windows.Forms.RadioButton();
            this.radioButtonGsm610 = new System.Windows.Forms.RadioButton();
            this.radioButtonAdpcm = new System.Windows.Forms.RadioButton();
            this.radioButtonALaw = new System.Windows.Forms.RadioButton();
            this.radioButtonMuLaw = new System.Windows.Forms.RadioButton();
            this.buttonChooseFormat = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxAcmDrivers
            // 
            this.listBoxAcmDrivers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxAcmDrivers.FormattingEnabled = true;
            this.listBoxAcmDrivers.Location = new System.Drawing.Point(12, 28);
            this.listBoxAcmDrivers.Name = "listBoxAcmDrivers";
            this.listBoxAcmDrivers.Size = new System.Drawing.Size(358, 95);
            this.listBoxAcmDrivers.TabIndex = 0;
            this.listBoxAcmDrivers.SelectedIndexChanged += new System.EventHandler(this.listBoxAcmDrivers_SelectedIndexChanged);
            this.listBoxAcmDrivers.DoubleClick += new System.EventHandler(this.listBoxAcmDrivers_DoubleClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(282, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "The following ACM Codecs are installed on your computer:";
            // 
            // buttonEncode
            // 
            this.buttonEncode.Location = new System.Drawing.Point(6, 134);
            this.buttonEncode.Name = "buttonEncode";
            this.buttonEncode.Size = new System.Drawing.Size(108, 23);
            this.buttonEncode.TabIndex = 2;
            this.buttonEncode.Text = "Encode";
            this.buttonEncode.UseVisualStyleBackColor = true;
            this.buttonEncode.Click += new System.EventHandler(this.buttonEncode_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxAutoLaunchEncodedFile);
            this.groupBox1.Controls.Add(this.radioButton5);
            this.groupBox1.Controls.Add(this.radioButtonGsm610);
            this.groupBox1.Controls.Add(this.radioButtonAdpcm);
            this.groupBox1.Controls.Add(this.radioButtonALaw);
            this.groupBox1.Controls.Add(this.radioButtonMuLaw);
            this.groupBox1.Controls.Add(this.buttonEncode);
            this.groupBox1.Location = new System.Drawing.Point(15, 129);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 184);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Encode";
            // 
            // checkBoxAutoLaunchEncodedFile
            // 
            this.checkBoxAutoLaunchEncodedFile.AutoSize = true;
            this.checkBoxAutoLaunchEncodedFile.Location = new System.Drawing.Point(6, 163);
            this.checkBoxAutoLaunchEncodedFile.Name = "checkBoxAutoLaunchEncodedFile";
            this.checkBoxAutoLaunchEncodedFile.Size = new System.Drawing.Size(152, 17);
            this.checkBoxAutoLaunchEncodedFile.TabIndex = 5;
            this.checkBoxAutoLaunchEncodedFile.Text = "Auto-Launch Encoded File";
            this.checkBoxAutoLaunchEncodedFile.UseVisualStyleBackColor = true;
            // 
            // radioButton5
            // 
            this.radioButton5.AutoSize = true;
            this.radioButton5.Location = new System.Drawing.Point(6, 111);
            this.radioButton5.Name = "radioButton5";
            this.radioButton5.Size = new System.Drawing.Size(85, 17);
            this.radioButton5.TabIndex = 4;
            this.radioButton5.TabStop = true;
            this.radioButton5.Text = "IMA ADPCM";
            this.radioButton5.UseVisualStyleBackColor = true;
            // 
            // radioButtonGsm610
            // 
            this.radioButtonGsm610.AutoSize = true;
            this.radioButtonGsm610.Location = new System.Drawing.Point(6, 88);
            this.radioButtonGsm610.Name = "radioButtonGsm610";
            this.radioButtonGsm610.Size = new System.Drawing.Size(73, 17);
            this.radioButtonGsm610.TabIndex = 4;
            this.radioButtonGsm610.TabStop = true;
            this.radioButtonGsm610.Text = "GSM 6.10";
            this.radioButtonGsm610.UseVisualStyleBackColor = true;
            // 
            // radioButtonAdpcm
            // 
            this.radioButtonAdpcm.AutoSize = true;
            this.radioButtonAdpcm.Location = new System.Drawing.Point(6, 65);
            this.radioButtonAdpcm.Name = "radioButtonAdpcm";
            this.radioButtonAdpcm.Size = new System.Drawing.Size(63, 17);
            this.radioButtonAdpcm.TabIndex = 4;
            this.radioButtonAdpcm.TabStop = true;
            this.radioButtonAdpcm.Text = "ADPCM";
            this.radioButtonAdpcm.UseVisualStyleBackColor = true;
            // 
            // radioButtonALaw
            // 
            this.radioButtonALaw.AutoSize = true;
            this.radioButtonALaw.Location = new System.Drawing.Point(6, 42);
            this.radioButtonALaw.Name = "radioButtonALaw";
            this.radioButtonALaw.Size = new System.Drawing.Size(55, 17);
            this.radioButtonALaw.TabIndex = 4;
            this.radioButtonALaw.TabStop = true;
            this.radioButtonALaw.Text = "A-Law";
            this.radioButtonALaw.UseVisualStyleBackColor = true;
            // 
            // radioButtonMuLaw
            // 
            this.radioButtonMuLaw.AutoSize = true;
            this.radioButtonMuLaw.Location = new System.Drawing.Point(6, 19);
            this.radioButtonMuLaw.Name = "radioButtonMuLaw";
            this.radioButtonMuLaw.Size = new System.Drawing.Size(63, 17);
            this.radioButtonMuLaw.TabIndex = 4;
            this.radioButtonMuLaw.TabStop = true;
            this.radioButtonMuLaw.Text = "Mu-Law";
            this.radioButtonMuLaw.UseVisualStyleBackColor = true;
            // 
            // buttonChooseFormat
            // 
            this.buttonChooseFormat.Location = new System.Drawing.Point(256, 129);
            this.buttonChooseFormat.Name = "buttonChooseFormat";
            this.buttonChooseFormat.Size = new System.Drawing.Size(114, 23);
            this.buttonChooseFormat.TabIndex = 4;
            this.buttonChooseFormat.Text = "Choose Format...";
            this.buttonChooseFormat.UseVisualStyleBackColor = true;
            this.buttonChooseFormat.Click += new System.EventHandler(this.buttonChooseFormat_Click);
            // 
            // AcmForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(382, 325);
            this.Controls.Add(this.buttonChooseFormat);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBoxAcmDrivers);
            this.Name = "AcmForm";
            this.Text = "AcmForm";
            this.Load += new System.EventHandler(this.AcmForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxAcmDrivers;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonEncode;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButton5;
        private System.Windows.Forms.RadioButton radioButtonGsm610;
        private System.Windows.Forms.RadioButton radioButtonAdpcm;
        private System.Windows.Forms.RadioButton radioButtonALaw;
        private System.Windows.Forms.RadioButton radioButtonMuLaw;
        private System.Windows.Forms.CheckBox checkBoxAutoLaunchEncodedFile;
        private System.Windows.Forms.Button buttonChooseFormat;
    }
}