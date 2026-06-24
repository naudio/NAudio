namespace AudioFileInspector
{
    partial class OptionsForm
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
            this.buttonAssociate = new System.Windows.Forms.Button();
            this.buttonDisassociate = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonAssociate
            // 
            this.buttonAssociate.Location = new System.Drawing.Point(73, 64);
            this.buttonAssociate.Name = "buttonAssociate";
            this.buttonAssociate.Size = new System.Drawing.Size(164, 23);
            this.buttonAssociate.TabIndex = 0;
            this.buttonAssociate.Text = "Recreate File Associations";
            this.buttonAssociate.UseVisualStyleBackColor = true;
            this.buttonAssociate.Click += new System.EventHandler(this.buttonAssociate_Click);
            // 
            // buttonDisassociate
            // 
            this.buttonDisassociate.Location = new System.Drawing.Point(73, 93);
            this.buttonDisassociate.Name = "buttonDisassociate";
            this.buttonDisassociate.Size = new System.Drawing.Size(164, 23);
            this.buttonDisassociate.TabIndex = 1;
            this.buttonDisassociate.Text = "Remove File Associations";
            this.buttonDisassociate.UseVisualStyleBackColor = true;
            this.buttonDisassociate.Click += new System.EventHandler(this.buttonDisassociate_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(290, 52);
            this.label1.TabIndex = 2;
            this.label1.Text = "Audio File Inspector can be set up to appear as a right-click \r\ncontext menu opti" +
                "on in Windows explorer. You can recreate\r\nor remove the file associations at any" +
                " time using the buttons\r\nbelow.";
            // 
            // OptionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(309, 128);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonDisassociate);
            this.Controls.Add(this.buttonAssociate);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonAssociate;
        private System.Windows.Forms.Button buttonDisassociate;
        private System.Windows.Forms.Label label1;
    }
}