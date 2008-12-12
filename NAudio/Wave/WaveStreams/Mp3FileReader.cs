using System;
using System.IO;

namespace NAudio.Wave
{

    /// <summary>
    /// Class for reading from MP3 files
    /// </summary>
    public class Mp3FileReader : WaveStream
    {
        private WaveFormat waveFormat;
		private Stream mp3Stream;
        private long length;
        private long dataStartPosition;
        private int frameLengthInBytes;

        private Id3v2Tag id3v2Tag;
        private XingHeader xingHeader;
        private byte[] id3v1Tag;
        private bool ownInputStream;

		/// <summary>Supports opening a MP3 file</summary>
        public Mp3FileReader(string mp3FileName) 
            : this(File.OpenRead(mp3FileName))
		{
            ownInputStream = true;
		}

        /// <summary>
        /// Opens MP3 from a stream rather than a file
        /// Will not dispose of this stream itself
        /// </summary>
        /// <param name="inputStream"></param>
        public Mp3FileReader(Stream inputStream)
        {
            int sampleRate;
            int bitRate;

            mp3Stream = inputStream;
            id3v2Tag = Id3v2Tag.ReadTag(mp3Stream);

            dataStartPosition = mp3Stream.Position;
            Mp3Frame mp3Frame = new Mp3Frame(mp3Stream);
            sampleRate = mp3Frame.SampleRate;
            frameLengthInBytes = mp3Frame.FrameLength;
            bitRate = mp3Frame.BitRate;
            xingHeader = XingHeader.LoadXingHeader(mp3Frame);

            this.length = mp3Stream.Length - dataStartPosition;

            // try for an ID3v1 tag as well
            mp3Stream.Position = mp3Stream.Length - 128;
            byte[] tag = new byte[128];
            mp3Stream.Read(tag, 0, 3);
            if (tag[0] == 'T' && tag[1] == 'A' && tag[2] == 'G')
            {
                id3v1Tag = tag;
                this.length -= 128;
            }

            mp3Stream.Position = dataStartPosition;

            // TODO: choose more appropriately
            waveFormat = new Mp3WaveFormat(sampleRate, 2, frameLengthInBytes, bitRate);
        }

        /// <summary>
        /// ID3v2 tag if present
        /// </summary>
        public Id3v2Tag Id3v2Tag
        {
            get
            {
                return id3v2Tag;
            }
        }

        /// <summary>
        /// ID3v1 tag if present
        /// </summary>
        public byte[] Id3v1Tag
        {
            get
            {
                return id3v1Tag;
            }
        }

        /// <summary>
        /// Reads the next mp3 frame
        /// </summary>
        /// <returns>Next mp3 frame, or null if EOF</returns>
        public Mp3Frame ReadNextFrame()
        {
            if (Position < Length)
            {
                return new Mp3Frame(mp3Stream);
            }
            return null;
        }

        /// <summary>
        /// <see cref="Stream.Length"/>
        /// </summary>
        public override long Length
        {
            get
            {
                return length;
            }
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get 
            { 
                return waveFormat; 
            }
        }

        /// <summary>
        /// <see cref="Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                return mp3Stream.Position - dataStartPosition;
            }
            set
            {
                lock (this)
                {
                    value = Math.Min(value, Length);
                    // make sure we don't get out of sync
                    value -= (value % waveFormat.BlockAlign);
                    mp3Stream.Position = value + dataStartPosition;
                }
            }
        }

        /// <summary>
        /// <see cref="Stream.Read"/>
        /// </summary>        
        public override int Read(byte[] sampleBuffer, int offset, int numBytes)
        {
            if (numBytes % waveFormat.BlockAlign != 0)
                //throw new ApplicationException("Must read complete blocks");
                numBytes -= (numBytes % waveFormat.BlockAlign);
            return mp3Stream.Read(sampleBuffer, offset, numBytes);
        }

        /// <summary>
        /// <see cref="WaveStream.GetReadSize"/>
        /// </summary>
        public override int GetReadSize(int milliseconds)
        {
            // TODO! AverageBytesPerSecond is complete guesswork at the moment
            int bytes = (this.WaveFormat.AverageBytesPerSecond / 1000) * milliseconds;
            if (bytes % BlockAlign != 0)
            {
                bytes = bytes / BlockAlign;
                bytes = (bytes + 1) * BlockAlign;
            }
            return bytes;
        }

        /// <summary>
        /// <see cref="WaveStream.BlockAlign"/>
        /// </summary>
        public override int BlockAlign
        {
            get
            {
                return frameLengthInBytes; //Mp3WaveFormat.BlockSize;
            }
        }

        /// <summary>
        /// Xing header if present
        /// </summary>
        public XingHeader XingHeader
        {
            get
            {
                return xingHeader;
            }
        }

        /// <summary>
        /// Disposes this WaveStream
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (mp3Stream != null)
                {
                    if (ownInputStream)
                    {
                        mp3Stream.Dispose();
                    }
                    mp3Stream = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
