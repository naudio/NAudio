using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace NAudio.Gui
{
    /// <summary>
    /// Control that represents a potentiometer
    /// TODO list:
    /// Optional Log scale
    /// Optional reverse scale
    /// Keyboard control
    /// Optional bitmap mode
    /// Optional complete draw mode
    /// Tooltip support
    /// </summary>
    public partial class Pot : UserControl
    {
        // control properties
        private double minimum = 0.0;
        private double maximum = 1.0;
        private double value = 0.5;
        //
        private int beginDragY;
        private double beginDragValue;
        private bool dragging;

        /// <summary>
        /// Value changed event
        /// </summary>
        public event EventHandler ValueChanged;

        /// <summary>
        /// Creates a new pot control
        /// </summary>
        public Pot()
        {
            this.SetStyle(ControlStyles.DoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint, true);
            InitializeComponent();
        }

        /// <summary>
        /// Minimum Value of the Pot
        /// </summary>
        public double Minimum
        {
            get
            {
                return minimum;
            }
            set
            {
                if (value >= maximum)
                    throw new ArgumentOutOfRangeException("Minimum must be less than maximum");
                minimum = value;
                if (Value < minimum)
                    Value = minimum;
            }
        }

        /// <summary>
        /// Maximum Value of the Pot
        /// </summary>
        public double Maximum
        {
            get
            {
                return maximum;
            }
            set
            {
                if (value <= minimum)
                    throw new ArgumentOutOfRangeException("Maximum must be greater than minimum");
                maximum = value;
                if (Value > maximum)
                    Value = maximum;
            }
        }

        /// <summary>
        /// The current value of the pot
        /// </summary>
        public double Value
        {
            get
            {
                return value;
            }
            set
            {
                SetValue(value, false);
            }
        }

        private void SetValue(double newValue, bool raiseEvents)
        {
            if (this.value != newValue)
            {
                this.value = newValue;
                if (raiseEvents)
                {
                    if (ValueChanged != null)
                        ValueChanged(this, EventArgs.Empty);
                }
                Invalidate();
            }
        }

        /// <summary>
        /// Draws the control
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            int diameter = Math.Min(this.Width-4,this.Height-4);
                        
            Pen potPen = new Pen(ForeColor,3.0f);
            potPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
            System.Drawing.Drawing2D.GraphicsState state = e.Graphics.Save();
            //e.Graphics.TranslateTransform(diameter / 2f, diameter / 2f);
            e.Graphics.TranslateTransform(this.Width / 2, this.Height / 2);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawArc(potPen, new Rectangle(diameter / -2, diameter / -2, diameter, diameter), 135, 270);
            
            double percent = (value - minimum) / (maximum - minimum);
            double degrees = 135 + (percent * 270);
            double x = (diameter / 2.0) * Math.Cos(Math.PI * degrees / 180);
            double y = (diameter / 2.0) * Math.Sin(Math.PI * degrees / 180);
            e.Graphics.DrawLine(potPen, 0, 0, (float) x, (float) y);
            e.Graphics.Restore(state);
            base.OnPaint(e);
        }

        /// <summary>
        /// Handles the mouse down event to allow changing value by dragging
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            dragging = true;
            beginDragY = e.Y;
            beginDragValue = value;
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Handles the mouse up event to allow changing value by dragging
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
        }

        /// <summary>
        /// Handles the mouse down event to allow changing value by dragging
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                int yDifference = beginDragY - e.Y;
                // 100 is the number of pixels of vertical movement that represents the whole scale
                double delta = (maximum - minimum) * (yDifference / 150.0);
                double newValue = beginDragValue + delta;
                if (newValue < minimum)
                    newValue = minimum;
                if (newValue > maximum)
                    newValue = maximum;
                SetValue(newValue,true);
            }
            base.OnMouseMove(e);
        }
    }


}
