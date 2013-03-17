using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Dsp;

namespace NAudio.Wave
{
    /// <summary>
    /// A simple compressor
    /// </summary>
    public class SimpleCompressorStream : WaveStream
    {
        private WaveStream sourceStream;
        private SimpleCompressor simpleCompressor;
        private byte[] sourceBuffer; // buffer used by Read function
        private bool enabled;
        private int channels;
        private int bytesPerSample;

        /// <summary>
        /// Create a new simple compressor stream
        /// </summary>
        /// <param name="sourceStream">Source stream</param>
        public SimpleCompressorStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
            this.channels = sourceStream.WaveFormat.Channels;
            this.bytesPerSample = sourceStream.WaveFormat.BitsPerSample / 8;
            simpleCompressor = new SimpleCompressor(5.0, 10.0, sourceStream.WaveFormat.SampleRate);
            simpleCompressor.Threshold = 16;
            simpleCompressor.Ratio = 6;
            simpleCompressor.MakeUpGain = 16;

        }

        /// <summary>
        /// Make-up Gain
        /// </summary>
        public double MakeUpGain
        {
            get 
            { 
                return simpleCompressor.MakeUpGain; 
            }
            set 
            {
                lock (this)
                {
                    simpleCompressor.MakeUpGain = value;
                } 
            }
        }

        /// <summary>
        /// Threshold
        /// </summary>
        public double Threshold
        {
            get 
            { 
                return simpleCompressor.Threshold; 
            }
            set 
            {
                lock (this)
                {
                    simpleCompressor.Threshold = value;
                }
            }
        }

        /// <summary>
        /// Ratio
        /// </summary>
        public double Ratio
        {
            get 
            { 
                return simpleCompressor.Ratio; 
            }
            set 
            {
                lock (this)
                {
                    simpleCompressor.Ratio = value;
                }
            }
        }

        /// <summary>
        /// Attack time
        /// </summary>
        public double Attack
        {
            get
            {
                return simpleCompressor.Attack;
            }
            set
            {
                lock (this)
                {
                    simpleCompressor.Attack = value;
                }
            }
        }

        /// <summary>
        /// Release time
        /// </summary>
        public double Release
        {
            get
            {
                return simpleCompressor.Release;
            }
            set
            {
                lock (this)
                {
                    simpleCompressor.Release = value;
                }
            }
        }


        /// <summary>
        /// Determine whether the stream has the required amount of data.
        /// </summary>
        /// <param name="count">Number of bytes of data required from the stream.</param>
        /// <returns>Flag indicating whether the required amount of data is avialable.</returns>
        public override bool HasData(int count)
        {
            return sourceStream.HasData(count);
        }


        /// <summary>
        /// Turns gain on or off
        /// </summary>
        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
            }
        }


        /// <summary>
        /// Returns the stream length
        /// </summary>
        public override long Length
        {
            get
            {
                return sourceStream.Length;
            }
        }

        /// <summary>
        /// Gets or sets the current position in the stream
        /// </summary>
        public override long Position
        {
            get
            {
                return sourceStream.Position;
            }
            set
            {
                lock (this)
                {
                    sourceStream.Position = value;
                }
            }
        }

        /// <summary>
        /// Gets the WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get
            {
                return sourceStream.WaveFormat;
            }
        }

        private void ReadSamples(byte[] buffer, int start, out double left, out double right)
        {
            if (bytesPerSample == 4)
            {
                left = BitConverter.ToSingle(buffer, start);
                if (channels > 1)
                {
                    right = BitConverter.ToSingle(buffer, start + bytesPerSample);
                }
                else
                {
                    right = left;
                }
            }
            else if (bytesPerSample == 2)
            {
                left = BitConverter.ToInt16(buffer, start) / 32768.0;
                if (channels > 1)
                {
                    right = BitConverter.ToInt16(buffer, start + bytesPerSample) / 32768.0;
                }
                else
                {
                    right = left;
                }
            }
            else
            {
                throw new InvalidOperationException(String.Format("Unsupported bytes per sample: {0}", bytesPerSample));
            }
        }

        private void WriteSamples(byte[] buffer, int start, double left, double right)
        {
            if (bytesPerSample == 4)
            {
                Array.Copy(BitConverter.GetBytes((float)left), 0, buffer, start, bytesPerSample);
                if (channels > 1)
                {
                    Array.Copy(BitConverter.GetBytes((float)right), 0, buffer, start + bytesPerSample, bytesPerSample);
                }
            }
            else if (bytesPerSample == 2)
            {
                Array.Copy(BitConverter.GetBytes((short)(left * 32768.0)), 0, buffer, start, bytesPerSample);
                if (channels > 1)
                {
                    Array.Copy(BitConverter.GetBytes((short)(right * 32768.0)), 0, buffer, start + bytesPerSample, bytesPerSample);
                }
            }
        }

        /// <summary>
        /// Reads bytes from this stream
        /// </summary>
        /// <param name="array">Buffer to read into</param>
        /// <param name="offset">Offset in array to read into</param>
        /// <param name="count">Number of bytes to read</param>
        /// <returns>Number of bytes read</returns>
        public override int Read(byte[] array, int offset, int count)
        {
            lock (this)
            {
                if (Enabled)
                {
                    if (sourceBuffer == null || sourceBuffer.Length < count)
                        sourceBuffer = new byte[count];
                    int sourceBytesRead = sourceStream.Read(sourceBuffer, 0, count);
                    int sampleCount = sourceBytesRead / (bytesPerSample * channels);
                    for (int sample = 0; sample < sampleCount; sample++)
                    {
                        int start = sample * bytesPerSample * channels;
                        double in1;
                        double in2;
                        ReadSamples(sourceBuffer, start, out in1, out in2);
                        simpleCompressor.Process(ref in1, ref in2);
                        WriteSamples(array, offset + start, in1, in2);
                    }
                    return count;
                }
                else
                {
                    return sourceStream.Read(array, offset, count);
                }
            }

        }

        /// <summary>
        /// Disposes this stream
        /// </summary>
        /// <param name="disposing">true if the user called this</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources.
                if (sourceStream != null)
                {
                    sourceStream.Dispose();
                    sourceStream = null;
                }
            }
            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the block alignment for this stream
        /// </summary>
        public override int BlockAlign
        {
            get
            {
                // TODO: investigate forcing 20ms
                return sourceStream.BlockAlign;
            }
        }
    }
}

