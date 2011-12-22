using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace NAudio.Gui
{
	/// <summary>
	/// Graph control
	/// </summary>
	public class Graph : System.Windows.Forms.Control
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        /// <summary>
        /// Creates a new graph control
        /// </summary>
		public Graph()
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
			components = new System.ComponentModel.Container();
		}
		#endregion

        /// <summary>
        /// <see cref="Control.OnPaint"/>
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
		{
			pe.Graphics.FillRectangle(new SolidBrush(this.BackColor),this.ClientRectangle);
			using(Pen pen = new Pen(this.ForeColor))
			{
				int binsPerPixel = this.x.Length / this.Width;
				if(binsPerPixel == 0)
					binsPerPixel = 1;
				int currentBin = 0;
				for(int pixel = 0; pixel < this.Width; pixel++)
				{
					double magnitude = 0;
					for(int bin = 0; bin < binsPerPixel; bin++)
					{
						magnitude += Math.Abs(x[currentBin+bin]);
					}
					magnitude /= binsPerPixel;
					currentBin += binsPerPixel;
					pe.Graphics.DrawLine(pen,pixel,0,pixel,(int) (this.Height * magnitude));

				}
			}

			// TODO: Add custom paint code here

			// Calling the base class OnPaint
			base.OnPaint(pe);
		}

		private double[] x;

        /// <summary>
        /// sets graph data
        /// </summary>
        public void SetData(double[] x)
		{
			this.x = x;
		}
	}
}
