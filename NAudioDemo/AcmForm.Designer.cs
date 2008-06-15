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
            this.checkBoxAutoLaunchConvertedFile = new System.Windows.Forms.CheckBox();
            this.buttonChooseFormat = new System.Windows.Forms.Button();
            this.buttonDisplayFormatInfo = new System.Windows.Forms.Button();
            this.buttonDecode = new System.Windows.Forms.Button();
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
            this.buttonEncode.Location = new System.Drawing.Point(12, 173);
            this.buttonEncode.Name = "buttonEncode";
            this.buttonEncode.Size = new System.Drawing.Size(108, 23);
            this.buttonEncode.TabIndex = 2;
            this.buttonEncode.Text = "Encode";
            this.buttonEncode.UseVisualStyleBackColor = true;
            this.buttonEncode.Click += new System.EventHandler(this.buttonEncode_Click);
            // 
            // checkBoxAutoLaunchConvertedFile
            // 
            this.checkBoxAutoLaunchConvertedFile.AutoSize = true;
            this.checkBoxAutoLaunchConvertedFile.Location = new System.Drawing.Point(142, 177);
            this.checkBoxAutoLaunchConvertedFile.Name = "checkBoxAutoLaunchConvertedFile";
            this.checkBoxAutoLaunchConvertedFile.Size = new System.Drawing.Size(158, 17);
            this.checkBoxAutoLaunchConvertedFile.TabIndex = 5;
            this.checkBoxAutoLaunchConvertedFile.Text = "Auto-Launch Converted File";
            this.checkBoxAutoLaunchConvertedFile.UseVisualStyleBackColor = true;
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
            // buttonDisplayFormatInfo
            // 
            this.buttonDisplayFormatInfo.Location = new System.Drawing.Point(12, 129);
            this.buttonDisplayFormatInfo.Name = "buttonDisplayFormatInfo";
            this.buttonDisplayFormatInfo.Size = new System.Drawing.Size(165, 23);
            this.buttonDisplayFormatInfo.TabIndex = 6;
            this.buttonDisplayFormatInfo.Text = "Display Selected Format Info...";
            this.buttonDisplayFormatInfo.UseVisualStyleBackColor = true;
            this.buttonDisplayFormatInfo.Click += new System.EventHandler(this.buttonDisplayFormatInfo_Click);
            // 
            // buttonDecode
            // 
            this.buttonDecode.Location = new System.Drawing.Point(12, 202);
            this.buttonDecode.Name = "buttonDecode";
            this.buttonDecode.Size = new System.Drawing.Size(108, 23);
            this.buttonDecode.TabIndex = 2;
            this.buttonDecode.Text = "Decode";
            this.buttonDecode.UseVisualStyleBackColor = true;
            this.buttonDecode.Click += new System.EventHandler(this.buttonDecode_Click);
            // 
            // AcmForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(382, 325);
            this.Controls.Add(this.buttonDisplayFormatInfo);
            this.Controls.Add(this.checkBoxAutoLaunchConvertedFile);
            this.Controls.Add(this.buttonChooseFormat);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBoxAcmDrivers);
            this.Controls.Add(this.buttonDecode);
            this.Controls.Add(this.buttonEncode);
            this.Name = "AcmForm";
            this.Text = "AcmForm";
            this.Load += new System.EventHandler(this.AcmForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxAcmDrivers;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonEncode;
        private System.Windows.Forms.CheckBox checkBoxAutoLaunchConvertedFile;
        private System.Windows.Forms.Button buttonChooseFormat;
        private System.Windows.Forms.Button buttonDisplayFormatInfo;
        private System.Windows.Forms.Button buttonDecode;
    }
}