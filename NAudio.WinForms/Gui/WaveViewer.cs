using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using NAudio.Wave;
using System.Drawing.Drawing2D;

namespace NAudio.Gui
{
    /// <summary>
    /// Control for viewing waveforms
    /// </summary>
    public class WaveViewer : System.Windows.Forms.UserControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private WaveStream waveStream;
        private int samplesPerPixel = 128;
        private long startPosition;
        private int bytesPerSample;
        /// <summary>
        /// Creates a new WaveViewer control
        /// </summary>
        public WaveViewer()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
            this.DoubleBuffered = true;			

        }

        /// <summary>
        /// sets the associated wavestream
        /// </summary>
        public WaveStream WaveStream
        {
            get
            {
                return waveStream;
            }
            set
            {
                waveStream = value;
                if (waveStream != null)
                {
                    bytesPerSample = (waveStream.WaveFormat.BitsPerSample / 8) * waveStream.WaveFormat.Channels;
                }
                this.Invalidate();
            }
        }

        /// <summary>
        /// The zoom level, in samples per pixel
        /// </summary>
        public int SamplesPerPixel
        {
            get
            {
                return samplesPerPixel;
            }
            set
            {
                samplesPerPixel = value;
                this.Invalidate();
            }
        }

        /// <summary>
        /// Start position (currently in bytes)
        /// </summary>
        public long StartPosition
        {
            get
            {
                return startPosition;
            }
            set
            {
                startPosition = value;
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        /// <summary>
        /// <see cref="Control.OnPaint"/>
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            if(waveStream != null)
            {
                waveStream.Position = 0;
                int bytesRead;
                byte[] waveData = new byte[samplesPerPixel*bytesPerSample];
                waveStream.Position = startPosition + (e.ClipRectangle.Left * bytesPerSample * samplesPerPixel);

                // This rectangle and the PointF array flips the coordinate system vertically.
                // It also properly scales the coordinate system so the amplitudes can be plotted directly.
                // 0 <= X <= this.Width
                // short.MinValue <= Y <= ushort.MaxValue / 2
                // The value "(float)ushort.MaxValue" is the total rectangle height. Adding it to the MinValue gives the Y max range "ushort.MaxValue / 2".
                RectangleF transformRectangle = new RectangleF(0, (float)short.MinValue, this.Width, (float)ushort.MaxValue);
                PointF[] pts =
                {
                        new PointF(0, this.Height),            // upper-left rectangle corner
                        new PointF(this.Width, this.Height),   // upper-right rectangle corner
                        new PointF(0, 0),                      // lower-left rectangle corner
                };

                // Assign the transformation.
                e.Graphics.Transform = new Matrix(transformRectangle, pts);

                for (float x = e.ClipRectangle.X; x < e.ClipRectangle.Right; x+=1)
                {
                    short low = 0;
                    short high = 0;
                    bytesRead = waveStream.Read(waveData, 0, samplesPerPixel * bytesPerSample);
                    if(bytesRead == 0)
                        break;
                    for(int n = 0; n < bytesRead; n+=2)
                    {
                        short sample = BitConverter.ToInt16(waveData, n);
                        if(sample < low) low = sample;
                        if(sample > high) high = sample;
                    }
                    e.Graphics.DrawLine(Pens.Black, x, low, x, high);
                } 
            }

            base.OnPaint (e);
        }


        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }
        #endregion
    }
}
