using System;
using NAudio.Utils;

namespace NAudio.Wave
{
	/// <summary>
	/// Converts 24 bit PCM to IEEE float, optionally adjusting volume along the way
	/// </summary>
	public class Wave24ToFloatProvider : IWaveProvider
	{
		private readonly IWaveProvider sourceProvider;
		private volatile float volume;
		private byte[] sourceBuffer;

		/// <summary>
		/// Creates a new Wave24toFloatProvider
		/// </summary>
		/// <param name="sourceProvider">the source provider</param>
		public Wave24ToFloatProvider(IWaveProvider sourceProvider)
		{
			if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
				throw new ArgumentException("Only PCM supported");
			if (sourceProvider.WaveFormat.BitsPerSample != 24)
				throw new ArgumentException("Only 24 bit audio supported");

			this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(
				sourceProvider.WaveFormat.SampleRate, 
				sourceProvider.WaveFormat.Channels);

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
			get => volume;
			set => volume = value;
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
			var sourceBytesRequired = (int)(numBytes * (3f / 4f));
			sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
			int sourceBytesRead = this.sourceProvider.Read(sourceBuffer, offset, sourceBytesRequired);

			var sourceWaveBuffer = new WaveBuffer(sourceBuffer);
			var destWaveBuffer = new WaveBuffer(destBuffer);

			var destSampleOffset = offset / 4;
			var sampleArray = new byte[4];
			var lastSampleOffset = sourceBytesRead - 3;

			// the source IWaveBuffer does not contain an Int24 buffer, 
			// so the source byte index is used to find the current the sample offset
			for (var sourceByteIndex = 0; sourceByteIndex < lastSampleOffset; sourceByteIndex += 3)
			{
				Array.Copy(sourceWaveBuffer.ByteBuffer, sourceByteIndex, sampleArray, 1, 3);
				var sample = BitConverter.ToInt32(sampleArray, 0);
				sample >>= 8;

				destWaveBuffer.FloatBuffer[destSampleOffset++] = (sample / 8388608f) * this.volume;
			}

			var sourceSamples = sourceBytesRead / 3;
			return sourceSamples * 4;
		}
	}
}
