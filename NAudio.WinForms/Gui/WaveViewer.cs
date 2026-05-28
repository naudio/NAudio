using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudio.Gui
{
    /// <summary>
    /// Control for viewing waveforms
    /// </summary>
    public class WaveViewer : UserControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components = null;
        private WaveStream waveStream;
        private ISampleProvider sampleProvider;
        private int samplesPerPixel = 128;
        private long startPosition;
        private int bytesPerSampleFrame;
        private int channels;
        private float[] readBuffer;

        /// <summary>
        /// Creates a new WaveViewer control
        /// </summary>
        public WaveViewer()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
            DoubleBuffered = true;
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
                    channels = waveStream.WaveFormat.Channels;
                    bytesPerSampleFrame = (waveStream.WaveFormat.BitsPerSample / 8) * channels;
                    sampleProvider = waveStream.ToSampleProvider();
                }
                else
                {
                    sampleProvider = null;
                }
                Invalidate();
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
                Invalidate();
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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// <see cref="Control.OnPaint"/>
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (sampleProvider != null)
            {
                int samplesPerColumn = samplesPerPixel * channels;
                waveStream.Position = startPosition + (e.ClipRectangle.Left * bytesPerSampleFrame * samplesPerPixel);
                readBuffer = BufferHelpers.Ensure(readBuffer, samplesPerColumn);

                for (float x = e.ClipRectangle.X; x < e.ClipRectangle.Right; x += 1)
                {
                    float low = 0;
                    float high = 0;
                    int samplesRead = sampleProvider.Read(readBuffer.AsSpan(0, samplesPerColumn));
                    if (samplesRead == 0)
                        break;
                    for (int n = 0; n < samplesRead; n++)
                    {
                        float sample = readBuffer[n];
                        if (sample < low) low = sample;
                        if (sample > high) high = sample;
                    }
                    float lowPercent = (low + 1f) / 2f;
                    float highPercent = (high + 1f) / 2f;
                    e.Graphics.DrawLine(Pens.Black, x, Height * (1 - lowPercent), x, Height * (1 - highPercent));
                }
            }

            base.OnPaint(e);
        }


        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new Container();
        }
        #endregion
    }
}
