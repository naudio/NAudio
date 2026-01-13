using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
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
    public class Mp3FileReaderBase : WaveStream
    {
        private readonly WaveFormat waveFormat;
        private Stream mp3Stream;
        private readonly long mp3DataLength;
        private readonly long dataStartPosition;
        
        /// <summary>
        /// The MP3 wave format (n.b. NOT the output format of this stream - see the WaveFormat property)
        /// </summary>
        public Mp3WaveFormat Mp3WaveFormat { get; private set; }

        private readonly XingHeader xingHeader;
        private readonly bool ownInputStream;

        private List<Mp3Index> tableOfContents;
        private int tocIndex;

        private long totalSamples;
        private readonly int bytesPerSample;
        private readonly int bytesPerDecodedFrame;

        private IMp3FrameDecompressor decompressor;
        
        private readonly byte[] decompressBuffer;
        private int decompressBufferOffset;
        private int decompressLeftovers;
        private bool repositionedFlag;

        private long position; // decompressed data position tracker

        private readonly object repositionLock = new object();


        /// <summary>Supports opening a MP3 file</summary>
        /// <param name="mp3FileName">MP3 File name</param>
        /// <param name="frameDecompressorBuilder">Factory method to build a frame decompressor</param>
        public Mp3FileReaderBase(string mp3FileName, FrameDecompressorBuilder frameDecompressorBuilder)
            : this(File.OpenRead(mp3FileName), frameDecompressorBuilder, true)
        {
        }



        /// <summary>
        /// Opens MP3 from a stream rather than a file
        /// Will not dispose of this stream itself
        /// </summary>
        /// <param name="inputStream">The incoming stream containing MP3 data</param>
        /// <param name="frameDecompressorBuilder">Factory method to build a frame decompressor</param>
        public Mp3FileReaderBase(Stream inputStream, FrameDecompressorBuilder frameDecompressorBuilder)
            : this(inputStream, frameDecompressorBuilder, false)
        {
            
        }

        /// <summary>
        /// Constructor that takes an input stream and a frame decompressor builder
        /// </summary>
        /// <param name="inputStream">Input stream</param>
        /// <param name="frameDecompressorBuilder">Factory method to build a frame decompressor</param>
        /// <param name="ownInputStream">Whether we own the stream and should dispose it</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected Mp3FileReaderBase(Stream inputStream, FrameDecompressorBuilder frameDecompressorBuilder, bool ownInputStream)
        {
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
            if (frameDecompressorBuilder == null) throw new ArgumentNullException(nameof(frameDecompressorBuilder));
            this.ownInputStream = ownInputStream;
            try
            {
                mp3Stream = inputStream;
                Id3v2Tag = Id3v2Tag.ReadTag(mp3Stream);

                dataStartPosition = mp3Stream.Position;
                var firstFrame = Mp3Frame.LoadFromStream(mp3Stream);
                if (firstFrame == null)
                    throw new InvalidDataException("Invalid MP3 file - no MP3 Frames Detected");
                double bitRate = firstFrame.BitRate;
                xingHeader = XingHeader.LoadXingHeader(firstFrame);
                // If the header exists, we can skip over it when decoding the rest of the file
                if (xingHeader != null) dataStartPosition = mp3Stream.Position;

                // workaround for a longstanding issue with some files failing to load
                // because they report a spurious sample rate change
                var secondFrame = Mp3Frame.LoadFromStream(mp3Stream);
                if (secondFrame != null &&
                    (secondFrame.SampleRate != firstFrame.SampleRate ||
                     secondFrame.ChannelMode != firstFrame.ChannelMode))
                {
                    // assume that the first frame was some kind of VBR/LAME header that we failed to recognise properly
                    dataStartPosition = secondFrame.FileOffset;
                    // forget about the first frame, the second one is the first one we really care about
                    firstFrame = secondFrame;
                }

                mp3DataLength = mp3Stream.Length - dataStartPosition;

                // try for an ID3v1 tag as well
                mp3Stream.Position = mp3Stream.Length - 128;
                byte[] tag = new byte[128];
                mp3Stream.Read(tag, 0, 128);
                if (tag[0] == 'T' && tag[1] == 'A' && tag[2] == 'G')
                {
                    Id3v1Tag = tag;
                    mp3DataLength -= 128;
                }

                mp3Stream.Position = dataStartPosition;

                // create a temporary MP3 format before we know the real bitrate
                Mp3WaveFormat = new Mp3WaveFormat(firstFrame.SampleRate,
                    firstFrame.ChannelMode == ChannelMode.Mono ? 1 : 2, firstFrame.FrameLength, (int) bitRate);

                CreateTableOfContents();
                tocIndex = 0;

                // [Bit rate in Kilobits/sec] = [Length in kbits] / [time in seconds] 
                //                            = [Length in bits ] / [time in milliseconds]

                // Note: in audio, 1 kilobit = 1000 bits.
                // Calculated as a double to minimize rounding errors
                bitRate = (mp3DataLength*8.0/TotalSeconds());

                mp3Stream.Position = dataStartPosition;

                // now we know the real bitrate we can create an accurate MP3 WaveFormat
                Mp3WaveFormat = new Mp3WaveFormat(firstFrame.SampleRate,
                    firstFrame.ChannelMode == ChannelMode.Mono ? 1 : 2, firstFrame.FrameLength, (int) bitRate);
                decompressor = frameDecompressorBuilder(Mp3WaveFormat);
                waveFormat = decompressor.OutputFormat;
                bytesPerSample = (decompressor.OutputFormat.BitsPerSample)/8*decompressor.OutputFormat.Channels;
                // no MP3 frames have more than 1152 samples in them
                bytesPerDecodedFrame = 1152 * bytesPerSample;
                // some MP3s I seem to get double
                decompressBuffer = new byte[bytesPerDecodedFrame * 2];
            }
            catch (Exception)
            {
                if (ownInputStream) inputStream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Function that can create an MP3 Frame decompressor
        /// </summary>
        /// <param name="mp3Format">A WaveFormat object describing the MP3 file format</param>
        /// <returns>An MP3 Frame decompressor</returns>
        public delegate IMp3FrameDecompressor FrameDecompressorBuilder(WaveFormat mp3Format);

        private void CreateTableOfContents()
        {
            try
            {
                // Just a guess at how many entries we'll need so the internal array need not resize very much
                // 400 bytes per frame is probably a good enough approximation.
                tableOfContents = new List<Mp3Index>((int)(mp3DataLength / 400));
                Mp3Frame frame;
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
            return (double)totalSamples / Mp3WaveFormat.SampleRate;
        }

        /// <summary>
        /// ID3v2 tag if present
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public Id3v2Tag Id3v2Tag { get; }

        /// <summary>
        /// ID3v1 tag if present
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public byte[] Id3v1Tag { get; }

        /// <summary>
        /// Reads the next mp3 frame
        /// </summary>
        /// <returns>Next mp3 frame, or null if EOF</returns>
        public Mp3Frame ReadNextFrame()
        {
            var frame = ReadNextFrame(true);
            if (frame != null) position += frame.SampleCount*bytesPerSample;
            return frame;
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
        public override long Length => totalSamples * bytesPerSample;

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// <see cref="Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                lock (repositionLock)
                {
                    value = Math.Max(Math.Min(value, Length), 0);
                    var samplePosition = value / bytesPerSample;
                    Mp3Index mp3Index = null;
                    for (int index = 0; index < tableOfContents.Count; index++)
                    {
                        if (tableOfContents[index].SamplePosition + tableOfContents[index].SampleCount > samplePosition)
                        {
                            mp3Index = tableOfContents[index];
                            tocIndex = index;
                            break;
                        }
                    }

                    decompressBufferOffset = 0;
                    decompressLeftovers = 0;
                    repositionedFlag = true;

                    if (mp3Index != null)
                    {
                        // perform the reposition
                        mp3Stream.Position = mp3Index.FilePosition;

                        // set the offset into the buffer (that is yet to be populated in Read())
                        var frameOffset = samplePosition - mp3Index.SamplePosition;
                        if (frameOffset > 0)
                        {
                            decompressBufferOffset = (int)frameOffset * bytesPerSample;
                        }
                    }
                    else
                    {
                        // we are repositioning to the end of the data
                        mp3Stream.Position = mp3DataLength + dataStartPosition;
                    }

                    position = value;
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

                int targetTocIndex = tocIndex; // the frame index that contains the requested data

                if (repositionedFlag)
                {
                    decompressor.Reset();

                    // Seek back a few frames of the stream to get the reset decoder decode a few
                    // warm-up frames before reading the requested data. Without the warm-up phase,
                    // the first half of the frame after the reset is attenuated and does not resemble
                    // the data as it would be when reading sequentially from the beginning, because 
                    // the decoder is missing the required overlap from the previous frame.
                    tocIndex = Math.Max(0, tocIndex - 3); // no warm-up at the beginning of the stream
                    mp3Stream.Position = tableOfContents[tocIndex].FilePosition;

                    repositionedFlag = false;
                }

                while (bytesRead < numBytes)
                {
                    Mp3Frame frame = ReadNextFrame(true); // internal read - should not advance position
                    if (frame != null)
                    {
                        int decompressed = decompressor.DecompressFrame(frame, decompressBuffer, 0);

                        if (tocIndex <= targetTocIndex || decompressed == 0)
                        {
                            // The first frame after a reset usually does not immediately yield decoded samples.
                            // Because the next instructions will fail if a buffer offset is set and the frame 
                            // decoding didn't return data, we skip the part.
                            // We skip the following instructions also after decoding a warm-up frame.
                            continue;
                        }
                        // Two special cases can happen here:
                        // 1. We are interested in the first frame of the stream, but need to read the second frame too
                        //    for the decoder to return decoded data
                        // 2. We are interested in the second frame of the stream, but because reading the first frame
                        //    as warm-up didn't yield any data (because the decoder needs two frames to return data), we
                        //    get data from the first and second frame. 
                        //    This case needs special handling, and we have to purge the data of the first frame.
                        else if (tocIndex == targetTocIndex + 1 && decompressed == bytesPerDecodedFrame * 2)
                        {
                            // Purge the first frame's data
                            Array.Copy(decompressBuffer, bytesPerDecodedFrame, decompressBuffer, 0, bytesPerDecodedFrame);
                            decompressed = bytesPerDecodedFrame;
                        }

                        int toCopy = Math.Min(decompressed - decompressBufferOffset, numBytes - bytesRead);
                        Array.Copy(decompressBuffer, decompressBufferOffset, sampleBuffer, offset, toCopy);
                        if ((toCopy + decompressBufferOffset) < decompressed)
                        {
                            decompressBufferOffset = toCopy + decompressBufferOffset;
                            decompressLeftovers = decompressed - decompressBufferOffset;
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
            position += bytesRead;
            return bytesRead;
        }

        /// <summary>
        /// Xing header if present
        /// </summary>
        public XingHeader XingHeader => xingHeader;

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
