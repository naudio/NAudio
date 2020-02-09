using System;
using NAudio.Wave.SampleProviders;

namespace NAudio.Wave
{
    /// <summary>
    /// Represents Channel for the WaveMixerStream
    /// 32 bit output and 16 bit input
    /// It's output is always stereo
    /// The input stream can be panned
    /// </summary>
    public class WaveChannel32 : WaveStream, ISampleNotifier
    {
        private WaveStream sourceStream;
        private readonly WaveFormat waveFormat;
        private readonly long length;
        private readonly int destBytesPerSample;
        private readonly int sourceBytesPerSample;
        private volatile float volume;
        private volatile float pan;
        private long position;
        private readonly ISampleChunkConverter sampleProvider;
        private readonly object lockObject = new object();

        /// <summary>
        /// Creates a new WaveChannel32
        /// </summary>
        /// <param name="sourceStream">the source stream</param>
        /// <param name="volume">stream volume (1 is 0dB)</param>
        /// <param name="pan">pan control (-1 to 1)</param>
        public WaveChannel32(WaveStream sourceStream, float volume, float pan)
        {
            PadWithZeroes = true;

            var providers = new ISampleChunkConverter[] 
            {
                new Mono8SampleChunkConverter(),
                new Stereo8SampleChunkConverter(),
                new Mono16SampleChunkConverter(),
                new Stereo16SampleChunkConverter(),
                new Mono24SampleChunkConverter(),
                new Stereo24SampleChunkConverter(),
                new MonoFloatSampleChunkConverter(),
                new StereoFloatSampleChunkConverter(),
            };
            foreach (var provider in providers)
            {
                if (provider.Supports(sourceStream.WaveFormat))
                {
                    this.sampleProvider = provider;
                    break;
                }
            }

            if (this.sampleProvider == null)
            {
                throw new ArgumentException("Unsupported sourceStream format");
            }
         
            // always outputs stereo 32 bit
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceStream.WaveFormat.SampleRate, 2);
            destBytesPerSample = 8; // includes stereo factoring

            this.sourceStream = sourceStream;
            this.volume = volume;
            this.pan = pan;
            sourceBytesPerSample = sourceStream.WaveFormat.Channels * sourceStream.WaveFormat.BitsPerSample / 8;

            length = SourceToDest(sourceStream.Length);
            position = 0;
        }

        private long SourceToDest(long sourceBytes)
        {
            return (sourceBytes / sourceBytesPerSample) * destBytesPerSample;
        }

        private long DestToSource(long destBytes)
        {
            return (destBytes / destBytesPerSample) * sourceBytesPerSample;
        }

        /// <summary>
        /// Creates a WaveChannel32 with default settings
        /// </summary>
        /// <param name="sourceStream">The source stream</param>
        public WaveChannel32(WaveStream sourceStream)
            :
            this(sourceStream, 1.0f, 0.0f)
        {
        }

        /// <summary>
        /// Gets the block alignment for this WaveStream
        /// </summary>
        public override int BlockAlign => (int)SourceToDest(sourceStream.BlockAlign);

        /// <summary>
        /// Returns the stream length
        /// </summary>
        public override long Length => length;

        /// <summary>
        /// Gets or sets the current position in the stream
        /// </summary>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                lock (lockObject)
                {
                    // make sure we don't get out of sync
                    value -= (value % BlockAlign);
                    if (value < 0)
                    {
                        sourceStream.Position = 0;
                    }
                    else
                    {
                        sourceStream.Position = DestToSource(value);
                    }
                    // source stream may not have accepted the reposition we gave it
                    position = SourceToDest(sourceStream.Position);
                }
            }
        }


        /// <summary>
        /// Reads bytes from this wave stream
        /// </summary>
        /// <param name="destBuffer">The destination buffer</param>
        /// <param name="offset">Offset into the destination buffer</param>
        /// <param name="numBytes">Number of bytes read</param>
        /// <returns>Number of bytes read.</returns>
        public override int Read(byte[] destBuffer, int offset, int numBytes)
        {
            lock (lockObject)
            {
                int bytesWritten = 0;
                WaveBuffer destWaveBuffer = new WaveBuffer(destBuffer);

                // 1. fill with silence
                if (position < 0)
                {
                    bytesWritten = (int) Math.Min(numBytes, 0 - position);
                    for (int n = 0; n < bytesWritten; n++)
                        destBuffer[n + offset] = 0;
                }
                if (bytesWritten < numBytes)
                {
                    sampleProvider.LoadNextChunk(sourceStream, (numBytes - bytesWritten)/8);
                    float left, right;

                    int outIndex = (offset/4) + bytesWritten/4;
                    while (this.sampleProvider.GetNextSample(out left, out right) && bytesWritten < numBytes)
                    {
                        // implement better panning laws. 
                        left = (pan <= 0) ? left : (left*(1 - pan)/2.0f);
                        right = (pan >= 0) ? right : (right*(pan + 1)/2.0f);
                        left *= volume;
                        right *= volume;
                        destWaveBuffer.FloatBuffer[outIndex++] = left;
                        destWaveBuffer.FloatBuffer[outIndex++] = right;
                        bytesWritten += 8;
                        if (Sample != null) RaiseSample(left, right);
                    }
                }
                // 3. Fill out with zeroes
                if (PadWithZeroes && bytesWritten < numBytes)
                {
                    Array.Clear(destBuffer, offset + bytesWritten, numBytes - bytesWritten);
                    bytesWritten = numBytes;
                }
                position += bytesWritten;
                return bytesWritten;
            }
        }

        /// <summary>
        /// If true, Read always returns the number of bytes requested
        /// </summary>
        public bool PadWithZeroes { get; set; }
      

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Volume of this channel. 1.0 = full scale
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }

        /// <summary>
        /// Pan of this channel (from -1 to 1)
        /// </summary>
        public float Pan
        {
            get { return pan; }
            set { pan = value; }
        }

        /// <summary>
        /// Determines whether this channel has any data to play
        /// to allow optimisation to not read, but bump position forward
        /// </summary>
        public override bool HasData(int count)
        {
            // Check whether the source stream has data.
            bool sourceHasData = sourceStream.HasData(count);

            if (sourceHasData)
            {
                if (position + count < 0)
                    return false;
                return (position < length) && (volume != 0);
            }
            return false;
        }

        /// <summary>
        /// Disposes this WaveStream
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (sourceStream != null)
                {
                    sourceStream.Dispose();
                    sourceStream = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "WaveChannel32 was not Disposed");
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Sample
        /// </summary>
        public event EventHandler<SampleEventArgs> Sample;

        // reuse the same object every time to avoid making lots of work for the garbage collector
        private SampleEventArgs sampleEventArgs = new SampleEventArgs(0,0);

        /// <summary>
        /// Raise the sample event (no check for null because it has already been done)
        /// </summary>
        private void RaiseSample(float left, float right)
        {
            sampleEventArgs.Left = left;
            sampleEventArgs.Right = right;
            Sample(this, sampleEventArgs);
        }
    }
}
