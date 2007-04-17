using System;
using System.IO;

namespace NAudio.Wave
{
	/// <summary>
	/// Implements a delay effect
	/// </summary>
	public class Delay : WaveStream 
	{
		private WaveStream inStream;
		private short[] buffer16; // signed 16 bit audio
		private float[] bufferFloat; // 32bit float audio
		
		/// <summary>
		/// Creates a new delay effect
		/// </summary>
		/// <param name="inStream">The incoming audio data</param>
		/// <param name="length">The delay length in milliseconds</param>
		/// <param name="wetPercent">Percentage of output delay</param>
		/// <param name="feedbackPercent">Percentage feedback</param>
		public Delay(WaveStream inStream, int length, float wetPercent, float feedbackPercent) 
		{
			this.inStream = inStream;
			if(inStream.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat) 
			{
				bufferFloat = new float[(inStream.WaveFormat.AverageBytesPerSecond * length)/ 1000];
			}
			else if(inStream.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
			{
				buffer16 = new short[(inStream.WaveFormat.AverageBytesPerSecond * length)/ 1000];
			}
		}
		
		/// <summary>
		/// Returns the WaveFormat of this WaveStream
		/// </summary>
		public override WaveFormat WaveFormat 
		{
			get 
			{
				return inStream.WaveFormat;
			}
		}
		
		/// <summary>
		/// Returns the length of this stream
		/// </summary>
		public override long Length 
		{
			get 
			{
				return inStream.Length; // TODO: plus our extra bit on the end
			}
		}
		
		/// <summary>
		/// Returns our current position in the stream
		/// </summary>
		public override long Position 
		{
			get 
			{
				return inStream.Position;
			}
			set 
			{
				inStream.Position = value;
			}
		}
		
		/// <summary>
		/// Reads bytes from this stream into a buffer
		/// </summary>
		/// <param name="buffer">Buffer to read into</param>
		/// <param name="offset">Offset to start at</param>
		/// <param name="length">Number of bytes to read</param>
		/// <returns>Number of bytes read</returns>
		public override int Read(byte[] buffer, int offset, int length) 
		{
			throw new NotSupportedException("Delay.Read");
		}
		
		/// <summary>
		/// Gets the recommended read size
		/// </summary>
		/// <param name="milliseconds">Number of milliseconds of audio to read</param>
		/// <returns>Recommended number of bytes to read</returns>
		public override int GetReadSize(int milliseconds)
		{
			return inStream.GetReadSize(milliseconds);
		}

		/// <summary>
		/// Block alignment for this stream
		/// </summary>
		public override int BlockAlign
		{
			get
			{
				return inStream.BlockAlign;
			}
		}
	}
}
