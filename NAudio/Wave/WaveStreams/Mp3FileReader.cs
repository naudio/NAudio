using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace NAudio.Wave
{
    class Mp3Index
    {
        public long FilePosition { get; set; }
        public long SamplePosition { get; set; }
        public int SampleCount { get; set; }
        public int ByteCount { get; set; }
    }

    /// <summary>
    /// Class for reading from MP3 files
    /// </summary>
    public class Mp3FileReader : WaveStream
    {
        private readonly WaveFormat waveFormat;
        private Stream mp3Stream;
        private readonly long mp3DataLength;
        private readonly long dataStartPosition;
        private readonly int frameLengthInBytes;

        /// <summary>
        /// The MP3 wave format (n.b. NOT the output format of this stream - see the WaveFormat property)
        /// </summary>
        public Mp3WaveFormat Mp3WaveFormat { get; private set; }

        private readonly Id3v2Tag id3v2Tag;
        private readonly XingHeader xingHeader;
        private readonly byte[] id3v1Tag;
        private readonly bool ownInputStream;

        private List<Mp3Index> tableOfContents;
        private int tocIndex;

        private readonly int sampleRate;
        private long totalSamples;
        private readonly int bytesPerSample;

        private IMp3FrameDecompressor decompressor;
        
        private readonly byte[] decompressBuffer;
        private int decompressBufferOffset;
        private int decompressLeftovers;
        private bool repositionedFlag;

        private readonly object repositionLock = new object();

        /// <summary>Supports opening a MP3 file</summary>
        public Mp3FileReader(string mp3FileName) 
            : this(File.OpenRead(mp3FileName))
        {
            ownInputStream = true;
        }

        /// <summary>Supports opening a MP3 file</summary>
        /// <param name="mp3FileName">MP3 File name</param>
        /// <param name="frameDecompressorBuilder">Factory method to build a frame decompressor</param>
        public Mp3FileReader(string mp3FileName, FrameDecompressorBuilder frameDecompressorBuilder)
            : this(File.OpenRead(mp3FileName), frameDecompressorBuilder)
        {
            ownInputStream = true;
        }

        /// <summary>
        /// Opens MP3 from a stream rather than a file
        /// Will not dispose of this stream itself
        /// </summary>
        /// <param name="inputStream">The incoming stream containing MP3 data</param>
        public Mp3FileReader(Stream inputStream)
            : this (inputStream, CreateAcmFrameDecompressor)
        {
            
        }
        
        /// <summary>
        /// Opens MP3 from a stream rather than a file
        /// Will not dispose of this stream itself
        /// </summary>
        /// <param name="inputStream">The incoming stream containing MP3 data</param>
        /// <param name="frameDecompressorBuilder">Factory method to build a frame decompressor</param>
        public Mp3FileReader(Stream inputStream, FrameDecompressorBuilder frameDecompressorBuilder)
        {
            // Calculated as a double to minimize rounding errors

            mp3Stream = inputStream;
            id3v2Tag = Id3v2Tag.ReadTag(mp3Stream);

            dataStartPosition = mp3Stream.Position;
            var mp3Frame = Mp3Frame.LoadFromStream(mp3Stream);
            sampleRate = mp3Frame.SampleRate;
            frameLengthInBytes = mp3Frame.FrameLength;
            double bitRate = mp3Frame.BitRate;
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
            
            // create a temporary MP3 format before we know the real bitrate
            this.Mp3WaveFormat = new Mp3WaveFormat(sampleRate, mp3Frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frameLengthInBytes, (int)bitRate);
            
            CreateTableOfContents();
            this.tocIndex = 0;

            // [Bit rate in Kilobits/sec] = [Length in kbits] / [time in seconds] 
            //                            = [Length in bits ] / [time in milliseconds]
            
            // Note: in audio, 1 kilobit = 1000 bits.
            bitRate = (mp3DataLength * 8.0 / TotalSeconds());

            mp3Stream.Position = dataStartPosition;

            // now we know the real bitrate we can create an accurate 
            this.Mp3WaveFormat = new Mp3WaveFormat(sampleRate, mp3Frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frameLengthInBytes, (int)bitRate);
            decompressor = frameDecompressorBuilder(Mp3WaveFormat); 
            this.waveFormat = decompressor.OutputFormat;
            this.bytesPerSample = (decompressor.OutputFormat.BitsPerSample) / 8 * decompressor.OutputFormat.Channels;
            // no MP3 frames have more than 1152 samples in them
            // some MP3s I seem to get double
            this.decompressBuffer = new byte[1152 * bytesPerSample * 2];
        }

        /// <summary>
        /// Function that can create an MP3 Frame decompressor
        /// </summary>
        /// <param name="mp3Format">A WaveFormat object describing the MP3 file format</param>
        /// <returns>An MP3 Frame decompressor</returns>
        public delegate IMp3FrameDecompressor FrameDecompressorBuilder(WaveFormat mp3Format);

        /// <summary>
        /// Creates an ACM MP3 Frame decompressor. This is the default with NAudio
        /// </summary>
        /// <param name="mp3Format">A WaveFormat object based </param>
        /// <returns></returns>
        public static IMp3FrameDecompressor CreateAcmFrameDecompressor(WaveFormat mp3Format)
        {
            // new DmoMp3FrameDecompressor(this.Mp3WaveFormat); 
            return new AcmMp3FrameDecompressor(mp3Format);
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
                    var index = new Mp3Index();
                    index.FilePosition = mp3Stream.Position;
                    index.SamplePosition = totalSamples;
                    frame = ReadNextFrame(false);
                    if (frame != null)
                    {
                        ValidateFrameFormat(frame);

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

        private void ValidateFrameFormat(Mp3Frame frame)
        {
            if (frame.SampleRate != Mp3WaveFormat.SampleRate)
            {
                string message =
                    String.Format(
                        "Got a frame at sample rate {0}, in an MP3 with sample rate {1}. Mp3FileReader does not support sample rate changes.",
                        frame.SampleRate, Mp3WaveFormat.SampleRate);
                throw new InvalidOperationException(message);
            }
            int channels = frame.ChannelMode == ChannelMode.Mono ? 1 : 2;
            if (channels != Mp3WaveFormat.Channels)
            {
                string message =
                    String.Format(
                        "Got a frame with channel mode {0}, in an MP3 with {1} channels. Mp3FileReader does not support changes to channel count.",
                        frame.ChannelMode, Mp3WaveFormat.Channels);
                throw new InvalidOperationException(message);
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
                    repositionedFlag = true;
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
                        if (repositionedFlag)
                        {
                            decompressor.Reset();
                            repositionedFlag = false;
                        }
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
