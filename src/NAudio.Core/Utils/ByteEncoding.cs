using System;
using System.Text;

namespace NAudio.Utils
{
    /// <summary>
    /// An encoding for use with file types that have one byte per character
    /// </summary>
    public class ByteEncoding : Encoding
    {
        private ByteEncoding() 
        { 
        }

        /// <summary>
        /// The one and only instance of this class
        /// </summary>
        public static readonly ByteEncoding Instance = new ByteEncoding();

        /// <summary>
        /// <see cref="Encoding.GetByteCount(char[],int,int)"/>
        /// </summary>
        public override int GetByteCount(char[] chars, int index, int count)
        {
            return count;
        }

        /// <summary>
        /// <see cref="Encoding.GetBytes(char[],int,int,byte[],int)"/>
        /// </summary>
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            for (int n = 0; n < charCount; n++)
            {
                bytes[byteIndex + n] = (byte)chars[charIndex + n];
            }
            return charCount;
        }

        /// <summary>
        /// <see cref="Encoding.GetCharCount(byte[],int,int)"/>
        /// </summary>
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            for (int n = 0; n < count; n++)
            {
                if (bytes[index + n] == 0)
                    return n;
            }
            return count;
        }

        /// <summary>
        /// <see cref="Encoding.GetChars(byte[],int,int,char[],int)"/>
        /// </summary>
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (int n = 0; n < byteCount; n++)
            {
                var b = bytes[byteIndex + n];
                if (b == 0)
                {
                    return n;
                }
                chars[charIndex + n] = (char)b;
            }
            return byteCount;
        }

        /// <summary>
        /// <see cref="Encoding.GetMaxCharCount"/>
        /// </summary>
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }

        /// <summary>
        /// <see cref="Encoding.GetMaxByteCount"/>
        /// </summary>
        public override int GetMaxByteCount(int charCount)
        {
            return charCount;
        }
    }
}
