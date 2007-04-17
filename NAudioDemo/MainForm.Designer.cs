namespace NAudioDemo
{
    partial class MainForm
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
            this.buttonMidiIn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonMidiIn
            // 
            this.buttonMidiIn.Location = new System.Drawing.Point(12, 12);
            this.buttonMidiIn.Name = "buttonMidiIn";
            this.buttonMidiIn.Size = new System.Drawing.Size(75, 23);
            this.buttonMidiIn.TabIndex = 0;
            this.buttonMidiIn.Text = "MIDI In";
            this.buttonMidiIn.UseVisualStyleBackColor = true;
            this.buttonMidiIn.Click += new System.EventHandler(this.buttonMidiIn_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 264);
            this.Controls.Add(this.buttonMidiIn);
            this.Name = "MainForm";
            this.Text = "NAudio Demo";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonMidiIn;
    }
}

