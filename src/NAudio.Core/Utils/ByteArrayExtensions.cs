using System;
using System.Text;

namespace NAudio.Utils;

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
        return buffer.AsSpan().IndexOfAnyExcept((byte)0) == -1;
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
        ReadOnlySpan<byte> bytes = buffer.AsSpan(offset, length);

        int nullIndex = bytes.IndexOf((byte)0);
        if (nullIndex != -1)
        {
            bytes = bytes.Slice(0, nullIndex);
        }

        return encoding.GetString(bytes);
    }

    /// <summary>
    /// Concatenates the given arrays into a single array.
    /// </summary>
    /// <param name="byteArrays">The arrays to concatenate</param>
    /// <returns>The concatenated resulting array.</returns>
    public static byte[] Concat(params byte[][] byteArrays)
    {
        int size = 0;
        foreach (byte[] btArray in byteArrays)
        {
            size += btArray.Length;
        }

        if (size <= 0)
        {
            return new byte[0];
        }

        byte[] result = new byte[size];
        int idx = 0;
        foreach (byte[] btArray in byteArrays)
        {
            Array.Copy(btArray, 0, result, idx, btArray.Length);
            idx += btArray.Length;
        }

        return result;
    }
}
