using System;
using System.IO;
using System.Collections.Generic;

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

        private List<long> tableOfContents;

        private int sampleRate;

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
            // Calculated as a double to minimize rounding errors
            double bitRate;

            mp3Stream = inputStream;
            id3v2Tag = Id3v2Tag.ReadTag(mp3Stream);

            dataStartPosition = mp3Stream.Position;
            Mp3Frame mp3Frame = new Mp3Frame(mp3Stream);
            sampleRate = mp3Frame.SampleRate;
            frameLengthInBytes = mp3Frame.FrameLength;
            bitRate = mp3Frame.BitRate;
            xingHeader = XingHeader.LoadXingHeader(mp3Frame);
            // If the header exists, we can skip over it when decoding the rest of the file
            if (xingHeader != null) dataStartPosition = mp3Stream.Position;

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

            CreateTableOfContents();
 
            // File length, in milliseconds:
            double seconds = TotalMilliseconds() / 1000.0;

            // [Bit rate in Kilobits/sec] = [Length in kbits] / [time in seconds] 
            //                            = [Length in bits ] / [time in milliseconds]
            
            // Note: in audio, 1 kilobit = 1000 bits.
            bitRate = (length * 8.0 / seconds);

            mp3Stream.Position = dataStartPosition;

            waveFormat = new Mp3WaveFormat(sampleRate, mp3Frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frameLengthInBytes, (int)bitRate);
        }

        private void CreateTableOfContents()
        {
            try
            {
                // Just a guess at how many entries we'll need so the internal array need not resize very much
                // 400 bytes per frame is probably a good enough approximation.
                tableOfContents = new List<long>((int)(length / 400));
                do
                {
                    tableOfContents.Add(mp3Stream.Position);
                } while (ReadNextFrame(false) != null);
            }
            catch (EndOfStreamException)
            {
                // not necessarily a problem
            }

            // Note that the very last entry is actually the position after the final frame, so the real
            // frame count is toc.Count - 1.
        }

        /// <summary>
        /// Gets the total length of this file in milliseconds.
        /// </summary>
        private double TotalMilliseconds()
        {
            // [Frame Count] * [Samples per Frame = 1152 ] / Sampling Rate = time in seconds
            // Multiply by 1000 for ms.
            // Calculated as a double to avoid possible overflow after the first three multiplications.
            double milliseconds = 1000.0 * FrameCount() * 1152.0 / sampleRate;
            return milliseconds;
        }

        /// <summary>
        /// Returns the number of frames in this file.
        /// </summary>
        private int FrameCount()
        {
            return (tableOfContents.Count - 1);
        }

        /// <summary>
        /// ID3v2 tag if present
        /// </summary>
        public Id3v2Tag Id3v2Tag
        {
            get { return id3v2Tag; }
        }

        /// <summary>
        /// ID3v1 tag if present
        /// </summary>
        public byte[] Id3v1Tag
        {
            get { return id3v1Tag; }
        }

        /// <summary>
        /// Reads the next mp3 frame
        /// </summary>
        /// <returns>Next mp3 frame, or null if EOF</returns>
        public Mp3Frame ReadNextFrame()
        {
            return ReadNextFrame(true);
        }

        /// <summary>
        /// Reads the next mp3 frame
        /// </summary>
        /// <returns>Next mp3 frame, or null if EOF</returns>
        public Mp3Frame ReadNextFrame(bool readData)
        {
            if (Position + frameLengthInBytes <= Length)
            {
                return new Mp3Frame(mp3Stream, readData);
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
                    value = Math.Max(Math.Min(value, Length), 0);
                    value += dataStartPosition;

                    // Find the index of the next prior frame in the TOC.
                    int index = tableOfContents.BinarySearch(value);

                    // If index is negative, that means the exact offset isn't in the TOC
                    // Do a bitwise complement to get the next larger offset, and subtract 1 to get the next smaller
                    // Limit the result to >= 0 (though it should be unnecessary)
                    if (index < 0)
                        index = Math.Max(~index - 1, 0);

                    mp3Stream.Position = tableOfContents[index];
                }
            }
        }

        /// <summary>
        /// <see cref="WaveStream.CurrentTime"/>
        /// </summary>
        public override TimeSpan CurrentTime
        {
            get
            {
                int index = tableOfContents.BinarySearch(Position);
                if (index < 0) index = ~index;

                // Again we use a double to avoid possible overflow issues
                return TimeSpan.FromMilliseconds(index * 1152.0 * 1000.0 / sampleRate);
            }
            set
            {
                double milliseconds = value.TotalMilliseconds;
                int frame = (int)(sampleRate * milliseconds / 1000.0 / 1152.0);

                frame = Math.Max(Math.Min(frame, FrameCount()), 0);
                mp3Stream.Position = tableOfContents[frame];
            }
        }

        
        /// <summary>
        /// <see cref="WaveStream.TotalTime"/>
        /// </summary>
        public override TimeSpan TotalTime
        {
            get
            {
                return TimeSpan.FromMilliseconds(TotalMilliseconds());
            }
        }

        /// <summary>
        /// <see cref="Stream.Read"/>
        /// </summary>        
        public override int Read(byte[] sampleBuffer, int offset, int numBytes)
        {
            // MP3 block align is the frame size
            if (numBytes % waveFormat.BlockAlign != 0)
            {
                numBytes -= (numBytes % waveFormat.BlockAlign);
            }
            return mp3Stream.Read(sampleBuffer, offset, numBytes);
        }

        /// <summary>
        /// <see cref="WaveStream.BlockAlign"/>
        /// </summary>
        public override int BlockAlign
        {
            get { return 1; } 
        }

        /// <summary>
        /// Xing header if present
        /// </summary>
        public XingHeader XingHeader
        {
            get { return xingHeader; }
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
