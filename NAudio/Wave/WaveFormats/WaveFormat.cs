using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
	/// <summary>
	/// Represents a Wave file format
	/// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2)]
	public class WaveFormat
	{
		/// <summary>format type</summary>
		protected WaveFormatEncoding waveFormatTag;
		/// <summary>number of channels</summary>
		protected short channels;
		/// <summary>sample rate</summary>
		protected int sampleRate;
		/// <summary>for buffer estimation</summary>
		protected int averageBytesPerSecond;
		/// <summary>block size of data</summary>
		protected short blockAlign;
		/// <summary>number of bits per sample of mono data</summary>
		protected short bitsPerSample;
        /// <summary>number of following bytes</summary>
        protected short extraSize;

		/// <summary>
		/// Creates a new PCM 44.1Khz stereo 16 bit format
		/// </summary>
		public WaveFormat() : this(44100,16,2)
		{

		}

        /// <summary>
        /// Creates a WaveFormat with custom members
        /// </summary>
        /// <param name="tag">The encoding</param>
        /// <param name="channels">Number of channels</param>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="averageBytesPerSecond">Average Bytes Per Second</param>
        /// <param name="blockAlign">Block Align</param>
        /// <param name="bitsPerSample">Bits Per Sample</param>
        /// <returns></returns>
        public static WaveFormat CreateCustomFormat(WaveFormatEncoding tag, int channels, int sampleRate, int averageBytesPerSecond, int blockAlign, int bitsPerSample)
        {
            WaveFormat waveFormat = new WaveFormat();
            waveFormat.waveFormatTag = tag;
            waveFormat.channels = (short)channels;
            waveFormat.sampleRate = sampleRate;
            waveFormat.averageBytesPerSecond = averageBytesPerSecond;
            waveFormat.blockAlign = (short)blockAlign;
            waveFormat.bitsPerSample = (short)bitsPerSample;
            waveFormat.extraSize = 0;
            return waveFormat;
        }

		/// <summary>
		/// Creates a new PCM format with the specified sample rate, bit depth and channels
		/// </summary>
		public WaveFormat(int rate, int bits, int channels)
		{
			// minimum 16 bytes, sometimes 18 for PCM
			this.waveFormatTag = WaveFormatEncoding.Pcm;
			this.channels = (short)channels;
			this.sampleRate = rate;
			this.bitsPerSample = (short)bits;
			this.extraSize = 0;
	               
			this.blockAlign = (short)(channels * (bits / 8));
			this.averageBytesPerSecond = this.sampleRate * this.blockAlign;
		}

        /// <summary>
        /// Creates a new 32 bit IEEE floating point wave format
        /// </summary>
        /// <param name="sampleRate">sample rate</param>
        /// <param name="channels">number of channels</param>
        public static WaveFormat CreateIeeeFloatWaveFormat(int sampleRate, int channels)
        {
            WaveFormat wf = new WaveFormat();
            wf.waveFormatTag = WaveFormatEncoding.IeeeFloat;
            wf.channels = (short)channels;
            wf.bitsPerSample = 32;
            wf.sampleRate = sampleRate;
            wf.blockAlign = (short) (4*channels);
            wf.averageBytesPerSecond = sampleRate * wf.blockAlign;
            wf.extraSize = 0;
            return wf;
        }

		/// <summary>
		/// Reads a new WaveFormat object from a stream
		/// </summary>
		/// <param name="br">A binary reader that wraps the stream</param>
		public WaveFormat(BinaryReader br)
		{
			int formatChunkLength = br.ReadInt32();
			if(formatChunkLength < 16)
				throw new ApplicationException("Invalid WaveFormat Structure");
			this.waveFormatTag = (WaveFormatEncoding) br.ReadUInt16();
			this.channels = br.ReadInt16();
			this.sampleRate = br.ReadInt32();				
			this.averageBytesPerSecond = br.ReadInt32();
			this.blockAlign = br.ReadInt16();
			this.bitsPerSample = br.ReadInt16();
            if (formatChunkLength > 16)
            {
                
                this.extraSize = br.ReadInt16();
                if (this.extraSize > formatChunkLength - 18)
                {
                    Console.WriteLine("Format chunk mismatch");
                    //RRL GSM exhibits this bug. Don't throw an exception
                    //throw new ApplicationException("Format chunk length mismatch");

                    this.extraSize = (short) (formatChunkLength - 18);
                }
                
                // read any extra data
                br.ReadBytes(extraSize);

            }
		}

		/// <summary>
		/// Reports this WaveFormat as a string
		/// </summary>
		/// <returns>String describing the wave format</returns>
		public override string ToString()
		{
            switch (this.waveFormatTag)
            {
                case WaveFormatEncoding.Pcm:
                case WaveFormatEncoding.WAVE_FORMAT_EXTENSIBLE:
                    // extensible just has some extra bits after the PCM header
                    return String.Format("{0} bit PCM: {1}kHz {2} channels",
                        bitsPerSample, sampleRate / 1000, channels);
                default:
                    return this.waveFormatTag.ToString();
            }
		}

		/// <summary>
		/// Compares with another WaveFormat object
		/// </summary>
		/// <param name="obj">Object to compare to</param>
		/// <returns>True if the objects are the same</returns>
		public override bool Equals(object obj)
		{
			WaveFormat other = obj as WaveFormat;
			if(other != null)
			{
				return waveFormatTag == other.waveFormatTag &&
					channels == other.channels &&
					sampleRate == other.sampleRate &&
					averageBytesPerSecond == other.averageBytesPerSecond &&
					blockAlign == other.blockAlign &&
					bitsPerSample == other.bitsPerSample;
			}
			return false;
		}

		/// <summary>
		/// Provides a Hashcode for this WaveFormat
		/// </summary>
		/// <returns>A hashcode</returns>
		public override int GetHashCode()
		{
			return (int) waveFormatTag ^ 
				(int) channels ^ 
				sampleRate ^ 
				averageBytesPerSecond ^ 
				(int) blockAlign ^ 
				(int) bitsPerSample;
		}

		/// <summary>
		/// Returns the encoding type used
		/// </summary>
		public WaveFormatEncoding Encoding
		{
			get	
			{
				return waveFormatTag;
			}
		}

        /// <summary>
        /// Writes this WaveFormat object to a stream
        /// </summary>
        /// <param name="writer">the output stream</param>
        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write((int)(18 + extraSize)); // wave format length
            writer.Write((short)Encoding);
            writer.Write((short)Channels);
            writer.Write((int)SampleRate);
            writer.Write((int)AverageBytesPerSecond);
            writer.Write((short)BlockAlign);
            writer.Write((short)BitsPerSample);
            writer.Write((short)extraSize);
        }

		/// <summary>
		/// Returns the number of channels (1=mono,2=stereo etc)
		/// </summary>
		public int Channels
		{
			get
			{
				return channels;
			}
		}

		/// <summary>
		/// Returns the sample rate (samples per second)
		/// </summary>
		public int SampleRate
		{
			get
			{
				return sampleRate;
			}
		}

		/// <summary>
		/// Returns the average number of bytes used per second
		/// </summary>
		public int AverageBytesPerSecond
		{
			get
			{
				return averageBytesPerSecond;
			}
		}

		/// <summary>
		/// Returns the block alignment
		/// </summary>
		public virtual int BlockAlign
		{
			get
			{
				return blockAlign;
			}
		}

		/// <summary>
		/// Returns the number of bits per sample (usually 16 or 32, sometimes 24 or 8)
		/// Can be 0 for some codecs
		/// </summary>
		public int BitsPerSample
		{
			get
			{
				return bitsPerSample;
			}
		}

        
	}
}
