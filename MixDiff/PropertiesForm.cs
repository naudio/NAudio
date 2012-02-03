using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MarkHeath.AudioUtils
{
    public partial class PropertiesForm : Form
    {
        MixdownInfo mixdownInfo;

        public PropertiesForm(MixdownInfo mixdownInfo)
        {
            InitializeComponent();
            this.mixdownInfo = mixdownInfo;
            textBoxDelay.Text = mixdownInfo.DelayMilliseconds.ToString();
            textBoxOffset.Text = mixdownInfo.OffsetMilliseconds.ToString();
            trackBarVolume.Value = mixdownInfo.VolumeDecibels;
            trackBarVolume_Scroll(this, EventArgs.Empty);
        }

        private void trackBarVolume_Scroll(object sender, EventArgs e)
        {
            textBoxVolume.Text = String.Format("{0} dB", trackBarVolume.Value);
            mixdownInfo.VolumeDecibels = trackBarVolume.Value;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            int delay = 0;
            int offset = 0;
            bool parse = Int32.TryParse(textBoxDelay.Text,out delay);
            if(!parse || delay < 0)
            {
                MessageBox.Show("Please enter a valid number of milliseconds for the delay.");
                textBoxDelay.Focus();    
                return;
            }
            parse = Int32.TryParse(textBoxOffset.Text, out offset);
            if (!parse || offset < 0)
            {
                MessageBox.Show("Please enter a valid number of milliseconds to trim from the start.");
                textBoxOffset.Focus();    
                return;
            }
            mixdownInfo.DelayMilliseconds = delay;
            mixdownInfo.OffsetMilliseconds = offset;
            mixdownInfo.VolumeDecibels = trackBarVolume.Value;
            this.Close();
        }
    }
}