using System;
using System.Collections;

namespace NAudio.Dsp
{
	/// <summary>
	/// A shift buffer
	/// </summary>
	public class ShiftBuffer
	{
		private double[][] list;
		private int insertPos;
		private int size;

        /// <summary>
        /// creates a new shift buffer
        /// </summary>
        public ShiftBuffer(int size)
		{
			list = new double[size][];
			insertPos = 0;
			this.size = size;
		}

        /// <summary>
        /// Add samples to the buffer
        /// </summary>
        public void Add(double[] buffer)
		{
			list[insertPos] = buffer;
			insertPos = (insertPos + 1) % size;			
		}

        /// <summary>
        /// Return samples from the buffer
        /// </summary>
		public double[] this[int index]
		{
			get
			{
				return list[(size+insertPos-index) % size];
			}
		}
	}
}
