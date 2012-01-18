using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave.SampleProviders;

namespace NAudio.Wave
{
    /// <summary>
    /// AudioFileReader simplifies opening an audio file in NAudio
    /// Simply pass in the filename, and it will attempt to open the
    /// file and set up a conversion path that turns into PCM IEEE float.
    /// ACM codecs will be used for conversion.
    /// It provides a volume property and implements both WaveStream and
    /// ISampleProvider, making it possibly the only stage in your audio
    /// pipeline necessary for simple playback scenarios
    /// </summary>
    public class AudioFileReader : WaveStream, ISampleProvider
    {
        private string fileName;
        private WaveStream readerStream; // the waveStream which we will use for all positioning
        private SampleChannel sampleChannel; // sample provider that gives us most stuff we need
        private int destBytesPerSample;
        private int sourceBytesPerSample;
        private long length;
        private object lockObject;

        /// <summary>
        /// Initializes a new instance of AudioFileReader
        /// </summary>
        /// <param name="fileName">The file to open</param>
        public AudioFileReader(string fileName)
        {
            this.fileName = fileName;
            CreateReaderStream(fileName);
            this.sourceBytesPerSample = (readerStream.WaveFormat.BitsPerSample / 8) * readerStream.WaveFormat.Channels;
            this.destBytesPerSample = 8; // stereo float
            this.sampleChannel = new SampleChannel(readerStream);
            this.length = SourceToDest(readerStream.Length);
            this.lockObject = new object();
        }

        /// <summary>
        /// Creates the reader stream, supporting all filetypes in the core NAudio library,
        /// and ensuring we are in PCM format
        /// </summary>
        /// <param name="fileName">File Name</param>
        private void CreateReaderStream(string fileName)
        {
            if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                readerStream = new WaveFileReader(fileName);
                if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                {
                    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                    readerStream = new BlockAlignReductionStream(readerStream);
                }
            }
            else if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                readerStream = new Mp3FileReader(fileName);
            }
            else if (fileName.EndsWith(".aiff"))
            {
                readerStream = new AiffFileReader(fileName);
            }
        }

        /// <summary>
        /// WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sampleChannel.WaveFormat; }
        }

        /// <summary>
        /// Length of this stream (in bytes)
        /// </summary>
        public override long Length
        {
            get { return this.length; }
        }

        /// <summary>
        /// Position of this stream (in bytes)
        /// </summary>
        public override long Position
        {
            get { return SourceToDest(readerStream.Position); }
            set { lock (lockObject) { readerStream.Position = DestToSource(value); }  }
        }

        /// <summary>
        /// Reads from this wave stream
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <param name="offset">Offset into buffer</param>
        /// <param name="count">Number of bytes required</param>
        /// <returns>Number of bytes read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            WaveBuffer waveBuffer = new WaveBuffer(buffer);
            int samplesRequired = count / 4;
            int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
            return samplesRead * 4;
        }

        /// <summary>
        /// Reads audio from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            lock (lockObject)
            {
                return this.sampleChannel.Read(buffer, offset, count);
            }
        }

        /// <summary>
        /// Gets or Sets the Volume of this AudioFileReader. 1.0f is full volume
        /// </summary>
        public float Volume
        {
            get { return sampleChannel.Volume; }
            set { sampleChannel.Volume = value; } 
        }

        /// <summary>
        /// Helper to convert source to dest bytes
        /// </summary>
        private long SourceToDest(long sourceBytes)
        {
            return destBytesPerSample * (sourceBytes / sourceBytesPerSample);
        }

        /// <summary>
        /// Helper to convert dest to source bytes
        /// </summary>
        private long DestToSource(long destBytes)
        {
            return sourceBytesPerSample * (destBytes / destBytesPerSample);
        }

        /// <summary>
        /// Disposes this AudioFileReader
        /// </summary>
        /// <param name="disposing">True if called from Dispose</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.readerStream.Dispose();
                this.readerStream = null;
            }
            base.Dispose(disposing);
        }
    }
}
