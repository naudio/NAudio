using System;
using NAudio.Utils;

namespace NAudio.Wave
{
	/// <summary>
	/// Converts IEEE float to 24 bit PCM, optionally clipping and adjusting volume along the way
	/// </summary>
	public class WaveFloatTo24Provider : IWaveProvider
	{
		private readonly IWaveProvider sourceProvider;
		private volatile float volume;
		private byte[] sourceBuffer;

		/// <summary>
		/// Creates a new WaveFloatTo24Provider
		/// </summary>
		/// <param name="sourceProvider">the source provider</param>
		public WaveFloatTo24Provider(IWaveProvider sourceProvider)
		{
			if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
				throw new ArgumentException("Input wave provider must be IEEE float", "sourceProvider");
			if (sourceProvider.WaveFormat.BitsPerSample != 32)
				throw new ArgumentException("Input wave provider must be 32 bit", "sourceProvider");

			WaveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 24, sourceProvider.WaveFormat.Channels);

			this.sourceProvider = sourceProvider;
			this.volume = 1f;
		}

		/// <summary>
		/// <see cref="IWaveProvider.WaveFormat"/>
		/// </summary>
		public WaveFormat WaveFormat { get; private set; }

		/// <summary>
		/// Volume of this channel. 1.0 = full scale
		/// </summary>
		public float Volume
		{
			get => this.volume;
			set => this.volume = value;
		}

		/// <summary>
		/// Reads bytes from this wave stream
		/// </summary>
		/// <param name="destBuffer">The destination buffer</param>
		/// <param name="offset">Offset into the destination buffer</param>
		/// <param name="numBytes">Number of bytes read</param>
		/// <returns>Number of bytes read.</returns>
		public int Read(byte[] destBuffer, int offset, int numBytes)
		{
			var sourceBytesRequired = (int)(numBytes * (4f / 3f));
			sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
			var sourceBytesRead = this.sourceProvider.Read(this.sourceBuffer, 0, sourceBytesRequired);

			var sourceWaveBuffer = new WaveBuffer(sourceBuffer);
			var destWaveBuffer = new WaveBuffer(destBuffer);

			var sourceSampleCount = sourceBytesRead / 4;
			for (var sampleIndex = 0; sampleIndex < sourceSampleCount; sampleIndex++)
			{
				// adjust volume
				var sample32 = sourceWaveBuffer.FloatBuffer[sampleIndex] * this.volume;

				// clip
				if (sample32 > 1f)
					sample32 = 1f;
				else if (sample32 < -1f)
					sample32 = -1f;

				var sample24 = (int)(sample32 * 8388607);
				destWaveBuffer.ByteBuffer[offset++] = (byte)(sample24 & 0xFF);
				destWaveBuffer.ByteBuffer[offset++] = (byte)(sample24 >> 8);
				destWaveBuffer.ByteBuffer[offset++] = (byte)(sample24 >> 16);
			}

			return sourceSampleCount * 3;
		}
	}
}
