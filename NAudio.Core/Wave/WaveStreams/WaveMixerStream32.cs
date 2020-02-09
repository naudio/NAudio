using System;
using System.Collections.Generic;

namespace NAudio.Wave
{
    /// <summary>
    /// WaveStream that can mix together multiple 32 bit input streams
    /// (Normally used with stereo input channels)
    /// All channels must have the same number of inputs
    /// </summary>
    public class WaveMixerStream32 : WaveStream
    {
        private readonly List<WaveStream> inputStreams;
        private readonly object inputsLock;
        private WaveFormat waveFormat;
        private long length;
        private long position;
        private readonly int bytesPerSample;

        /// <summary>
        /// Creates a new 32 bit WaveMixerStream
        /// </summary>
        public WaveMixerStream32()
        {
            AutoStop = true;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            bytesPerSample = 4;
            inputStreams = new List<WaveStream>();
            inputsLock = new object();
        }

        /// <summary>
        /// Creates a new 32 bit WaveMixerStream
        /// </summary>
        /// <param name="inputStreams">An Array of WaveStreams - must all have the same format.
        /// Use WaveChannel is designed for this purpose.</param>
        /// <param name="autoStop">Automatically stop when all inputs have been read</param>
        /// <exception cref="ArgumentException">Thrown if the input streams are not 32 bit floating point,
        /// or if they have different formats to each other</exception>
        public WaveMixerStream32(IEnumerable<WaveStream> inputStreams, bool autoStop)
            : this()
        {
            AutoStop = autoStop;

            foreach (var inputStream in inputStreams)
            {
                AddInputStream(inputStream);
            }
        }

        /// <summary>
        /// Add a new input to the mixer
        /// </summary>
        /// <param name="waveStream">The wave input to add</param>
        public void AddInputStream(WaveStream waveStream)
        {
            if (waveStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Must be IEEE floating point", "waveStream");
            if (waveStream.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Only 32 bit audio currently supported", "waveStream");

            if (inputStreams.Count == 0)
            {
                // first one - set the format
                int sampleRate = waveStream.WaveFormat.SampleRate;
                int channels = waveStream.WaveFormat.Channels;
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            }
            else
            {
                if (!waveStream.WaveFormat.Equals(waveFormat))
                    throw new ArgumentException("All incoming channels must have the same format", "waveStream");
            }

            lock (inputsLock)
            {
                inputStreams.Add(waveStream);
                length = Math.Max(length, waveStream.Length);
                // get to the right point in this input file
                waveStream.Position = Position;
            }
        }

        /// <summary>
        /// Remove a WaveStream from the mixer
        /// </summary>
        /// <param name="waveStream">waveStream to remove</param>
        public void RemoveInputStream(WaveStream waveStream)
        {
            lock (inputsLock)
            {
                if (inputStreams.Remove(waveStream))
                {
                    // recalculate the length
                    long newLength = 0;
                    foreach (var inputStream in inputStreams)
                    {
                        newLength = Math.Max(newLength, inputStream.Length);
                    }
                    length = newLength;
                }
            }
        }

        /// <summary>
        /// The number of inputs to this mixer
        /// </summary>
        public int InputCount => inputStreams.Count;

        /// <summary>
        /// Automatically stop when all inputs have been read
        /// </summary>
        public bool AutoStop { get; set; }

        /// <summary>
        /// Reads bytes from this wave stream
        /// </summary>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="offset">offset into buffer</param>
        /// <param name="count">number of bytes required</param>
        /// <returns>Number of bytes read.</returns>
        /// <exception cref="ArgumentException">Thrown if an invalid number of bytes requested</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (AutoStop)
            {
                if (position + count > length)
                    count = (int)(length - position);

                // was a bug here, should be fixed now
                System.Diagnostics.Debug.Assert(count >= 0, "length and position mismatch");
            }


            if (count % bytesPerSample != 0)
                throw new ArgumentException("Must read an whole number of samples", "count");

            // blank the buffer
            Array.Clear(buffer, offset, count);
            int bytesRead = 0;

            // sum the channels in
            var readBuffer = new byte[count];
            lock (inputsLock)
            {
                foreach (var inputStream in inputStreams)
                {
                    if (inputStream.HasData(count))
                    {
                        int readFromThisStream = inputStream.Read(readBuffer, 0, count);
                        // don't worry if input stream returns less than we requested - may indicate we have got to the end
                        bytesRead = Math.Max(bytesRead, readFromThisStream);
                        if (readFromThisStream > 0)
                            Sum32BitAudio(buffer, offset, readBuffer, readFromThisStream);
                    }
                    else
                    {
                        bytesRead = Math.Max(bytesRead, count);
                        inputStream.Position += count;
                    }
                }
            }
            position += count;
            return count;
        }

        /// <summary>
        /// Actually performs the mixing
        /// </summary>
        static unsafe void Sum32BitAudio(byte[] destBuffer, int offset, byte[] sourceBuffer, int bytesRead)
        {
            fixed (byte* pDestBuffer = &destBuffer[offset],
                      pSourceBuffer = &sourceBuffer[0])
            {
                float* pfDestBuffer = (float*)pDestBuffer;
                float* pfReadBuffer = (float*)pSourceBuffer;
                int samplesRead = bytesRead / 4;
                for (int n = 0; n < samplesRead; n++)
                {
                    pfDestBuffer[n] += pfReadBuffer[n];
                }
            }
        }

        /// <summary>
        /// <see cref="WaveStream.BlockAlign"/>
        /// </summary>
        public override int BlockAlign => waveFormat.BlockAlign;

        /// <summary>
        /// Length of this Wave Stream (in bytes)
        /// <see cref="System.IO.Stream.Length"/>
        /// </summary>
        public override long Length => length;

        /// <summary>
        /// Position within this Wave Stream (in bytes)
        /// <see cref="System.IO.Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                // all streams are at the same position
                return position;
            }
            set
            {
                lock (inputsLock)
                {
                    value = Math.Min(value, Length);
                    foreach (WaveStream inputStream in inputStreams)
                    {
                        inputStream.Position = Math.Min(value, inputStream.Length);
                    }
                    position = value;
                }
            }
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Disposes this WaveStream
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (inputsLock)
                {
                    foreach (WaveStream inputStream in inputStreams)
                    {
                        inputStream.Dispose();
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "WaveMixerStream32 was not disposed");
            }
            base.Dispose(disposing);
        }
    }
}
