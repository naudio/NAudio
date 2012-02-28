using System;
using System.IO;
using System.Collections.Generic;
using NAudio.Utils;
using System.Diagnostics;
using NAudio.FileFormats.Mp3;

namespace NAudio.Wave
{
    class Mp3Index
    {
        public long FilePosition { get; set; }
        public int SamplePosition { get; set; }
        public int SampleCount { get; set; }
        public int ByteCount { get; set; }
    }

    /// <summary>
    /// Class for reading from MP3 files
    /// </summary>
    public class Mp3FileReader : WaveStream
    {
        private WaveFormat waveFormat;
        private Stream mp3Stream;
        private long mp3DataLength;
        private long dataStartPosition;
        private int frameLengthInBytes;

        /// <summary>
        /// The MP3 wave format (n.b. NOT the output format of this stream - see the WaveFormat property)
        /// </summary>
        public Mp3WaveFormat Mp3WaveFormat { get; private set; }

        private Id3v2Tag id3v2Tag;
        private XingHeader xingHeader;
        private byte[] id3v1Tag;
        private bool ownInputStream;

        private List<Mp3Index> tableOfContents;
        private int tocIndex;

        private int sampleRate;
        private int totalSamples;
        private int bytesPerSample;

        private IMp3FrameDecompressor decompressor;
        
        private byte[] decompressBuffer;
        private int decompressBufferOffset;
        private int decompressLeftovers;

        private object repositionLock = new object();

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
            Mp3Frame mp3Frame = Mp3Frame.LoadFromStream(mp3Stream);
            sampleRate = mp3Frame.SampleRate;
            frameLengthInBytes = mp3Frame.FrameLength;
            bitRate = mp3Frame.BitRate;
            xingHeader = XingHeader.LoadXingHeader(mp3Frame);
            // If the header exists, we can skip over it when decoding the rest of the file
            if (xingHeader != null) dataStartPosition = mp3Stream.Position;

            this.mp3DataLength = mp3Stream.Length - dataStartPosition;

            // try for an ID3v1 tag as well
            mp3Stream.Position = mp3Stream.Length - 128;
            byte[] tag = new byte[128];
            mp3Stream.Read(tag, 0, 3);
            if (tag[0] == 'T' && tag[1] == 'A' && tag[2] == 'G')
            {
                id3v1Tag = tag;
                this.mp3DataLength -= 128;
            }

            mp3Stream.Position = dataStartPosition;

            CreateTableOfContents();
            this.tocIndex = 0;

            // [Bit rate in Kilobits/sec] = [Length in kbits] / [time in seconds] 
            //                            = [Length in bits ] / [time in milliseconds]
            
            // Note: in audio, 1 kilobit = 1000 bits.
            bitRate = (mp3DataLength * 8.0 / TotalSeconds());

            mp3Stream.Position = dataStartPosition;

            this.Mp3WaveFormat = new Mp3WaveFormat(sampleRate, mp3Frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frameLengthInBytes, (int)bitRate);
            decompressor = new AcmMp3FrameDecompressor(this.Mp3WaveFormat); // new DmoMp3FrameDecompressor(this.Mp3WaveFormat); 
            this.waveFormat = decompressor.OutputFormat;
            this.bytesPerSample = (decompressor.OutputFormat.BitsPerSample) / 8 * decompressor.OutputFormat.Channels;
            // no MP3 frames have more than 1152 samples in them
            // some MP3s I seem to get double
            this.decompressBuffer = new byte[1152 * bytesPerSample * 2];
        }

        private void CreateTableOfContents()
        {
            try
            {
                // Just a guess at how many entries we'll need so the internal array need not resize very much
                // 400 bytes per frame is probably a good enough approximation.
                tableOfContents = new List<Mp3Index>((int)(mp3DataLength / 400));
                Mp3Frame frame = null;
                this.totalSamples = 0;
                do
                {
                    Mp3Index index = new Mp3Index();
                    index.FilePosition = mp3Stream.Position;
                    index.SamplePosition = totalSamples;
                    frame = ReadNextFrame(false);
                    if (frame != null)
                    {
                        totalSamples += frame.SampleCount;
                        index.SampleCount = frame.SampleCount;
                        index.ByteCount = (int)(mp3Stream.Position - index.FilePosition);
                        tableOfContents.Add(index);
                    }
                } while (frame != null);
            }
            catch (EndOfStreamException)
            {
                // not necessarily a problem
            }
        }

