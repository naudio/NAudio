using System;
using System.IO;
using NAudio.Wave.SampleProviders;

// ReSharper disable once CheckNamespace
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
        private WaveStream readerStream; // the waveStream which we will use for all positioning
        private readonly SampleChannel sampleChannel; // sample provider that gives us most stuff we need
        private readonly int destBytesPerSample;
        private readonly int sourceBytesPerSample;
        private readonly long length;
        private readonly object lockObject;

        private readonly AudioFileExtensions audioFileExt = new AudioFileExtensions();

        /// <summary>
        /// Initializes a new instance of AudioFileReader
        /// </summary>
        /// <param name="fileName">The file to open</param>
        public AudioFileReader(string fileName)
        {
            lockObject = new object();
            FileName = fileName;
            CreateReaderStreamFromFileName(fileName);
            sourceBytesPerSample = (readerStream.WaveFormat.BitsPerSample / 8) * readerStream.WaveFormat.Channels;
            sampleChannel = new SampleChannel(readerStream, false);
            destBytesPerSample = 4*sampleChannel.WaveFormat.Channels;
            length = SourceToDest(readerStream.Length);
        }

        /// <summary>
        /// Initializes a new instance of AudioFileReader
        /// </summary>
        /// <param name="stream">The stream containing the audio file data</param>
        /// <param name="fileExt">The extension of the audio file, including the period ('.')</param>
        public AudioFileReader(Stream stream, string fileExt)
        {
            lockObject = new object();
            FileName = null;
            CreateReaderStreamFromStream(stream, fileExt);
            sourceBytesPerSample = (readerStream.WaveFormat.BitsPerSample / 8) * readerStream.WaveFormat.Channels;
            sampleChannel = new SampleChannel(readerStream, false);
            destBytesPerSample = 4 * sampleChannel.WaveFormat.Channels;
            length = SourceToDest(readerStream.Length);
        }

        /// <summary>
        /// Creates the reader stream, supporting all filetypes in the core NAudio library,
        /// and ensuring we are in PCM format
        /// </summary>
        /// <param name="fileName">File Name</param>
        private void CreateReaderStreamFromFileName(string fileName)
        {
            var fileExt = Path.GetExtension(fileName);
            var fileFormat = audioFileExt.GetFormatFromFileExt(fileExt);

            if (fileFormat == AudioFileFormatEnum.WAV)
            {
                readerStream = new WaveFileReader(fileName);
                if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                {
                    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                    readerStream = new BlockAlignReductionStream(readerStream);
                }
            }
            else if (fileFormat == AudioFileFormatEnum.MP3)
            {
                if (Environment.OSVersion.Version.Major < 6)
                    readerStream = new Mp3FileReader(fileName);
                else // make MediaFoundationReader the default for MP3 going forwards
                    readerStream = new MediaFoundationReader(fileName);
            }
            else if (fileFormat == AudioFileFormatEnum.AIFF)
            {
                readerStream = new AiffFileReader(fileName);
            }
            else
            {
                // fall back to media foundation reader, see if that can play it
                readerStream = new MediaFoundationReader(fileName);
            }
        }

        /// <summary>
        /// Creates the reader stream, supporting all filetypes in the core NAudio library,
        /// and ensuring we are in PCM format
        /// </summary>
        /// <param name="stream">The stream that contains the audio file data</param>
        /// <param name="fileExt">The sound file extension including the period ('.').  Example: ".mp3"</param>
        private void CreateReaderStreamFromStream(Stream stream, string fileExt)
        {
            if(stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (string.IsNullOrEmpty(fileExt) == false)
            {
                if (fileExt.StartsWith(".") == false)
                {
                    throw new ArgumentOutOfRangeException("File extension expected to start with a period ('.')");
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(fileExt));
            }

            var fileFormat = audioFileExt.GetFormatFromFileExt(fileExt);

            if (fileFormat == AudioFileFormatEnum.WAV)
            {
                readerStream = new WaveFileReader(stream);
                if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                {
                    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                    readerStream = new BlockAlignReductionStream(readerStream);
                }
            }
            else if (fileFormat == AudioFileFormatEnum.MP3)
            {
                if (Environment.OSVersion.Version.Major < 6)
                    readerStream = new Mp3FileReader(stream);
                else // make MediaFoundationReader the default for MP3 going forwards
                    readerStream = new StreamMediaFoundationReader(stream);
            }
            else if (fileFormat == AudioFileFormatEnum.AIFF)
            {
                readerStream = new AiffFileReader(stream);
            }
            else
            {
                // fall back to media foundation reader, see if that can play it
                readerStream = new StreamMediaFoundationReader(stream);
            }
        }

        /// <summary>
        /// File Name.  Value is null when a stream is used to construct the reader
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat => sampleChannel.WaveFormat;

        /// <summary>
        /// Length of this stream (in bytes)
        /// </summary>
        public override long Length => length;

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
            var waveBuffer = new WaveBuffer(buffer);
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
                return sampleChannel.Read(buffer, offset, count);
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
                if (readerStream != null) {
                    readerStream.Dispose();
                    readerStream = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
