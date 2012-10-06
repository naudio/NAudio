using System;
using System.Text;

namespace NAudio.Utils
{
    /// <summary>
    /// An encoding that can read files with extended ASCII characters
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
            return count;
        }

        /// <summary>
        /// <see cref="Encoding.GetChars(byte[],int,int,char[],int)"/>
        /// </summary>
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (int n = 0; n < byteCount; n++)
            {
                chars[charIndex + n] = (char)bytes[byteIndex + n];
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
