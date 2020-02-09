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
        /// <param name="frames">The Id3v2 frames that will be included in the file. This is used to calculate the ID3v2 tag size.</param>
        /// <returns></returns>
        static byte[] CreateId3v2TagHeader(IEnumerable<byte[]> frames)
        {
            int size = 0;
            foreach (byte[] frame in frames)
            {
                size += frame.Length;
            }

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
            foreach (KeyValuePair<string, string> tag in tags)
            {
                frames.Add(CreateId3v2Frame(tag.Key, tag.Value));
            }

            byte[] header = CreateId3v2TagHeader(frames);

            MemoryStream ms = new MemoryStream();
            ms.Write(header, 0, header.Length);
            foreach (byte[] frame in frames)
            {
                ms.Write(frame, 0, frame.Length);
            }

            ms.Position = 0;
            return ms;
        }

        #endregion

        private Id3v2Tag(Stream input)
        {
            tagStartPosition = input.Position;
            var reader = new BinaryReader(input);
            byte[] headerBytes = reader.ReadBytes(10);
            if ((headerBytes.Length >= 3) &&
                (headerBytes[0] == (byte)'I') &&
                (headerBytes[1] == (byte)'D') &&
                (headerBytes[2] == '3'))
            {

                // http://www.id3.org/develop.html
                // OK found an ID3 tag
                // bytes 3 & 4 are ID3v2 version

                if ((headerBytes[5] & 0x40) == 0x40)
                {
                    // extended header present
                    byte[] extendedHeader = reader.ReadBytes(4);
                    int extendedHeaderLength = extendedHeader[0] * (1 << 21);
                    extendedHeaderLength += extendedHeader[1] * (1 << 14);
                    extendedHeaderLength += extendedHeader[2] * (1 << 7);
                    extendedHeaderLength += extendedHeader[3];
                }

                // synchsafe
                int dataLength = headerBytes[6] * (1 << 21);
                dataLength += headerBytes[7] * (1 << 14);
                dataLength += headerBytes[8] * (1 << 7);
                dataLength += headerBytes[9];
                byte[] tagData = reader.ReadBytes(dataLength);

                if ((headerBytes[5] & 0x10) == 0x10)
                {
                    // footer present
                    byte[] footer = reader.ReadBytes(10);
                }
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
