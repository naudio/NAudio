using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NAudio.Wave
{
    /// <summary>
    /// MPEG Layer flags
    /// </summary>
    public enum MpegLayer
    {
        /// <summary>
        /// Reserved
        /// </summary>
        Reserved,
        /// <summary>
        /// Layer 3
        /// </summary>
        Layer3,
        /// <summary>
        /// Layer 2
        /// </summary>
        Layer2,
        /// <summary>
        /// Layer 1
        /// </summary>
        Layer1
    }

    /// <summary>
    /// MPEG Version Flags
    /// </summary>
    public enum MpegVersion
    {
        /// <summary>
        /// Version 2.5
        /// </summary>
        Version25,
        /// <summary>
        /// Reserved
        /// </summary>
        Reserved,
        /// <summary>
        /// Version 2
        /// </summary>
        Version2,
        /// <summary>
        /// Version 1
        /// </summary>
        Version1
    }

    /// <summary>
    /// Channel Mode
    /// </summary>
    public enum ChannelMode
    {
        /// <summary>
        /// Stereo
        /// </summary>
        Stereo,
        /// <summary>
        /// Joint Stereo
        /// </summary>
        JointStereo,
        /// <summary>
        /// Dual Channel
        /// </summary>
        DualChannel,
        /// <summary>
        /// Mono
        /// </summary>
        Mono
    }

    /// <summary>
    /// Represents an MP3 Frame
    /// </summary>
    public class Mp3Frame
    {
        private static readonly int[] bitRatesLayer1Version1 = new int[] { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448 };
        private static readonly int[] bitRatesLayer1Version2 = new int[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256 };
        private static readonly int[] bitRatesLayer2Version1 = new int[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384 };
        private static readonly int[] bitRatesLayer3Version1 = new int[] { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 };
        private static readonly int[] bitRatesLayer3Version2 = new int[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160 };
        private static readonly int[] sampleRatesVersion1 = new int[] { 44100, 48000, 32000 };
        private static readonly int[] sampleRatesVersion2 = new int[] { 22050, 24000, 16000 };
        private static readonly int[] sampleRatesVersion25 = new int[] { 11025, 12000, 8000 };

        private int bitRate;
        private bool crcPresent;
        private short crc;
        private int sampleRate;
        private int frameLengthInBytes;
        private byte[] rawData;
        private MpegVersion mpegVersion;
        private MpegLayer layer;
        private ChannelMode channelMode;
        // not sure what the max frame length is, but 2MB should be plenty
        private const int MaxFrameLength = 2 * 1024 * 1024;

        /// <summary>Reads an MP3Frame from a stream</summary>
        /// <remarks>http://mpgedit.org/mpgedit/mpeg_format/mpeghdr.htm 
        /// has some good info</remarks>
        public Mp3Frame(Stream input)
            : this(input, true)
        { }


        /// <summary>Reads an MP3Frame from a stream</summary>
        /// <remarks>http://mpgedit.org/mpgedit/mpeg_format/mpeghdr.htm 
        /// has some good info</remarks>
        /// <exception cref="EndOfStreamException">Thrown when we reach the end of the stream without reading a valid frame</exception>
        public Mp3Frame(Stream input, bool readData)
        {
            BinaryReader reader = new BinaryReader(input);
            // try for a header
            long headerStartPosition = input.Position;
            byte[] headerBytes = reader.ReadBytes(4);
            if (headerBytes.Length < 4)
            {
                throw new EndOfStreamException();
            }

            // Added -jam to play wrapped mp3 files via RIFF
            headerBytes = CheckForRiff(input, reader, headerBytes);

            while (!IsValidHeader(headerBytes))
            {
                //headerStartPosition++;
                byte extra = reader.ReadByte(); // n.b can throw an end of stream exception
                // shift down by one and try again
                headerBytes[0] = headerBytes[1];
                headerBytes[1] = headerBytes[2];
                headerBytes[2] = headerBytes[3];
                headerBytes[3] = extra;
            }
            /* no longer read the CRC since we include this in framelengthbytes
            if (this.crcPresent)
                this.crc = reader.ReadInt16();*/

            this.rawData = new byte[this.frameLengthInBytes];
            Array.Copy(headerBytes, this.rawData, 4);
            int bytesRead = reader.Read(rawData, 4, this.frameLengthInBytes - 4);
            if (bytesRead < this.frameLengthInBytes - 4)
            {
                throw new EndOfStreamException();
            }
        }

        /// <summary>checks if the four bytes represent a valid header,
        /// if they are, will parse the values into local properties
        /// </summary>
        private bool IsValidHeader(byte[] headerBytes)
        {
            if ((headerBytes[0] == 0xFF) && ((headerBytes[1] & 0xE0) == 0xE0))
            {
                // TODO: could do with a bitstream class here
                mpegVersion = (MpegVersion)((headerBytes[1] & 0x18) >> 3);
                if (mpegVersion == MpegVersion.Reserved)
                {
                    //throw new FormatException("Unsupported MPEG Version");
                    return false;
                }

                layer = (MpegLayer)((headerBytes[1] & 0x06) >> 1);
                if (layer != MpegLayer.Layer3)
                {
                    //throw new FormatException("Not an MP3");
                    //return false;
                }
                crcPresent = (headerBytes[1] & 0x01) == 0x00;
                int bitRateIndex = (headerBytes[2] & 0xF0) >> 4;
                if (bitRateIndex == 15)
                {
                    // invalid index
                    return false;
                }
                if (this.layer == MpegLayer.Layer1 && this.mpegVersion == Wave.MpegVersion.Version1)
                {
                    bitRate = bitRatesLayer1Version1[bitRateIndex];
                }
                else if (this.layer == MpegLayer.Layer1 && this.mpegVersion == Wave.MpegVersion.Version2)
                {
                    bitRate = bitRatesLayer1Version2[bitRateIndex];
                }
                else if (this.layer == MpegLayer.Layer3 && this.mpegVersion == Wave.MpegVersion.Version1)
                {
                    bitRate = bitRatesLayer3Version1[bitRateIndex];                    
                }
                else if (((this.layer == MpegLayer.Layer2) || (this.layer == MpegLayer.Layer3)) && this.mpegVersion == Wave.MpegVersion.Version2)
                {
                    bitRate = bitRatesLayer3Version2[bitRateIndex];
                }
                else if (this.layer == MpegLayer.Layer2 && this.mpegVersion == Wave.MpegVersion.Version1)
                {
                    bitRate = bitRatesLayer2Version1[bitRateIndex];
                }
                else
                {
                    // not supported yet
                    return false;
                }
                if (bitRate == 0)
                {
                    return false;
                }
                int sampleFrequencyIndex = (headerBytes[2] & 0x0C) >> 2;
                if (sampleFrequencyIndex == 3)
                {
                    return false;
                }
                if (mpegVersion == MpegVersion.Version1)
                    sampleRate = sampleRatesVersion1[sampleFrequencyIndex];
                else if (mpegVersion == MpegVersion.Version2)
                    sampleRate = sampleRatesVersion2[sampleFrequencyIndex];
                else // mpegVersion == MpegVersion.Version25
                    sampleRate = sampleRatesVersion25[sampleFrequencyIndex];

                bool padding = (headerBytes[2] & 0x02) == 0x02;
                bool privateBit = (headerBytes[2] & 0x01) == 0x01;
                channelMode = (ChannelMode)((headerBytes[3] & 0xC0) >> 6);
                int channelExtension = (headerBytes[3] & 0x30) >> 4;
                bool copyright = (headerBytes[3] & 0x08) == 0x80;
                bool original = (headerBytes[3] & 0x04) == 0x80;
                int emphasis = (headerBytes[3] & 0x03);

                int nPadding = padding ? 1 : 0;
                if (this.layer == MpegLayer.Layer1)
                {
                    this.frameLengthInBytes = (12000 * bitRate / sampleRate + nPadding) * 4;
                }
                else
                {
                    frameLengthInBytes = (144 * 1000 * bitRate) / sampleRate + nPadding;
                }
                
                if (this.frameLengthInBytes > MaxFrameLength)
                {
                    return false;
                }                
                return true;
            }
            return false;
        }

        private static byte[] CheckForRiff(Stream input, BinaryReader reader, byte[] headerBytes)
        {
            if ((headerBytes[0] == 'R') &&
                (headerBytes[1] == 'I') &&
                (headerBytes[2] == 'F') &&
                (headerBytes[3] == 'F'))
            {
                // Backup 4 bytes
                input.Position -= 4;

                // Now start parsing
                WaveFormat format;
                long dataChunkPosition;
                int dataChunkLength;                

                WaveFileReader.ReadWaveHeader(input, out format, out dataChunkPosition, out dataChunkLength, null);
               
                // Now read the actual mp3 header
                input.Position = dataChunkPosition;
                headerBytes = reader.ReadBytes(4);
            }
            return headerBytes;
        }

        /// <summary>
        /// Sample rate of this frame
        /// </summary>
        public int SampleRate
        {
            get
            {
                return sampleRate;
            }
        }

        /// <summary>
        /// Frame length in bytes
        /// </summary>
        public int FrameLength
        {
            get
            {
                return frameLengthInBytes;
            }
        }

        /// <summary>
        /// Bit Rate
        /// </summary>
        public int BitRate
        {
            get { return bitRate; }
        }

        /// <summary>
        /// Raw frame data
        /// </summary>
        public byte[] RawData
        {
            get { return rawData; }
        }

        /// <summary>
        /// MPEG Version
        /// </summary>
        public MpegVersion MpegVersion
        {
            get { return mpegVersion; }
        }

        /// <summary>
        /// MPEG Layer
        /// </summary>
        public MpegLayer MpegLayer
        {
            get { return layer; }
        }

        /// <summary>
        /// Channel Mode
        /// </summary>
        public ChannelMode ChannelMode
        {
            get { return channelMode; }
        }

    }
}
