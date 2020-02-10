namespace MarkHeath.MidiUtils
{
    partial class AdvancedOptionsForm
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
            this.checkBoxRemoveSequencerSpecific = new System.Windows.Forms.CheckBox();
            this.checkBoxRecreateEndTrack = new System.Windows.Forms.CheckBox();
            this.checkBoxAddNameMarker = new System.Windows.Forms.CheckBox();
            this.checkBoxTrimTextEvents = new System.Windows.Forms.CheckBox();
            this.checkBoxRemoveEmptyTracks = new System.Windows.Forms.CheckBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.checkBoxRemoveExtraTempoEvents = new System.Windows.Forms.CheckBox();
            this.checkBoxRemoveExtraMarkers = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // checkBoxRemoveSequencerSpecific
            // 
            this.checkBoxRemoveSequencerSpecific.AutoSize = true;
            this.checkBoxRemoveSequencerSpecific.Location = new System.Drawing.Point(13, 13);
            this.checkBoxRemoveSequencerSpecific.Name = "checkBoxRemoveSequencerSpecific";
            this.checkBoxRemoveSequencerSpecific.Size = new System.Drawing.Size(198, 17);
            this.checkBoxRemoveSequencerSpecific.TabIndex = 0;
            this.checkBoxRemoveSequencerSpecific.Text = "Remove Sequencer Specific Events";
            this.checkBoxRemoveSequencerSpecific.UseVisualStyleBackColor = true;
            // 
            // checkBoxRecreateEndTrack
            // 
            this.checkBoxRecreateEndTrack.AutoSize = true;
            this.checkBoxRecreateEndTrack.Location = new System.Drawing.Point(13, 37);
            this.checkBoxRecreateEndTrack.Name = "checkBoxRecreateEndTrack";
            this.checkBoxRecreateEndTrack.Size = new System.Drawing.Size(159, 17);
            this.checkBoxRecreateEndTrack.TabIndex = 1;
            this.checkBoxRecreateEndTrack.Text = "Recreate End Track Events";
            this.checkBoxRecreateEndTrack.UseVisualStyleBackColor = true;
            // 
            // checkBoxAddNameMarker
            // 
            this.checkBoxAddNameMarker.AutoSize = true;
            this.checkBoxAddNameMarker.Location = new System.Drawing.Point(13, 61);
            this.checkBoxAddNameMarker.Name = "checkBoxAddNameMarker";
            this.checkBoxAddNameMarker.Size = new System.Drawing.Size(112, 17);
            this.checkBoxAddNameMarker.TabIndex = 2;
            this.checkBoxAddNameMarker.Text = "Add Name Marker";
            this.checkBoxAddNameMarker.UseVisualStyleBackColor = true;
            // 
            // checkBoxTrimTextEvents
            // 
            this.checkBoxTrimTextEvents.AutoSize = true;
            this.checkBoxTrimTextEvents.Location = new System.Drawing.Point(13, 85);
            this.checkBoxTrimTextEvents.Name = "checkBoxTrimTextEvents";
            this.checkBoxTrimTextEvents.Size = new System.Drawing.Size(106, 17);
            this.checkBoxTrimTextEvents.TabIndex = 3;
            this.checkBoxTrimTextEvents.Text = "Trim Text Events";
            this.checkBoxTrimTextEvents.UseVisualStyleBackColor = true;
            // 
            // checkBoxRemoveEmptyTracks
            // 
            this.checkBoxRemoveEmptyTracks.AutoSize = true;
            this.checkBoxRemoveEmptyTracks.Location = new System.Drawing.Point(13, 109);
            this.checkBoxRemoveEmptyTracks.Name = "checkBoxRemoveEmptyTracks";
            this.checkBoxRemoveEmptyTracks.Size = new System.Drawing.Size(134, 17);
            this.checkBoxRemoveEmptyTracks.TabIndex = 4;
            this.checkBoxRemoveEmptyTracks.Text = "Remove Empty Tracks";
            this.checkBoxRemoveEmptyTracks.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(60, 182);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 5;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(141, 182);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // checkBoxRemoveExtraTempoEvents
            // 
            this.checkBoxRemoveExtraTempoEvents.AutoSize = true;
            this.checkBoxRemoveExtraTempoEvents.Location = new System.Drawing.Point(13, 132);
            this.checkBoxRemoveExtraTempoEvents.Name = "checkBoxRemoveExtraTempoEvents";
            this.checkBoxRemoveExtraTempoEvents.Size = new System.Drawing.Size(165, 17);
            this.checkBoxRemoveExtraTempoEvents.TabIndex = 4;
            this.checkBoxRemoveExtraTempoEvents.Text = "Remove Extra Tempo Events";
            this.checkBoxRemoveExtraTempoEvents.UseVisualStyleBackColor = true;
            // 
            // checkBoxRemoveExtraMarkers
            // 
            this.checkBoxRemoveExtraMarkers.AutoSize = true;
            this.checkBoxRemoveExtraMarkers.Location = new System.Drawing.Point(13, 155);
            this.checkBoxRemoveExtraMarkers.Name = "checkBoxRemoveExtraMarkers";
            this.checkBoxRemoveExtraMarkers.Size = new System.Drawing.Size(134, 17);
            this.checkBoxRemoveExtraMarkers.TabIndex = 4;
            this.checkBoxRemoveExtraMarkers.Text = "Remove Extra Markers";
            this.checkBoxRemoveExtraMarkers.UseVisualStyleBackColor = true;
            // 
            // AdvancedOptionsForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(227, 217);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.checkBoxRemoveExtraMarkers);
            this.Controls.Add(this.checkBoxRemoveExtraTempoEvents);
            this.Controls.Add(this.checkBoxRemoveEmptyTracks);
            this.Controls.Add(this.checkBoxTrimTextEvents);
            this.Controls.Add(this.checkBoxAddNameMarker);
            this.Controls.Add(this.checkBoxRecreateEndTrack);
            this.Controls.Add(this.checkBoxRemoveSequencerSpecific);
            this.MinimumSize = new System.Drawing.Size(235, 210);
            this.Name = "AdvancedOptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Advanced Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxRemoveSequencerSpecific;
        private System.Windows.Forms.CheckBox checkBoxRecreateEndTrack;
        private System.Windows.Forms.CheckBox checkBoxAddNameMarker;
        private System.Windows.Forms.CheckBox checkBoxTrimTextEvents;
        private System.Windows.Forms.CheckBox checkBoxRemoveEmptyTracks;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.CheckBox checkBoxRemoveExtraTempoEvents;
        private System.Windows.Forms.CheckBox checkBoxRemoveExtraMarkers;
    }
}