using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NAudio.Gui
{
    public partial class WaveformPainter : Control
    {
        Pen foregroundPen;
        List<float> samples = new List<float>(1000);
        int maxSamples;
        int insertPos;

        public WaveformPainter()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            InitializeComponent();
            OnForeColorChanged(EventArgs.Empty);
            OnResize(EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            maxSamples = this.Width;
            base.OnResize(e);
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            foregroundPen = new Pen(ForeColor);
            base.OnForeColorChanged(e);
        }

        public void AddMax(float maxSample)
        {
            if (samples.Count <= maxSamples)
            {
                samples.Add(maxSample);
            }
            else
            {
                samples[insertPos] = maxSample;
            }
            insertPos++;
            insertPos %= maxSamples;
            
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            for (int x = 0; x < this.Width; x++)
            {                
                float lineHeight = this.Height * GetSample(x - this.Width + insertPos);
                float y1 = (this.Height - lineHeight) / 2;
                pe.Graphics.DrawLine(foregroundPen, x, y1, x, y1 + lineHeight);
            }
        }

        float GetSample(int index)
        {
            if (index < 0)
                index += maxSamples;
            if (index >= 0 & index < samples.Count)
                return samples[index];
            return 0;
        }
    }
}
