using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace NAudio.Gui.TrackView
{
    /// <summary>
    /// The timeline control displays a time-grid
    /// Features to add:
    /// - Time vs MBFT
    /// - markers, marker dragging
    /// - clicking new time
    /// - range selection
    /// - zoom level
    /// </summary>
    public partial class TimeLine : UserControl
    {
        private TimeSpan nowTime = TimeSpan.Zero;
        private const int measureTickHeight = 20;
        private const int beatTickHeight = 5;
        
        private double tempo = 120;
        private double pixelsPerSecond = 80;
        private Pen foregroundPen;
        private Brush foregroundBrush;

        /// <summary>
        /// Position clicked event
        /// </summary>
        public event EventHandler<TimeLinePositionClickedEventArgs> PositionClicked;

        /// <summary>
        /// Creates a new timeline control
        /// </summary>
        public TimeLine()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;
            InitializeComponent();
            CreateForegroundPens();
        }

        /// <summary>
        /// The current time
        /// </summary>
        public TimeSpan NowTime
        {
            get { return nowTime; }
            set { nowTime = value; Invalidate(); }
        }

        /// <summary>
        /// Pixels per second.
        /// Effectively a zoom level
        /// </summary>
        public double PixelsPerSecond
        {
            get { return pixelsPerSecond; }
            set { pixelsPerSecond = value; Invalidate(); }
        }

        /// <summary>
        /// Forecolor changed event handler
        /// </summary>
        protected override void OnForeColorChanged(EventArgs e)
        {
            CreateForegroundPens();
            base.OnForeColorChanged(e);
        }


        private void CreateForegroundPens()
        {
            foregroundPen = new Pen(ForeColor);
            foregroundBrush = new SolidBrush(ForeColor);

        }

        /// <summary>
        /// Paint event handler
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            //int pixelsPerBeat = (int) (pixelsPerSecond * 60.0 / tempo);
            int x = 0;
            for (int beat = 0; x < this.Width; beat++)
            {
                int height = (beat % 4 == 0) ? measureTickHeight : beatTickHeight;
                //x = beat * pixelsPerBeat;
                x = TimeToX(TimeSpan.FromSeconds((beat * 60) / tempo));
                e.Graphics.DrawLine(foregroundPen, x, this.Height - height, x, this.Height);
                if (beat % 4 == 0)
                {
                    int measure = beat / 4;
                    e.Graphics.DrawString(measure.ToString(), this.Font, foregroundBrush, new PointF(x + 2, this.Height - measureTickHeight));
                }
            }
            x = TimeToX(nowTime);
            e.Graphics.DrawLine(Pens.Red, x, 0, x, this.Height);
            
            e.Graphics.DrawLine(foregroundPen, 0, this.Height - 1, this.Width, this.Height -1);
            base.OnPaint(e);
        }

        private int TimeToX(TimeSpan t)
        {
            return (int)(pixelsPerSecond * t.TotalSeconds);
        }

        private TimeSpan XToTime(int x)
        {
            return TimeSpan.FromSeconds(x / pixelsPerSecond);
        }

        /// <summary>
        /// Mouse down event handler
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (PositionClicked != null)
            {
                PositionClicked(this, new TimeLinePositionClickedEventArgs(XToTime(e.X)));
            }
        }
    }

    /// <summary>
    /// TimeLine Position Clicked event arguments
    /// </summary>
    public class TimeLinePositionClickedEventArgs : EventArgs
    {
        private TimeSpan position;

        /// <summary>
        /// Creates a new TimeLinePositionClickedEventArgs
        /// </summary>
        public TimeLinePositionClickedEventArgs(TimeSpan position)
        {
            this.position = position;
        }

        /// <summary>
        /// The position clicked
        /// </summary>
        public TimeSpan Position
        {
            get { return position; }
        }
    }
}
