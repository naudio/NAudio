using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace NAudio.Gui
{
	/// <summary>
	/// LCD panel control
	/// </summary>
	public class LcdPanel : System.Windows.Forms.UserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private int pixelSize = 2;
		
		private Color onColor;
		private Color offColor;
		//private string text;
		
        /// <summary>
        /// Creates a new LCD panel
        /// </summary>
		public LcdPanel()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			this.BackColor = Color.FromArgb(0xBD,0xFF,0xBC); //Color.FromArgb(0x8D,0xE0,0x74);
			this.OffColor = Color.FromArgb(0x7D,0xC6,0x67);
			this.OnColor = Color.FromArgb(0x42,0x68,0x37);
			this.Text = "ACDEFHILMNORSTU";
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
			int row = 0;
			int col = 0;
			int charsPerLine = this.Width / (pixelSize * 6);
			int lines = this.Height / (pixelSize * 8);
			foreach(char c in this.Text)
			{
				DrawCharacter(e.Graphics,c,row,col);
				col++; 
				if(col >= charsPerLine)
				{
					col=0;
					row++;
					if(row >= lines)
						return;
				}				
			}

			base.OnPaint (e);
		}

		private void DrawCharacter(Graphics g, char c,int row, int col)
		{
			g.FillRectangle(new SolidBrush(OffColor),1+col*6*pixelSize,1+row*8*pixelSize,5*pixelSize,7*pixelSize);
			BitArray b = new BitArray(7*5);
			// TODO: use a long
			switch(c)
			{
				case ' ':
					break;
				case 'A':
					b[1]=b[2]=b[3]=true;
					b[5]=b[9]=true;
					b[10]=b[14]=true;
					b[15]=b[19]=true;
					b[20]=b[21]=b[22]=b[23]=b[24]=true;
					b[25]=b[29]=true;
					b[30]=b[34]=true;
					break;
				case 'C':
					b[1]=b[2]=b[3]=true;
					b[5]=b[9]=true;
					b[10]=true;
					b[15]=true;
					b[20]=true;
					b[25]=b[29]=true;
					b[31]=b[32]=b[33]=true;
					break;
				case 'D':
					b[0]=b[1]=b[2]=true;
					b[5]=b[8]=true;
					b[10]=b[14]=true;
					b[15]=b[19]=true;
					b[20]=b[24]=true;
					b[25]=b[28]=true;
					b[30]=b[31]=b[32]=true;
					break;
				case 'E':
					b[0]=b[1]=b[2]=b[3]=b[4]=true;
					b[5]=true;
					b[10]=true;
					b[15]=b[16]=b[17]=b[18]=true;
					b[20]=true;
					b[25]=true;
					b[30]=b[31]=b[32]=b[33]=b[34]=true;
					break;
				case 'F':
					b[0]=b[1]=b[2]=b[3]=b[4]=true;
					b[5]=true;
					b[10]=true;
					b[15]=b[16]=b[17]=b[18]=true;
					b[20]=true;
					b[25]=true;
					b[30]=true;
					break;
				case 'H':
					b[0]=b[4]=true;
					b[5]=b[9]=true;
					b[10]=b[14]=true;
					b[15]=b[16]=b[17]=b[18]=b[19]=true;
					b[20]=b[24]=true;
					b[25]=b[29]=true;
					b[30]=b[34]=true;
					break;				
				case 'I':
					b[1]=b[2]=b[3]=true;
					b[7]=true;
					b[12]=true;
					b[17]=true;
					b[22]=true;
					b[27]=true;
					b[31]=b[32]=b[33]=true;
					break;
				case 'L':
					b[0]=true;
					b[5]=true;
					b[10]=true;
					b[15]=true;
					b[20]=true;
					b[25]=true;
					b[30]=b[31]=b[32]=b[33]=b[34]=true;
					break;
				case 'M':
					b[0]=b[4]=true;
					b[5]=b[6]=b[8]=b[9]=true;
					b[10]=b[12]=b[14]=true;
					b[15]=b[17]=b[19]=true;
					b[20]=b[24]=true;
					b[25]=b[29]=true;
					b[30]=b[34]=true;
					break;
				case 'N':
					b[0]=b[4]=true;
					b[5]=b[9]=true;
					b[10]=b[11]=b[14]=true;
					b[15]=b[17]=b[19]=true;
					b[20]=b[23]=b[24]=true;
					b[25]=b[29]=true;
					b[30]=b[34]=true;
					break;
				case 'O':
					b[1]=b[2]=b[3]=true;
					b[5]=b[9]=true;
					b[10]=b[14]=true;
					b[15]=b[19]=true;
					b[20]=b[24]=true;
					b[25]=b[29]=true;
					b[31]=b[32]=b[33]=true;
					break;
				case 'R':
					b[0]=b[1]=b[2]=b[3]=true;
					b[5]=b[9]=true;
					b[10]=b[14]=true;
					b[15]=b[16]=b[17]=b[18]=true;
					b[20]=b[22]=true;
					b[25]=b[28]=true;
					b[30]=b[34]=true;
					break;
				case 'S':
					b[1]=b[2]=b[3]=b[4]=true;
					b[5]=true;
					b[10]=true;
					b[16]=b[17]=b[18]=true;
					b[24]=true;
					b[29]=true;
					b[30]=b[31]=b[32]=b[33]=true;
					break;
				case 'T':
					b[0]=b[1]=b[2]=b[3]=b[4]=true;
					b[7]=true;
					b[12]=true;
					b[17]=true;
					b[22]=true;
					b[27]=true;
					b[32]=true;
					break;
				case 'U':
					b[0]=b[4]=true;
					b[5]=b[9]=true;
					b[10]=b[14]=true;
					b[15]=b[19]=true;
					b[20]=b[24]=true;
					b[25]=b[29]=true;
					b[31]=b[32]=b[33]=true;
					break;
			}
			SolidBrush onBrush = new SolidBrush(OnColor);
			for(int x = 0; x < 5; x++)
				for(int y = 0; y < 7; y++)
					if(b[y*5+x])
						g.FillRectangle(onBrush,1+col*6*pixelSize+x*pixelSize,1+row*8*pixelSize+y*pixelSize,pixelSize,pixelSize);
		}

        /// <summary>
        /// Pixel colour for pixels turned off
        /// </summary>
		public Color OffColor
		{
			get
			{
				return offColor;
			}
			set
			{
				offColor = value;
			}
		}

        /// <summary>
        /// Pixel colour for pixels turned on
        /// </summary>
		public Color OnColor
		{
			get
			{
				return onColor;
			}
			set
			{
				onColor = value;
			}
		}


		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// LcdPanel
			// 
			this.Name = "LcdPanel";
			this.Size = new System.Drawing.Size(192, 56);

		}
		#endregion
	}
}
