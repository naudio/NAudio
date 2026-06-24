using System;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace NAudio.Gui
{
    /// <summary>
    /// Summary description for Fader.
    /// </summary>
    public class Fader : System.Windows.Forms.Control
    {
        private int minimum;
        private int maximum;
        private float percent;
        private Orientation orientation;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        /// <summary>
        /// Creates a new Fader control
        /// </summary>
        public Fader()
        {
            // Required for Windows.Forms Class Composition Designer support
            InitializeComponent();

            this.SetStyle(ControlStyles.DoubleBuffer | 
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,true);
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

        private readonly int SliderHeight = 30;
        private readonly int SliderWidth = 15;
        private Rectangle sliderRectangle = new Rectangle();

        private void DrawSlider(Graphics g)
        {
            Brush block = new SolidBrush(Color.White);
            Pen centreLine = new Pen(Color.Black);
            sliderRectangle.X = (this.Width - SliderWidth) / 2;
            sliderRectangle.Width = SliderWidth;
            sliderRectangle.Y = (int) ((this.Height - SliderHeight) * percent);
            sliderRectangle.Height = SliderHeight;

            g.FillRectangle(block,sliderRectangle);
            g.DrawLine(centreLine,sliderRectangle.Left,sliderRectangle.Top + sliderRectangle.Height/2,sliderRectangle.Right,sliderRectangle.Top + sliderRectangle.Height/2);
            block.Dispose();
            centreLine.Dispose();

            /*sliderRectangle.X = (this.Width - SliderWidth) / 2;
            sliderRectangle.Width = SliderWidth;
            sliderRectangle.Y = (int)((this.Height - SliderHeight) * percent);
            sliderRectangle.Height = SliderHeight;
            g.DrawImage(Images.Fader1,sliderRectangle);*/

            
        }


        /// <summary>
        /// <see cref="Control.OnPaint"/>
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if(this.Orientation == Orientation.Vertical)
            {
                Brush groove = new SolidBrush(Color.Black);
                g.FillRectangle(groove, this.Width / 2, SliderHeight / 2, 2, this.Height - SliderHeight);
                groove.Dispose();
                DrawSlider(g);
            }
            
            base.OnPaint (e);
        }

        private bool dragging;
        private int dragY;

        /// <summary>
        /// <see cref="Control.OnMouseDown"/>
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if(sliderRectangle.Contains(e.X,e.Y))
            {
                dragging = true;
                dragY = e.Y - sliderRectangle.Y;
            }
            // TODO: are we over the fader
            base.OnMouseDown (e);
        }

        /// <summary>
        /// <see cref="Control.OnMouseMove"/>
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if(dragging)
            {
                int sliderTop = e.Y - dragY;
                if(sliderTop < 0)
                {
                    this.percent = 0;
                }
                else if(sliderTop > this.Height - SliderHeight)
                {
                    this.percent = 1;
                }
                else
                {
                    percent = (float) sliderTop / (float) (this.Height - SliderHeight);					
                }
                this.Invalidate();
            }
            base.OnMouseMove (e);
        }

        /// <summary>
        /// <see cref="Control.OnMouseUp"/>
        /// </summary>        
        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp (e);
        }



        /// <summary>
        /// Minimum value of this fader
        /// </summary>
        public int Minimum
        {
            get
            {
                return minimum;
            }
            set
            {
                minimum = value;
            }
        }

        /// <summary>
        /// Maximum value of this fader
        /// </summary>
        public int Maximum
        {
            get
            {
                return maximum;
            }
            set
            {
                maximum = value;
            }
        }

        /// <summary>
        /// Current value of this fader
        /// </summary>
        public int Value
        {
            get
            {
                return (int) (percent * (maximum-minimum)) + minimum;
            }
            set
            {
                percent = (float) (value-minimum) / (maximum-minimum);
            }
        }

        /// <summary>
        /// Fader orientation
        /// </summary>
        public Orientation Orientation
        {
            get
            {
                return orientation;
            }
            set
            {
                orientation = value;
            }
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
