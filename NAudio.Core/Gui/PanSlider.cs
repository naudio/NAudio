using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace NAudio.Gui
{
	/// <summary>
	/// Pan slider control
	/// </summary>
	public class PanSlider : System.Windows.Forms.UserControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private float pan;

        /// <summary>
        /// True when pan value changed
        /// </summary>
		public event EventHandler PanChanged;

        /// <summary>
        /// Creates a new PanSlider control
        /// </summary>
		public PanSlider()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitComponent call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// PanSlider
			// 
			this.Name = "PanSlider";
			this.Size = new System.Drawing.Size(104, 16);

		}
		#endregion

        /// <summary>
        /// <see cref="Control.OnPaint"/>
        /// </summary>
		protected override void OnPaint(PaintEventArgs pe)
		{
			StringFormat format = new StringFormat();
			format.LineAlignment = StringAlignment.Center;
			format.Alignment = StringAlignment.Center;
			string panValue;
			if(pan == 0.0)
			{
				pe.Graphics.FillRectangle(Brushes.Orange,(this.Width/2) - 1  ,1,3,this.Height-2);
				panValue = "C";
			}
			else if(pan > 0)
			{
				pe.Graphics.FillRectangle(Brushes.Orange,(this.Width/2),1,(int) ((this.Width/2) * pan),this.Height-2);
				panValue = String.Format("{0:F0}%R",pan*100);
			}
			else
			{
				pe.Graphics.FillRectangle(Brushes.Orange,(int)((this.Width/2) * (pan+1)),1,(int) ((this.Width/2) * (0-pan)),this.Height-2);
				panValue = String.Format("{0:F0}%L",pan*-100);
			}
			pe.Graphics.DrawRectangle(Pens.Black,0,0,this.Width-1,this.Height-1);

			pe.Graphics.DrawString(panValue,this.Font,
				Brushes.Black,this.ClientRectangle,format);
			// Calling the base class OnPaint
			//base.OnPaint(pe);
		}

        /// <summary>
        /// <see cref="Control.OnMouseMove"/>
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
		{
			if(e.Button == MouseButtons.Left)
			{
				SetPanFromMouse(e.X);
			}
			base.OnMouseMove (e);
		}

		/// <summary>
		/// <see cref="Control.OnMouseDown"/>
		/// </summary>
		/// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
		{			
			SetPanFromMouse(e.X);
			base.OnMouseDown (e);			
		}

		private void SetPanFromMouse(int x)
		{
			Pan = (((float) x / this.Width) * 2.0f) - 1.0f;
		}

        /// <summary>
        /// The current Pan setting
        /// </summary>
		public float Pan
		{
			get
			{
				return pan;
			}
			set
			{
				if(value < -1.0f)
					value = -1.0f;
				if(value > 1.0f)
					value = 1.0f;
				if(value != pan)
				{
					pan = value;
					if(PanChanged != null)
						PanChanged(this,EventArgs.Empty);
					Invalidate();
				}
				
			}
		}
	}
}
