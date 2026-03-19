using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// An ID3v2 Tag
    /// </summary>
    public class Id3v2Tag
    {
        private long tagStartPosition;
        private long tagEndPosition;
        private byte[] rawData;

        /// <summary>
        /// Reads an ID3v2 tag from a stream
        /// </summary>
        public static Id3v2Tag ReadTag(Stream input)
        {
            try
            {
                return new Id3v2Tag(input);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        #region Id3v2 Creation from key-value pairs

        /// <summary>
        /// Creates a new ID3v2 tag from a collection of key-value pairs.
        /// </summary>
        /// <param name="tags">A collection of key-value pairs containing the tags to include in the ID3v2 tag.</param>
        /// <returns>A new ID3v2 tag</returns>
        public static Id3v2Tag Create(IEnumerable<KeyValuePair<string, string>> tags)
        {
            return Id3v2Tag.ReadTag(CreateId3v2TagStream(tags));
        }

        /// <summary>
        /// Convert the frame size to a byte array.
        /// </summary>
        /// <param name="n">The frame body size.</param>
        /// <returns></returns>
        static byte[] FrameSizeToBytes(int n)
        {
            byte[] result = BitConverter.GetBytes(n);
            Array.Reverse(result);
            return result;
        }

        /// <summary>
        /// Creates an ID3v2 frame for the given key-value pair.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        static byte[] CreateId3v2Frame(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException("value");
            }

            if (key.Length != 4)
            {
                throw new ArgumentOutOfRangeException("key", "key " + key + " must be 4 characters long");
            }

            const byte UnicodeEncoding = 01; // encode text in Unicode
            byte[] UnicodeOrder = new byte[] { 0xff, 0xfe }; // Unicode byte order mark
            byte[] language = new byte[] { 0, 0, 0 }; // language is empty (only used in COMM -> comment)
            byte[] shortDescription = new byte[] { 0, 0 }; // short description is empty (only used in COMM -> comment)

            byte[] body;
            if (key == "COMM") // comment
            {
                body = ByteArrayExtensions.Concat(
                    new byte[] { UnicodeEncoding },
                    language,
                    shortDescription,
                    UnicodeOrder,
                    Encoding.Unicode.GetBytes(value));
            }
            else
            {
                body = ByteArrayExtensions.Concat(
                    new byte[] { UnicodeEncoding },
                    UnicodeOrder,
                    Encoding.Unicode.GetBytes(value));
            }

            return ByteArrayExtensions.Concat(
                // needs review - have converted to UTF8 as Win 8 has no Encoding.ASCII, 
                // need to check what the rules are for ID3v2 tag identifiers
                Encoding.UTF8.GetBytes(key),
                FrameSizeToBytes(body.Length),
                new byte[] { 0, 0 }, // flags
                body);
        }

        /// <summary>
        /// Gets the Id3v2 Header size. The size is encoded so that only 7 bits per byte are actually used.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        static byte[] GetId3TagHeaderSize(int size)
        {
            byte[] result = new byte[4];
            for (int idx = result.Length - 1; idx >= 0; idx--)
            {
                result[idx] = (byte)(size % 128);
                size = size / 128;
            }

            return result;
        }

        /// <summary>
        /// Creates the Id3v2 tag header and returns is as a byte array.
        /// </summary>
        /// <param name="size">The sum of all frame sizes included in the tag.</param>
        /// <returns></returns>
        static byte[] CreateId3v2TagHeader(int size)
        {
            byte[] tagHeader = ByteArrayExtensions.Concat(
                Encoding.UTF8.GetBytes("ID3"),
                new byte[] { 3, 0 }, // version
                new byte[] { 0 }, // flags
                GetId3TagHeaderSize(size));
            return tagHeader;
        }

        /// <summary>
        /// Creates the Id3v2 tag for the given key-value pairs and returns it in the a stream.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        static Stream CreateId3v2TagStream(IEnumerable<KeyValuePair<string, string>> tags)
        {
            List<byte[]> frames = new List<byte[]>();
            int framesSize = 0;
            foreach (KeyValuePair<string, string> tag in tags)
            {
                byte[] frame = CreateId3v2Frame(tag.Key, tag.Value);
                frames.Add(frame);
                framesSize += frame.Length;
            }

            byte[] header = CreateId3v2TagHeader(framesSize);

            MemoryStream ms = new MemoryStream(header.Length + framesSize);
            ms.Write(header, 0, header.Length);
            foreach (byte[] frame in frames)
            {
                ms.Write(frame, 0, frame.Length);
            }

            ms.Position = 0;
            return ms;
        }

        #endregion

        /// <summary>
        /// Attempts to skip an ID3v2 tag at the current position in the stream.
        /// </summary>
        /// <param name="input">The stream to read from.</param>
        /// <returns>True if an ID3v2 tag was found and skipped, false otherwise.</returns>
        public static bool TrySkipTag(Stream input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (!input.CanSeek || input.Length - input.Position < 10)
            {
                return false;
            }

            long originalPosition = input.Position;
            var reader = new BinaryReader(input);
            int dataLength;
            int footerLength;
            if (!TryReadTagHeader(reader, out dataLength, out footerLength))
            {
                input.Position = originalPosition;
                return false;
            }

            long newPosition = originalPosition + 10 + dataLength + footerLength;
            if (newPosition > input.Length)
            {
                input.Position = originalPosition;
                return false;
            }

            input.Position = newPosition;
            return true;
        }

        private static int ReadSynchsafeInt32(byte[] bytes, int offset)
        {
            return bytes[offset] * (1 << 21)
                   + bytes[offset + 1] * (1 << 14)
                   + bytes[offset + 2] * (1 << 7)
                   + bytes[offset + 3];
        }

        private static bool TryReadTagHeader(BinaryReader reader, out int dataLength, out int footerLength)
        {
            dataLength = 0;
            footerLength = 0;

            byte[] headerBytes = reader.ReadBytes(10);
            if (headerBytes.Length < 10)
            {
                return false;
            }

            if (headerBytes[0] != (byte)'I' || headerBytes[1] != (byte)'D' || headerBytes[2] != (byte)'3')
            {
                return false;
            }

            if ((headerBytes[6] & 0x80) != 0 ||
                (headerBytes[7] & 0x80) != 0 ||
                (headerBytes[8] & 0x80) != 0 ||
                (headerBytes[9] & 0x80) != 0)
            {
                return false;
            }

            dataLength = ReadSynchsafeInt32(headerBytes, 6);
            footerLength = (headerBytes[5] & 0x10) == 0x10 ? 10 : 0;
            return true;
        }

        private static void SkipBytes(BinaryReader reader, int bytesToSkip)
        {
            if (bytesToSkip <= 0)
            {
                return;
            }

            byte[] buffer = new byte[Math.Min(4096, bytesToSkip)];
            while (bytesToSkip > 0)
            {
                int read = reader.Read(buffer, 0, Math.Min(buffer.Length, bytesToSkip));
                if (read <= 0)
                {
                    break;
                }

                bytesToSkip -= read;
            }
        }

        private Id3v2Tag(Stream input)
        {
            tagStartPosition = input.Position;
            var reader = new BinaryReader(input);
            int dataLength;
            int footerLength;
            if (TryReadTagHeader(reader, out dataLength, out footerLength))
            {
                SkipBytes(reader, dataLength);
                SkipBytes(reader, footerLength);
            }
            else
            {
                input.Position = tagStartPosition;
                throw new FormatException("Not an ID3v2 tag");
            }
            tagEndPosition = input.Position;
            input.Position = tagStartPosition;
            rawData = reader.ReadBytes((int)(tagEndPosition - tagStartPosition));

        }

        /// <summary>
        /// Raw data from this tag
        /// </summary>
        public byte[] RawData
        {
            get
            {
                return rawData;
            }
        }
    }
}
