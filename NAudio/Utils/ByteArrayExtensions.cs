using System;
using System.Collections.Generic;
using System.Text;



namespace NAudio.Utils
{    
    /// <summary>
    /// these will become extension methods once we move to .NET 3.5
    /// </summary>
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Checks if the buffer passed in is entirely full of nulls
        /// </summary>
        public static bool IsEntirelyNull(byte[] buffer)
        {
            foreach (byte b in buffer)
                if (b != 0)
                    return false;
            return true;
        }

        /// <summary>
        /// Converts to a string containing the buffer described in hex
        /// </summary>
        public static string DescribeAsHex(byte[] buffer, string separator, int bytesPerLine)
        {
            StringBuilder sb = new StringBuilder();
            int n = 0;
            foreach (byte b in buffer)
            {
                sb.AppendFormat("{0:X2}{1}", b, separator);
                if (++n % bytesPerLine == 0)
                    sb.Append("\r\n");
            }
            sb.Append("\r\n");
            return sb.ToString();
        }
        
        /// <summary>
        /// Decodes the buffer using the specified encoding, stopping at the first null
        /// </summary>
        public static string DecodeAsString(byte[] buffer, int offset, int length, Encoding encoding)
        {
            for (int n = 0; n < length; n++)
            {
                if (buffer[offset + n] == 0)
                    length = n;
            }
            return encoding.GetString(buffer, offset, length);
        }
    }
}
