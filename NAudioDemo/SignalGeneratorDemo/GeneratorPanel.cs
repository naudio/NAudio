using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Windows.Forms;
using NAudio.Wave.SampleProviders;

namespace NAudioDemo.Generator
{
    public partial class GeneratorPanel : UserControl
    {

        private WaveOut driverOut;

        private SignalGenerator wg;

        // Frequency Max
        private const double FMax = 20000;

        // Frequency Min
        private const double FMin = 20;

        // constante  Math.Log10(FMax / FMin)
        private double Log10FmaxFMin;


        public GeneratorPanel()
        {
            // Const
            Log10FmaxFMin = Math.Log10(FMax/FMin);

            // Panel Init
            InitializeComponent();
            this.Disposed += new EventHandler(GeneratorPanel_Disposed);

            // Init Audio
            driverOut = new WaveOut();
            driverOut.DesiredLatency = 100;
            //driverOut = new AsioOut(0);
            wg = new SignalGenerator();

            // Par Default Frq 1200Hz
            cmbFrq.SelectedIndex = 0;
            cmbPrecisionFrq.SelectedIndex = 2;
            tbFrq.Value = 12; // 1200Hz
            tbToFrq();

            // Par Default Frq End 2000Hz
            cmbFrqEnd.SelectedIndex = 0;
            cmbPrecisionFrqEnd.SelectedIndex = 2;
            tbFrqEnd.Value = tbFrqEnd.Maximum;
            tbToFrqEnd();

            // comboBox Type
            cmbType.DataSource = Enum.GetValues(typeof (SignalGeneratorType));
            cmbType.SelectedIndex = 0;

            // Par Default Gain -20dB
            tbGain.Value = -20;
            tbToGain();

            // Par Default SweepSeconds
            tbSweepLength.Value = 10;
            tbToSweepLength();

            // Init Driver Audio
            driverOut.Init(wg);
            StartStopEnabled();

        }

        private void GeneratorPanel_Disposed(object sender, EventArgs e)
        {
            Cleanup();
        }

        // --------------
        // Start, Stop
        // --------------

        // btn Start
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (driverOut != null)
            {
                driverOut.Play();
                StartStopEnabled();
            }
        }

        // btn Stop
        private void btnStop_Click(object sender, EventArgs e)
        {
            if (driverOut != null)
            {
                driverOut.Stop();
                StartStopEnabled();
            }


        }

        // Bouton Enabled
        private void StartStopEnabled()
        {
            bool bDriverReady = (driverOut != null);

            btnStart.Enabled = bDriverReady & (driverOut.PlaybackState == PlaybackState.Stopped);
            btnStop.Enabled = bDriverReady & (driverOut.PlaybackState == PlaybackState.Playing);
            buttonSave.Enabled = btnStart.Enabled;

        }

        // --------------
        // Type Generator
        // --------------

        // cmb Type
        private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
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

                default:
                    break;
            }

            wg.Type = (SignalGeneratorType) cmbType.SelectedItem;
        }

        // --------------
        // Frequency
        // --------------

        // cmbFrq
        private void cmbFrq_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbToFrq();
            FrqEnabled(true);
        }

        // trackbar Frq
        private void tbFrq_Scroll(object sender, EventArgs e)
        {
            tbToFrq();
        }

        // comboBox Precision Frq
        private void cmbPrecisionFrq_SelectedIndexChanged(object sender, EventArgs e)
        {
            // change Type
            int octave = cmbPrecisionToOctave(cmbPrecisionFrq.SelectedIndex);

            // change tbFrq
            tbFrq.Maximum = octave;

            // 
            tbToFrq();
        }


        // Calcul TaskBar to Frq
        private void tbToFrq()
        {
            double x = Math.Pow(10, (tbFrq.Value/(tbFrq.Maximum/Log10FmaxFMin)))*FMin;
            x = Math.Round(x, 1);


            // Change Frequency in Generator
            if (cmbFrq.SelectedIndex <= 0)
                wg.Frequency = x;
            else
                wg.Frequency = Convert.ToDouble(cmbFrq.SelectedItem);

            // View Frq
            lblFrq.Text = x.ToString();
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
        private void cmbFrqEnd_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbToFrqEnd();
            FrqEndEnabled(true);
        }

        // trackbar FrqEnd
        private void tbFrqEnd_Scroll(object sender, EventArgs e)
        {
            tbToFrqEnd();
        }

        // combobox FrqEnd Precision
        private void cmbPrecisionFrqEnd_SelectedIndexChanged(object sender, EventArgs e)
        {
            // change Type
            int octave = cmbPrecisionToOctave(cmbPrecisionFrqEnd.SelectedIndex);

            // change tbFrq
            tbFrqEnd.Maximum = octave;

            // 
            tbToFrqEnd();
        }

        // Calcul TaskBar to Frq
        private void tbToFrqEnd()
        {
            double x = Math.Pow(10, (tbFrqEnd.Value/(tbFrqEnd.Maximum/Log10FmaxFMin)))*FMin;
            x = Math.Round(x, 1);

            // Change Frequency in Generator
            if (cmbFrqEnd.SelectedIndex <= 0)
                wg.FrequencyEnd = x;
            else
                wg.FrequencyEnd = Convert.ToDouble(cmbFrqEnd.SelectedItem);


            // View Frq
            lblFrqEnd.Text = x.ToString();
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
        private void tbGain_Scroll(object sender, EventArgs e)
        {
            tbToGain();
        }

        // Calcul TaskBar to Gain
        private void tbToGain()
        {
            lblGain.Text = tbGain.Value.ToString();
            wg.Gain = Decibels.DecibelsToLinear(tbGain.Value);
        }

        // --------------
        // Sweep Length
        // --------------

        // trackbar Sweep Length
        private void tbSweepLength_Scroll(object sender, EventArgs e)
        {
            tbToSweepLength();
        }

        // Calcul TaskBar to Length
        private void tbToSweepLength()
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
        private void chkReverseLeft_CheckedChanged(object sender, EventArgs e)
        {
            PhaseReverse();
        }

        // Reverse Right
        private void chkReverseRight_CheckedChanged(object sender, EventArgs e)
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

            if (wg != null)
                wg = null;

            if (driverOut != null)
            {
                driverOut.Dispose();
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            btnStop_Click(this,e);
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