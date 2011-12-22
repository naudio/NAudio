namespace NAudioDemo.AudioPlaybackDemo
{
    partial class WasapiOutSettingsPanel
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
            this.comboBoxWaspai = new System.Windows.Forms.ComboBox();
            this.checkBoxWasapiEventCallback = new System.Windows.Forms.CheckBox();
            this.checkBoxWasapiExclusiveMode = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // comboBoxWaspai
            // 
            this.comboBoxWaspai.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxWaspai.FormattingEnabled = true;
            this.comboBoxWaspai.Location = new System.Drawing.Point(3, 3);
            this.comboBoxWaspai.Name = "comboBoxWaspai";
            this.comboBoxWaspai.Size = new System.Drawing.Size(232, 21);
            this.comboBoxWaspai.TabIndex = 20;
            // 
            // checkBoxWasapiEventCallback
            // 
            this.checkBoxWasapiEventCallback.AutoSize = true;
            this.checkBoxWasapiEventCallback.Location = new System.Drawing.Point(3, 30);
            this.checkBoxWasapiEventCallback.Name = "checkBoxWasapiEventCallback";
            this.checkBoxWasapiEventCallback.Size = new System.Drawing.Size(98, 17);
            this.checkBoxWasapiEventCallback.TabIndex = 19;
            this.checkBoxWasapiEventCallback.Text = "Event Callback";
            this.checkBoxWasapiEventCallback.UseVisualStyleBackColor = true;
            // 
            // checkBoxWasapiExclusiveMode
            // 
            this.checkBoxWasapiExclusiveMode.AutoSize = true;
            this.checkBoxWasapiExclusiveMode.Location = new System.Drawing.Point(134, 30);
            this.checkBoxWasapiExclusiveMode.Name = "checkBoxWasapiExclusiveMode";
            this.checkBoxWasapiExclusiveMode.Size = new System.Drawing.Size(101, 17);
            this.checkBoxWasapiExclusiveMode.TabIndex = 18;
            this.checkBoxWasapiExclusiveMode.Text = "Exclusive Mode";
            this.checkBoxWasapiExclusiveMode.UseVisualStyleBackColor = true;
            // 
            // WasapiOutSettingsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.comboBoxWaspai);
            this.Controls.Add(this.checkBoxWasapiEventCallback);
            this.Controls.Add(this.checkBoxWasapiExclusiveMode);
            this.Name = "WasapiOutSettingsPanel";
            this.Size = new System.Drawing.Size(245, 57);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxWaspai;
        private System.Windows.Forms.CheckBox checkBoxWasapiEventCallback;
        private System.Windows.Forms.CheckBox checkBoxWasapiExclusiveMode;
    }
}