        /// <summary>
        /// Gets the total length of this file in milliseconds.
        /// </summary>
        private double TotalSeconds()
        {
            return (double)this.totalSamples / sampleRate;
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
        private Mp3Frame ReadNextFrame(bool readData)
        {
            Mp3Frame frame = null;
            try
            {
                frame = Mp3Frame.LoadFromStream(mp3Stream, readData);
                if (frame != null)
                {
                    tocIndex++;
                }
            }
            catch (EndOfStreamException)
            {
                // suppress for now - it means we unexpectedly got to the end of the stream
                // half way through
            }
            return frame;
        }

        /// <summary>
        /// This is the length in bytes of data available to be read out from the Read method
        /// (i.e. the decompressed MP3 length)
        /// n.b. this may return 0 for files whose length is unknown
        /// </summary>
        public override long Length
        {
            get
            {
                return this.totalSamples * this.bytesPerSample; // assume converting to 16 bit (n.b. may have to check if this includes) //length;
            }
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// <see cref="Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                if (tocIndex >= tableOfContents.Count)
                {
                    return this.Length;
                }
                else
                {
                    return (tableOfContents[tocIndex].SamplePosition * this.bytesPerSample) + decompressBufferOffset;
                }
            }
            set
            {
                lock (repositionLock)
                {
                    value = Math.Max(Math.Min(value, Length), 0);
                    var samplePosition = value / this.bytesPerSample;
                    Mp3Index mp3Index = null;
                    for (int index = 0; index < tableOfContents.Count; index++)
                    {
                        if (tableOfContents[index].SamplePosition >= samplePosition)
                        {
                            mp3Index = tableOfContents[index];
                            tocIndex = index;
                            break;
                        }
                    }
                    if (mp3Index != null)
                    {
                        // perform the reposition
                        mp3Stream.Position = mp3Index.FilePosition;
                    }
                    else
                    {
                        // we are repositioning to the end of the data
                        mp3Stream.Position = mp3DataLength + dataStartPosition;
                    }
                    decompressBufferOffset = 0;
                    decompressLeftovers = 0;                    
                }
            }
        }

        /// <summary>
        /// Reads decompressed PCM data from our MP3 file.
        /// </summary>
        public override int Read(byte[] sampleBuffer, int offset, int numBytes)
        {
            int bytesRead = 0;
            lock (repositionLock)
            {
                if (decompressLeftovers != 0)
                {
                    int toCopy = Math.Min(decompressLeftovers, numBytes);
                    Array.Copy(decompressBuffer, decompressBufferOffset, sampleBuffer, offset, toCopy);
                    decompressLeftovers -= toCopy;
                    if (decompressLeftovers == 0)
                    {
                        decompressBufferOffset = 0;
                    }
                    else
                    {
                        decompressBufferOffset += toCopy;
                    }
                    bytesRead += toCopy;
                    offset += toCopy;
                }

                while (bytesRead < numBytes)
                {
                    Mp3Frame frame = ReadNextFrame();
                    if (frame != null)
                    {
                        int decompressed = decompressor.DecompressFrame(frame, decompressBuffer, 0);
                        int toCopy = Math.Min(decompressed, numBytes - bytesRead);
                        Array.Copy(decompressBuffer, 0, sampleBuffer, offset, toCopy);
                        if (toCopy < decompressed)
                        {
                            decompressBufferOffset = toCopy;
                            decompressLeftovers = decompressed - toCopy;
                        }
                        else
                        {
                            // no lefovers
                            decompressBufferOffset = 0;
                            decompressLeftovers = 0;
                        }
                        offset += toCopy;
                        bytesRead += toCopy;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            Debug.Assert(bytesRead <= numBytes, "MP3 File Reader read too much");
            return bytesRead;
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
                if (decompressor != null)
                {
                    decompressor.Dispose();
                    decompressor = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
