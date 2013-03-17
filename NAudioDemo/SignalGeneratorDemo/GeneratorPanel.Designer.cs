namespace NAudioDemo.Generator
{
	partial class GeneratorPanel
	{
		/// <summary> 
		/// Variable nécessaire au concepteur.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Nettoyage des ressources utilisées.
		/// </summary>
		/// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Code généré par le Concepteur de composants

		/// <summary> 
		/// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
		/// le contenu de cette méthode avec l'éditeur de code.
		/// </summary>
		private void InitializeComponent()
		{
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tbFrq = new System.Windows.Forms.TrackBar();
            this.lblFrq = new System.Windows.Forms.Label();
            this.lblFrqTitle = new System.Windows.Forms.Label();
            this.lblFrqEndTitle = new System.Windows.Forms.Label();
            this.tbFrqEnd = new System.Windows.Forms.TrackBar();
            this.lblFrqEnd = new System.Windows.Forms.Label();
            this.lblFrqEndUnit = new System.Windows.Forms.Label();
            this.tbGain = new System.Windows.Forms.TrackBar();
            this.lblGain = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.tbSweepLength = new System.Windows.Forms.TrackBar();
            this.lblSweepLength = new System.Windows.Forms.Label();
            this.lblSweepLengthUnit = new System.Windows.Forms.Label();
            this.chkReverseLeft = new System.Windows.Forms.CheckBox();
            this.chkReverseRight = new System.Windows.Forms.CheckBox();
            this.cmbPrecisionFrq = new System.Windows.Forms.ComboBox();
            this.lblFrqPrecision = new System.Windows.Forms.Label();
            this.cmbPrecisionFrqEnd = new System.Windows.Forms.ComboBox();
            this.lblFrqEndPrecision = new System.Windows.Forms.Label();
            this.lblFrqUnit = new System.Windows.Forms.Label();
            this.cmbFrq = new System.Windows.Forms.ComboBox();
            this.grbFrq = new System.Windows.Forms.GroupBox();
            this.lblPreset = new System.Windows.Forms.Label();
            this.grpFrqEnd = new System.Windows.Forms.GroupBox();
            this.cmbFrqEnd = new System.Windows.Forms.ComboBox();
            this.lblEndPreset = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.grpSweepLength = new System.Windows.Forms.GroupBox();
            this.grpPhaseReverse = new System.Windows.Forms.GroupBox();
            this.buttonSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.tbFrq)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbFrqEnd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbGain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSweepLength)).BeginInit();
            this.grbFrq.SuspendLayout();
            this.grpFrqEnd.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.grpSweepLength.SuspendLayout();
            this.grpPhaseReverse.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(15, 17);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(65, 27);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(86, 17);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(65, 27);
            this.btnStop.TabIndex = 0;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // cmbType
            // 
            this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbType.FormattingEnabled = true;
            this.cmbType.Location = new System.Drawing.Point(330, 24);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(191, 21);
            this.cmbType.TabIndex = 1;
            this.cmbType.SelectedIndexChanged += new System.EventHandler(this.cmbType_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(277, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Type";
            // 
            // tbFrq
            // 
            this.tbFrq.Location = new System.Drawing.Point(6, 70);
            this.tbFrq.Margin = new System.Windows.Forms.Padding(0);
            this.tbFrq.Maximum = 272;
            this.tbFrq.Name = "tbFrq";
            this.tbFrq.Size = new System.Drawing.Size(272, 45);
            this.tbFrq.TabIndex = 3;
            this.tbFrq.TickStyle = System.Windows.Forms.TickStyle.None;
            this.tbFrq.Scroll += new System.EventHandler(this.tbFrq_Scroll);
            // 
            // lblFrq
            // 
            this.lblFrq.AutoSize = true;
            this.lblFrq.Location = new System.Drawing.Point(292, 72);
            this.lblFrq.Name = "lblFrq";
            this.lblFrq.Size = new System.Drawing.Size(37, 13);
            this.lblFrq.TabIndex = 4;
            this.lblFrq.Text = "00000";
            // 
            // lblFrqTitle
            // 
            this.lblFrqTitle.AutoSize = true;
            this.lblFrqTitle.Location = new System.Drawing.Point(10, 54);
            this.lblFrqTitle.Name = "lblFrqTitle";
            this.lblFrqTitle.Size = new System.Drawing.Size(57, 13);
            this.lblFrqTitle.TabIndex = 2;
            this.lblFrqTitle.Text = "Frequency";
            // 
            // lblFrqEndTitle
            // 
            this.lblFrqEndTitle.AutoSize = true;
            this.lblFrqEndTitle.Location = new System.Drawing.Point(6, 48);
            this.lblFrqEndTitle.Name = "lblFrqEndTitle";
            this.lblFrqEndTitle.Size = new System.Drawing.Size(57, 13);
            this.lblFrqEndTitle.TabIndex = 2;
            this.lblFrqEndTitle.Text = "Frequency";
            // 
            // tbFrqEnd
            // 
            this.tbFrqEnd.Location = new System.Drawing.Point(2, 64);
            this.tbFrqEnd.Maximum = 1000;
            this.tbFrqEnd.Name = "tbFrqEnd";
            this.tbFrqEnd.Size = new System.Drawing.Size(272, 45);
            this.tbFrqEnd.TabIndex = 3;
            this.tbFrqEnd.TickStyle = System.Windows.Forms.TickStyle.None;
            this.tbFrqEnd.Scroll += new System.EventHandler(this.tbFrqEnd_Scroll);
            // 
            // lblFrqEnd
            // 
            this.lblFrqEnd.AutoSize = true;
            this.lblFrqEnd.Location = new System.Drawing.Point(288, 66);
            this.lblFrqEnd.Name = "lblFrqEnd";
            this.lblFrqEnd.Size = new System.Drawing.Size(37, 13);
            this.lblFrqEnd.TabIndex = 4;
            this.lblFrqEnd.Text = "00000";
            // 
            // lblFrqEndUnit
            // 
            this.lblFrqEndUnit.AutoSize = true;
            this.lblFrqEndUnit.Location = new System.Drawing.Point(331, 66);
            this.lblFrqEndUnit.Name = "lblFrqEndUnit";
            this.lblFrqEndUnit.Size = new System.Drawing.Size(20, 13);
            this.lblFrqEndUnit.TabIndex = 4;
            this.lblFrqEndUnit.Text = "Hz";
            // 
            // tbGain
            // 
            this.tbGain.Location = new System.Drawing.Point(2, 40);
            this.tbGain.Maximum = 0;
            this.tbGain.Minimum = -100;
            this.tbGain.Name = "tbGain";
            this.tbGain.Size = new System.Drawing.Size(216, 45);
            this.tbGain.TabIndex = 3;
            this.tbGain.TickStyle = System.Windows.Forms.TickStyle.None;
            this.tbGain.Value = -50;
            this.tbGain.Scroll += new System.EventHandler(this.tbGain_Scroll);
            // 
            // lblGain
            // 
            this.lblGain.AutoSize = true;
            this.lblGain.Location = new System.Drawing.Point(224, 40);
            this.lblGain.Name = "lblGain";
            this.lblGain.Size = new System.Drawing.Size(37, 13);
            this.lblGain.TabIndex = 4;
            this.lblGain.Text = "00000";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(267, 40);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(20, 13);
            this.label8.TabIndex = 4;
            this.label8.Text = "dB";
            // 
            // tbSweepLength
            // 
            this.tbSweepLength.Location = new System.Drawing.Point(6, 19);
            this.tbSweepLength.Maximum = 30;
            this.tbSweepLength.Minimum = 1;
            this.tbSweepLength.Name = "tbSweepLength";
            this.tbSweepLength.Size = new System.Drawing.Size(212, 45);
            this.tbSweepLength.TabIndex = 3;
            this.tbSweepLength.TickStyle = System.Windows.Forms.TickStyle.None;
            this.tbSweepLength.Value = 10;
            this.tbSweepLength.Scroll += new System.EventHandler(this.tbSweepLength_Scroll);
            // 
            // lblSweepLength
            // 
            this.lblSweepLength.AutoSize = true;
            this.lblSweepLength.Location = new System.Drawing.Point(224, 16);
            this.lblSweepLength.Name = "lblSweepLength";
            this.lblSweepLength.Size = new System.Drawing.Size(37, 13);
            this.lblSweepLength.TabIndex = 4;
            this.lblSweepLength.Text = "00000";
            // 
            // lblSweepLengthUnit
            // 
            this.lblSweepLengthUnit.AutoSize = true;
            this.lblSweepLengthUnit.Location = new System.Drawing.Point(267, 16);
            this.lblSweepLengthUnit.Name = "lblSweepLengthUnit";
            this.lblSweepLengthUnit.Size = new System.Drawing.Size(12, 13);
            this.lblSweepLengthUnit.TabIndex = 4;
            this.lblSweepLengthUnit.Text = "s";
            // 
            // chkReverseLeft
            // 
            this.chkReverseLeft.AutoSize = true;
            this.chkReverseLeft.Location = new System.Drawing.Point(108, 19);
            this.chkReverseLeft.Name = "chkReverseLeft";
            this.chkReverseLeft.Size = new System.Drawing.Size(44, 17);
            this.chkReverseLeft.TabIndex = 5;
            this.chkReverseLeft.Text = "Left";
            this.chkReverseLeft.UseVisualStyleBackColor = true;
            this.chkReverseLeft.CheckedChanged += new System.EventHandler(this.chkReverseLeft_CheckedChanged);
            // 
            // chkReverseRight
            // 
            this.chkReverseRight.AutoSize = true;
            this.chkReverseRight.Location = new System.Drawing.Point(167, 19);
            this.chkReverseRight.Name = "chkReverseRight";
            this.chkReverseRight.Size = new System.Drawing.Size(51, 17);
            this.chkReverseRight.TabIndex = 5;
            this.chkReverseRight.Text = "Right";
            this.chkReverseRight.UseVisualStyleBackColor = true;
            this.chkReverseRight.CheckedChanged += new System.EventHandler(this.chkReverseRight_CheckedChanged);
            // 
            // cmbPrecisionFrq
            // 
            this.cmbPrecisionFrq.FormattingEnabled = true;
            this.cmbPrecisionFrq.Items.AddRange(new object[] {
            "Octave",
            "1/2 Octave",
            "1/3 Octave",
            "1/6 Octave",
            "1/12 Octave",
            "1/24 Octave",
            "1/48 Octave"});
            this.cmbPrecisionFrq.Location = new System.Drawing.Point(159, 51);
            this.cmbPrecisionFrq.Name = "cmbPrecisionFrq";
            this.cmbPrecisionFrq.Size = new System.Drawing.Size(108, 21);
            this.cmbPrecisionFrq.TabIndex = 1;
            this.cmbPrecisionFrq.SelectedIndexChanged += new System.EventHandler(this.cmbPrecisionFrq_SelectedIndexChanged);
            // 
            // lblFrqPrecision
            // 
            this.lblFrqPrecision.AutoSize = true;
            this.lblFrqPrecision.Location = new System.Drawing.Point(96, 54);
            this.lblFrqPrecision.Name = "lblFrqPrecision";
            this.lblFrqPrecision.Size = new System.Drawing.Size(50, 13);
            this.lblFrqPrecision.TabIndex = 2;
            this.lblFrqPrecision.Text = "Precision";
            // 
            // cmbPrecisionFrqEnd
            // 
            this.cmbPrecisionFrqEnd.FormattingEnabled = true;
            this.cmbPrecisionFrqEnd.Items.AddRange(new object[] {
            "Octave",
            "1/2 Octave",
            "1/3 Octave",
            "1/6 Octave",
            "1/12 Octave",
            "1/24 Octave",
            "1/48 Octave"});
            this.cmbPrecisionFrqEnd.Location = new System.Drawing.Point(155, 45);
            this.cmbPrecisionFrqEnd.Name = "cmbPrecisionFrqEnd";
            this.cmbPrecisionFrqEnd.Size = new System.Drawing.Size(108, 21);
            this.cmbPrecisionFrqEnd.TabIndex = 1;
            this.cmbPrecisionFrqEnd.SelectedIndexChanged += new System.EventHandler(this.cmbPrecisionFrqEnd_SelectedIndexChanged);
            // 
            // lblFrqEndPrecision
            // 
            this.lblFrqEndPrecision.AutoSize = true;
            this.lblFrqEndPrecision.Location = new System.Drawing.Point(91, 48);
            this.lblFrqEndPrecision.Name = "lblFrqEndPrecision";
            this.lblFrqEndPrecision.Size = new System.Drawing.Size(50, 13);
            this.lblFrqEndPrecision.TabIndex = 2;
            this.lblFrqEndPrecision.Text = "Precision";
            // 
            // lblFrqUnit
            // 
            this.lblFrqUnit.AutoSize = true;
            this.lblFrqUnit.Location = new System.Drawing.Point(335, 72);
            this.lblFrqUnit.Name = "lblFrqUnit";
            this.lblFrqUnit.Size = new System.Drawing.Size(20, 13);
            this.lblFrqUnit.TabIndex = 4;
            this.lblFrqUnit.Text = "Hz";
            // 
            // cmbFrq
            // 
            this.cmbFrq.FormattingEnabled = true;
            this.cmbFrq.Items.AddRange(new object[] {
            "Variable",
            "16",
            "20",
            "25",
            "31.5",
            "40",
            "50",
            "63",
            "80",
            "100",
            "125",
            "160",
            "200",
            "250",
            "315",
            "400",
            "440",
            "500",
            "630",
            "800",
            "1000",
            "1250",
            "1600",
            "2000",
            "2500",
            "3150",
            "4000",
            "5000",
            "6300",
            "8000",
            "9600",
            "10000",
            "11000",
            "16000",
            "20000"});
            this.cmbFrq.Location = new System.Drawing.Point(77, 22);
            this.cmbFrq.Name = "cmbFrq";
            this.cmbFrq.Size = new System.Drawing.Size(108, 21);
            this.cmbFrq.TabIndex = 1;
            this.cmbFrq.SelectedIndexChanged += new System.EventHandler(this.cmbFrq_SelectedIndexChanged);
            // 
            // grbFrq
            // 
            this.grbFrq.Controls.Add(this.cmbFrq);
            this.grbFrq.Controls.Add(this.tbFrq);
            this.grbFrq.Controls.Add(this.cmbPrecisionFrq);
            this.grbFrq.Controls.Add(this.lblFrqUnit);
            this.grbFrq.Controls.Add(this.lblFrqTitle);
            this.grbFrq.Controls.Add(this.lblPreset);
            this.grbFrq.Controls.Add(this.lblFrqPrecision);
            this.grbFrq.Controls.Add(this.lblFrq);
            this.grbFrq.Location = new System.Drawing.Point(5, 51);
            this.grbFrq.Name = "grbFrq";
            this.grbFrq.Size = new System.Drawing.Size(355, 118);
            this.grbFrq.TabIndex = 6;
            this.grbFrq.TabStop = false;
            this.grbFrq.Text = "Start Frequency";
            // 
            // lblPreset
            // 
            this.lblPreset.AutoSize = true;
            this.lblPreset.Location = new System.Drawing.Point(14, 25);
            this.lblPreset.Name = "lblPreset";
            this.lblPreset.Size = new System.Drawing.Size(37, 13);
            this.lblPreset.TabIndex = 2;
            this.lblPreset.Text = "Preset";
            // 
            // grpFrqEnd
            // 
            this.grpFrqEnd.Controls.Add(this.cmbFrqEnd);
            this.grpFrqEnd.Controls.Add(this.lblEndPreset);
            this.grpFrqEnd.Controls.Add(this.lblFrqEndTitle);
            this.grpFrqEnd.Controls.Add(this.cmbPrecisionFrqEnd);
            this.grpFrqEnd.Controls.Add(this.lblFrqEndPrecision);
            this.grpFrqEnd.Controls.Add(this.tbFrqEnd);
            this.grpFrqEnd.Controls.Add(this.lblFrqEndUnit);
            this.grpFrqEnd.Controls.Add(this.lblFrqEnd);
            this.grpFrqEnd.Location = new System.Drawing.Point(5, 175);
            this.grpFrqEnd.Name = "grpFrqEnd";
            this.grpFrqEnd.Size = new System.Drawing.Size(355, 111);
            this.grpFrqEnd.TabIndex = 7;
            this.grpFrqEnd.TabStop = false;
            this.grpFrqEnd.Text = "End Frequency";
            // 
            // cmbFrqEnd
            // 
            this.cmbFrqEnd.FormattingEnabled = true;
            this.cmbFrqEnd.Items.AddRange(new object[] {
            "Variable",
            "16",
            "20",
            "25",
            "31.5",
            "40",
            "50",
            "63",
            "80",
            "100",
            "125",
            "160",
            "200",
            "250",
            "315",
            "400",
            "440",
            "500",
            "630",
            "800",
            "1000",
            "1250",
            "1600",
            "2000",
            "2500",
            "3150",
            "4000",
            "5000",
            "6300",
            "8000",
            "9600",
            "10000",
            "11000",
            "16000",
            "20000"});
            this.cmbFrqEnd.Location = new System.Drawing.Point(74, 19);
            this.cmbFrqEnd.Name = "cmbFrqEnd";
            this.cmbFrqEnd.Size = new System.Drawing.Size(108, 21);
            this.cmbFrqEnd.TabIndex = 5;
            this.cmbFrqEnd.SelectedIndexChanged += new System.EventHandler(this.cmbFrqEnd_SelectedIndexChanged);
            // 
            // lblEndPreset
            // 
            this.lblEndPreset.AutoSize = true;
            this.lblEndPreset.Location = new System.Drawing.Point(11, 22);
            this.lblEndPreset.Name = "lblEndPreset";
            this.lblEndPreset.Size = new System.Drawing.Size(37, 13);
            this.lblEndPreset.TabIndex = 6;
            this.lblEndPreset.Text = "Preset";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tbGain);
            this.groupBox1.Controls.Add(this.lblGain);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Location = new System.Drawing.Point(366, 51);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(287, 90);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Gain";
            // 
            // grpSweepLength
            // 
            this.grpSweepLength.Controls.Add(this.tbSweepLength);
            this.grpSweepLength.Controls.Add(this.lblSweepLength);
            this.grpSweepLength.Controls.Add(this.lblSweepLengthUnit);
            this.grpSweepLength.Location = new System.Drawing.Point(366, 147);
            this.grpSweepLength.Name = "grpSweepLength";
            this.grpSweepLength.Size = new System.Drawing.Size(287, 80);
            this.grpSweepLength.TabIndex = 9;
            this.grpSweepLength.TabStop = false;
            this.grpSweepLength.Text = "Sweep Length (seconds)";
            // 
            // grpPhaseReverse
            // 
            this.grpPhaseReverse.Controls.Add(this.chkReverseLeft);
            this.grpPhaseReverse.Controls.Add(this.chkReverseRight);
            this.grpPhaseReverse.Location = new System.Drawing.Point(366, 233);
            this.grpPhaseReverse.Name = "grpPhaseReverse";
            this.grpPhaseReverse.Size = new System.Drawing.Size(287, 51);
            this.grpPhaseReverse.TabIndex = 10;
            this.grpPhaseReverse.TabStop = false;
            this.grpPhaseReverse.Text = "PhaseReverse";
            // 
            // buttonSave
            // 
            this.buttonSave.Location = new System.Drawing.Point(157, 17);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(65, 27);
            this.buttonSave.TabIndex = 0;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // GeneratorPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(674, 296);
            this.Controls.Add(this.grpPhaseReverse);
            this.Controls.Add(this.grpSweepLength);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.grpFrqEnd);
            this.Controls.Add(this.grbFrq);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbType);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Name = "GeneratorPanel";
            this.Size = new System.Drawing.Size(675, 298);
            ((System.ComponentModel.ISupportInitialize)(this.tbFrq)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbFrqEnd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbGain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSweepLength)).EndInit();
            this.grbFrq.ResumeLayout(false);
            this.grbFrq.PerformLayout();
            this.grpFrqEnd.ResumeLayout(false);
            this.grpFrqEnd.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.grpSweepLength.ResumeLayout(false);
            this.grpSweepLength.PerformLayout();
            this.grpPhaseReverse.ResumeLayout(false);
            this.grpPhaseReverse.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnStart;
		private System.Windows.Forms.Button btnStop;
		private System.Windows.Forms.ComboBox cmbType;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TrackBar tbFrq;
		private System.Windows.Forms.Label lblFrq;
		private System.Windows.Forms.Label lblFrqTitle;
		private System.Windows.Forms.Label lblFrqEndTitle;
		private System.Windows.Forms.TrackBar tbFrqEnd;
		private System.Windows.Forms.Label lblFrqEnd;
		private System.Windows.Forms.Label lblFrqEndUnit;
		private System.Windows.Forms.TrackBar tbGain;
		private System.Windows.Forms.Label lblGain;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TrackBar tbSweepLength;
		private System.Windows.Forms.Label lblSweepLength;
		private System.Windows.Forms.Label lblSweepLengthUnit;
		private System.Windows.Forms.CheckBox chkReverseLeft;
		private System.Windows.Forms.CheckBox chkReverseRight;
		private System.Windows.Forms.ComboBox cmbPrecisionFrq;
		private System.Windows.Forms.Label lblFrqPrecision;
		private System.Windows.Forms.ComboBox cmbPrecisionFrqEnd;
		private System.Windows.Forms.Label lblFrqEndPrecision;
		private System.Windows.Forms.Label lblFrqUnit;
		private System.Windows.Forms.ComboBox cmbFrq;
		private System.Windows.Forms.GroupBox grbFrq;
		private System.Windows.Forms.Label lblPreset;
		private System.Windows.Forms.GroupBox grpFrqEnd;
		private System.Windows.Forms.ComboBox cmbFrqEnd;
		private System.Windows.Forms.Label lblEndPreset;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox grpSweepLength;
		private System.Windows.Forms.GroupBox grpPhaseReverse;
        private System.Windows.Forms.Button buttonSave;
	}
}
