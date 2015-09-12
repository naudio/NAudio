using System;
using System.Globalization;
using System.Windows.Forms;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudioDemo.SignalGeneratorDemo
{
    public partial class GeneratorPanel : UserControl
    {

        private readonly IWavePlayer driverOut;

        private SignalGenerator wg;

        // Frequency Max
        private const double FMax = 20000;

        // Frequency Min
        private const double FMin = 20;

        // constante  Math.Log10(FMax / FMin)
        private readonly double log10FmaxFMin;


        public GeneratorPanel()
        {
            // Const
            log10FmaxFMin = Math.Log10(FMax/FMin);

            // Panel Init
            InitializeComponent();
            Disposed += OnGeneratorPanelDisposed;

            // Init Audio
            driverOut = new WaveOutEvent();
            //driverOut = new AsioOut(0);
            wg = new SignalGenerator();

            // Par Default Frq 1200Hz
            cmbFrq.SelectedIndex = 0;
            cmbPrecisionFrq.SelectedIndex = 2;
            tbFrq.Value = 12; // 1200Hz
            CalculateTrackBarFrequency();

            // Par Default Frq End 2000Hz
            cmbFrqEnd.SelectedIndex = 0;
            cmbPrecisionFrqEnd.SelectedIndex = 2;
            tbFrqEnd.Value = tbFrqEnd.Maximum;
            CalculateTrackBarEndFrequency();

            // comboBox Type
            cmbType.DataSource = Enum.GetValues(typeof (SignalGeneratorType));
            cmbType.SelectedIndex = 0;

            // Par Default Gain -20dB
            tbGain.Value = -20;
            CalculateTrackBarToGain();

            // Par Default SweepSeconds
            tbSweepLength.Value = 10;
            CalculateTrackBarToSweepLength();

            // Init Driver Audio
            driverOut.Init(wg);
            StartStopEnabled();

        }

        private void OnGeneratorPanelDisposed(object sender, EventArgs e)
        {
            Cleanup();
        }

        // --------------
        // Start, Stop
        // --------------

        // btn Start
        private void OnButtonStartClick(object sender, EventArgs e)
        {
            if (driverOut != null)
            {
                driverOut.Play();
                StartStopEnabled();
            }
        }

        // btn Stop
        private void OnButtonStopClick(object sender, EventArgs e)
        {
            if (driverOut != null)
            {
                driverOut.Stop();
                StartStopEnabled();
            }
        }

        private void StartStopEnabled()
        {
            bool bDriverReady = (driverOut != null);

            btnStart.Enabled = bDriverReady && (driverOut.PlaybackState == PlaybackState.Stopped);
            btnStop.Enabled = bDriverReady && (driverOut.PlaybackState == PlaybackState.Playing);
            buttonSave.Enabled = btnStart.Enabled;
        }

        // --------------
        // Type Generator
        // --------------

        // cmb Type
        private void OnComboTypeSelectedIndexChanged(object sender, EventArgs e)
        {

            FrqEnabled(false);
            FrqEndEnabled(false);
            SweepLengthEnabled(false);

            switch ((SignalGeneratorType) cmbType.SelectedItem)
            {
                case SignalGeneratorType.Sin:
                case SignalGeneratorType.Square:
                case SignalGeneratorType.Triangle:
                case SignalGeneratorType.SawTooth:
                    FrqEnabled(true);
                    break;

                case SignalGeneratorType.Sweep:
                    FrqEnabled(true);
                    FrqEndEnabled(true);
                    SweepLengthEnabled(true);
                    break;
            }

            wg.Type = (SignalGeneratorType) cmbType.SelectedItem;
        }

        // --------------
        // Frequency
        // --------------

        // cmbFrq
        private void OnComboFrequencySelectedIndexChanged(object sender, EventArgs e)
        {
            CalculateTrackBarFrequency();
            FrqEnabled(true);
        }

        // trackbar Frq
        private void OnTrackBarFrequencyScroll(object sender, EventArgs e)
        {
            CalculateTrackBarFrequency();
        }

        // comboBox Precision Frq
        private void OnComboPrecisionFrequencySelectedIndexChanged(object sender, EventArgs e)
        {
            // change Type
            int octave = cmbPrecisionToOctave(cmbPrecisionFrq.SelectedIndex);

            // change tbFrq
            tbFrq.Maximum = octave;

            // 
            CalculateTrackBarFrequency();
        }

        private void CalculateTrackBarFrequency()
        {
            double x = Math.Pow(10, (tbFrq.Value/(tbFrq.Maximum/log10FmaxFMin)))*FMin;
            x = Math.Round(x, 1);


            // Change Frequency in Generator
            if (cmbFrq.SelectedIndex <= 0)
                wg.Frequency = x;
            else
                wg.Frequency = Convert.ToDouble(cmbFrq.SelectedItem);

            // View Frq
            lblFrq.Text = x.ToString(CultureInfo.InvariantCulture);
        }

        // Frq Enabled
        private void FrqEnabled(bool state)
        {
            grbFrq.Enabled = state;
            bool bFrqVariable = (cmbFrq.SelectedIndex <= 0);
            tbFrq.Enabled = bFrqVariable;
            cmbPrecisionFrq.Enabled = bFrqVariable;
            lblFrqPrecision.Enabled = bFrqVariable;
            lblFrq.Enabled = bFrqVariable;
            lblFrqUnit.Enabled = bFrqVariable;
            lblFrqTitle.Enabled = bFrqVariable;
        }

        // --------------
        // Frequency End
        // --------------

        // cmb Frq End
        private void OnComboFrequencyEndSelectedIndexChanged(object sender, EventArgs e)
        {
            CalculateTrackBarEndFrequency();
            FrqEndEnabled(true);
        }

        // trackbar FrqEnd
        private void OnTrackBarFrequencyEndScroll(object sender, EventArgs e)
        {
            CalculateTrackBarEndFrequency();
        }

        // combobox FrqEnd Precision
        private void OnComboPrecisionFrequencyEndSelectedIndexChanged(object sender, EventArgs e)
        {
            // change Type
            int octave = cmbPrecisionToOctave(cmbPrecisionFrqEnd.SelectedIndex);

            // change tbFrq
            tbFrqEnd.Maximum = octave;

            // 
            CalculateTrackBarEndFrequency();
        }

        private void CalculateTrackBarEndFrequency()
        {
            double x = Math.Pow(10, (tbFrqEnd.Value/(tbFrqEnd.Maximum/log10FmaxFMin)))*FMin;
            x = Math.Round(x, 1);

            // Change Frequency in Generator
            if (cmbFrqEnd.SelectedIndex <= 0)
                wg.FrequencyEnd = x;
            else
                wg.FrequencyEnd = Convert.ToDouble(cmbFrqEnd.SelectedItem);


            // View Frq
            lblFrqEnd.Text = x.ToString(CultureInfo.InvariantCulture);
        }

        // FrqEnd Enabled
        private void FrqEndEnabled(bool state)
        {
            grpFrqEnd.Enabled = state;
            bool bFrqEndVariable = (cmbFrqEnd.SelectedIndex <= 0);
            tbFrqEnd.Enabled = bFrqEndVariable;
            cmbPrecisionFrqEnd.Enabled = bFrqEndVariable;
            lblFrqEndPrecision.Enabled = bFrqEndVariable;
            lblFrqEnd.Enabled = bFrqEndVariable;
            lblFrqEndUnit.Enabled = bFrqEndVariable;
            lblFrqEndTitle.Enabled = bFrqEndVariable;
        }

        // --------------
        // Gain 
        // --------------

        // trackbar Gain
        private void OnTrackBarGainScroll(object sender, EventArgs e)
        {
            CalculateTrackBarToGain();
        }

        private void CalculateTrackBarToGain()
        {
            lblGain.Text = tbGain.Value.ToString();
            wg.Gain = Decibels.DecibelsToLinear(tbGain.Value);
        }

        // --------------
        // Sweep Length
        // --------------

        // trackbar Sweep Length
        private void OnTrackBarSweepLengthScroll(object sender, EventArgs e)
        {
            CalculateTrackBarToSweepLength();
        }

        private void CalculateTrackBarToSweepLength()
        {
            lblSweepLength.Text = tbSweepLength.Value.ToString();
            wg.SweepLengthSecs = tbSweepLength.Value;

        }

        // Sweep Length Enabled
        private void SweepLengthEnabled(bool state)
        {
            grpSweepLength.Enabled = state;
        }

        // --------------
        // Phase Reverse
        // --------------

        // Reverse Left
        private void OnReverseLeftCheckedChanged(object sender, EventArgs e)
        {
            PhaseReverse();
        }

        // Reverse Right
        private void OnReverseRightCheckedChanged(object sender, EventArgs e)
        {
            PhaseReverse();
        }

        // Apply PhaseReverse
        private void PhaseReverse()
        {
            wg.PhaseReverse[0] = chkReverseLeft.Checked;
            wg.PhaseReverse[1] = chkReverseRight.Checked;
        }

        // --------------
        // Other
        // --------------

        // Nb of Frequency
        private int cmbPrecisionToOctave(int idx)
        {
            return (int) (10.35f*idx);
        }

        // Clean DriverOut
        private void Cleanup()
        {
            if (driverOut != null)
                driverOut.Stop();

            wg = null;

            if (driverOut != null)
            {
                driverOut.Dispose();
            }
        }

        private void OnButtonSaveClick(object sender, EventArgs e)
        {
            OnButtonStopClick(this,e);
            var sfd = new SaveFileDialog();
            sfd.Filter = "WAV File|*.wav";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var osp = new OffsetSampleProvider(wg);
                osp.TakeSamples = wg.WaveFormat.SampleRate*20*wg.WaveFormat.Channels;
                WaveFileWriter.CreateWaveFile16(sfd.FileName, osp);
            }
        }

    }
}