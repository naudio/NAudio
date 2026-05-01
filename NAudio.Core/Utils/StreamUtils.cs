
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NAudio.Utils
{
    /// <summary>
    /// Provides utilities for manipulating <see cref="Stream"/> instances.
    /// </summary>
    /// <remarks>
    /// Most of the methods do not validate for the nullness of <see cref="Stream"/>. 
    /// It is assumed that the class that uses these has already done a validation against <see langword="null"/>.
    /// </remarks>
    public static class StreamUtils
    {
        /// <summary>
        /// Reads a byte from the stream. <br />
        /// This method enforces that the stream must have at least one byte left, or an <see cref="EndOfStreamException"/> is thrown.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read byte.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static byte ReadByte(Stream stream)
        {
            int rb = stream.ReadByte();
            if (rb == -1)
            {
                throw new EndOfStreamException();
            }
            else
            {
                return unchecked((byte)rb); // Just do the conversion directly.
            }
        }

        /// <summary>
        /// Computes the buffer sizes when dispatching requests regarding methods related to data 
        /// streams that are instructed to read a fixed number of bytes, and are using temporary buffers. <br />
        /// You pass in the actual number of bytes read or to write, the total number of bytes to read or write, and the actual size of the buffer. <br />
        /// Example: <br /> <br />
        /// <code>
        /// // An example of how to use it
        /// System.IO.Stream stream; // A data stream to read from. Assumed that it is properly initialized before.
        /// byte[] temp = new byte[2048];
        /// 
        /// long total_bytes = 300000;
        /// 
        /// int bytes_read;
        /// 
        /// for (long c = 0; c &lt; total_bytes; c += bytes_read)
        /// {
        ///     bytes_read = stream.Read(temp , 0 , ComputeStreamBufferSize(c , total_bytes, 2048));
        ///     
        ///     // Do something with the data now...
        /// }
        /// </code>
        /// </summary>
        /// <param name="consumed">The number of bytes already processed.</param>
        /// <param name="total">The total number of bytes that are to be read/written.</param>
        /// <param name="buffer_size">The temporary buffer size.</param>
        /// <returns>A computed value, so that the value is in range [0..<paramref name="buffer_size"/>].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeStreamBufferSize(long consumed, long total, int buffer_size) => ((consumed + buffer_size) < total) ? buffer_size : (int)(total - consumed);

        #region Little Endian

        /// <summary>Reads a <see cref="short"/> from a <see cref="Stream"/>, that has been encoded using the little-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="short"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static short ReadShortLittleEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            stream.ReadExactly(buffer);
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<short>(ref buffer.GetPinnableReference()); // GetPinnableReference executes faster than ref buffer[0]
        }

        /// <summary>
        /// Writes a <see cref="short"/> to a <see cref="Stream"/>, and will be encoded using the little-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="short"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteShortLittleEndian(Stream stream, short value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<short, byte>(ref value), sizeof(short));
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="ushort"/> from a <see cref="Stream"/>, that has been encoded using the little-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="ushort"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static ushort ReadUShortLittleEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            stream.ReadExactly(buffer);
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<ushort>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="ushort"/> to a <see cref="Stream"/>, and will be encoded using the little-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="ushort"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteUShortLittleEndian(Stream stream, ushort value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<ushort, byte>(ref value), sizeof(ushort));
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="int"/> from a <see cref="Stream"/>, that has been encoded using the little-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="int"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static int ReadIntLittleEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            stream.ReadExactly(buffer);
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<int>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="int"/> to a <see cref="Stream"/>, and will be encoded using the little-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="int"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteIntLittleEndian(Stream stream, int value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<int, byte>(ref value), sizeof(int));
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="uint"/> from a <see cref="Stream"/>, that has been encoded using the little-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="uint"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static uint ReadUIntLittleEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            stream.ReadExactly(buffer);
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<uint>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="uint"/> to a <see cref="Stream"/>, and will be encoded using the little-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="uint"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteUIntLittleEndian(Stream stream, uint value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref value), sizeof(uint));
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="long"/> from a <see cref="Stream"/>, that has been encoded using the little-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="long"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static long ReadLongLittleEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            stream.ReadExactly(buffer);
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<long>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="long"/> to a <see cref="Stream"/>, and will be encoded using the little-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="long"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteLongLittleEndian(Stream stream, long value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<long, byte>(ref value), sizeof(long));
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="ulong"/> from a <see cref="Stream"/>, that has been encoded using the little-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="ulong"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static ulong ReadULongLittleEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            stream.ReadExactly(buffer);
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<ulong>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="ulong"/> to a <see cref="Stream"/>, and will be encoded using the little-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="ulong"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteULongLittleEndian(Stream stream, ulong value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref value), sizeof(ulong));
            if (!BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="float"/> from a <see cref="Stream"/>, that has been encoded using the little-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="float"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static float ReadFloatLittleEndian(Stream stream) => Unsafe.BitCast<int, float>(ReadIntLittleEndian(stream));

        /// <summary>
        /// Writes a <see cref="float"/> to a <see cref="Stream"/>, and will be encoded using the little-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="float"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteFloatLittleEndian(Stream stream, float value) => WriteIntLittleEndian(stream, Unsafe.BitCast<float, int>(value));

        /// <summary>Reads a <see cref="double"/> from a <see cref="Stream"/>, that has been encoded using the little-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="double"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static double ReadDoubleLittleEndian(Stream stream) => Unsafe.BitCast<long, double>(ReadLongLittleEndian(stream));

        /// <summary>
        /// Writes a <see cref="double"/> to a <see cref="Stream"/>, and will be encoded using the little-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="double"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteDoubleLittleEndian(Stream stream, double value) => WriteLongLittleEndian(stream, Unsafe.BitCast<double, long>(value));

        #endregion

        #region Big Endian

        /// <summary>Reads a <see cref="short"/> from a <see cref="Stream"/>, that has been encoded using the big-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="short"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static short ReadShortBigEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            stream.ReadExactly(buffer);
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<short>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="short"/> to a <see cref="Stream"/>, and will be encoded using the big-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="short"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteShortBigEndian(Stream stream, short value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<short, byte>(ref value), sizeof(short));
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="ushort"/> from a <see cref="Stream"/>, that has been encoded using the big-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="ushort"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static ushort ReadUShortBigEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            stream.ReadExactly(buffer);
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<ushort>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="ushort"/> to a <see cref="Stream"/>, and will be encoded using the big-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="ushort"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteUShortBigEndian(Stream stream, ushort value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<ushort, byte>(ref value), sizeof(ushort));
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="int"/> from a <see cref="Stream"/>, that has been encoded using the big-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="int"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static int ReadIntBigEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            stream.ReadExactly(buffer);
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<int>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="int"/> to a <see cref="Stream"/>, and will be encoded using the big-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="int"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteIntBigEndian(Stream stream, int value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<int, byte>(ref value), sizeof(int));
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="uint"/> from a <see cref="Stream"/>, that has been encoded using the big-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="uint"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static uint ReadUIntBigEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            stream.ReadExactly(buffer);
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<uint>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="uint"/> to a <see cref="Stream"/>, and will be encoded using the big-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="uint"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteUIntBigEndian(Stream stream, uint value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref value), sizeof(uint));
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="long"/> from a <see cref="Stream"/>, that has been encoded using the big-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="long"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static long ReadLongBigEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            stream.ReadExactly(buffer);
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<long>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="long"/> to a <see cref="Stream"/>, and will be encoded using the big-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="long"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteLongBigEndian(Stream stream, long value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<long, byte>(ref value), sizeof(long));
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="ulong"/> from a <see cref="Stream"/>, that has been encoded using the big-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="ulong"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static ulong ReadULongBigEndian(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            stream.ReadExactly(buffer);
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            return Unsafe.ReadUnaligned<ulong>(ref buffer.GetPinnableReference());
        }

        /// <summary>
        /// Writes a <see cref="ulong"/> to a <see cref="Stream"/>, and will be encoded using the big-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="ulong"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteULongBigEndian(Stream stream, ulong value)
        {
            Span<byte> buffer = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref value), sizeof(ulong));
            if (BitConverter.IsLittleEndian) { buffer.Reverse(); }
            stream.Write(buffer);
        }

        /// <summary>Reads a <see cref="float"/> from a <see cref="Stream"/>, that has been encoded using the little-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="float"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static float ReadFloatBigEndian(Stream stream) => Unsafe.BitCast<int, float>(ReadIntBigEndian(stream));

        /// <summary>
        /// Writes a <see cref="float"/> to a <see cref="Stream"/>, and will be encoded using the little-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="float"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteFloatBigEndian(Stream stream, float value) => WriteIntBigEndian(stream, Unsafe.BitCast<float, int>(value));

        /// <summary>Reads a <see cref="double"/> from a <see cref="Stream"/>, that has been encoded using the little-endian endianness.</summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>The read <see cref="double"/>.</returns>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="EndOfStreamException">The stream was ended before this method was called.</exception>
        public static double ReadDoubleBigEndian(Stream stream) => Unsafe.BitCast<long, double>(ReadLongBigEndian(stream));

        /// <summary>
        /// Writes a <see cref="double"/> to a <see cref="Stream"/>, and will be encoded using the little-endian endianness. 
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="value">The <see cref="double"/> to write to <see cref="Stream"/>.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        public static void WriteDoubleBigEndian(Stream stream, double value) => WriteLongBigEndian(stream, Unsafe.BitCast<double, long>(value));

        #endregion

        #region Strings

        /// <summary>
        /// Writes a string to the specified stream , under the specified encoding.
        /// </summary>
        /// <param name="stream">The stream to write the specified string.</param>
        /// <param name="str">The string to write.</param>
        /// <param name="enc">The character encoding under which <paramref name="str"/> will be saved.</param>
        /// <exception cref="IOException">An I/O exception was occurred.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="enc"/> was null.</exception>
        /// <returns>The number of bytes written for saving the string into the data stream.</returns>
        public static unsafe long WriteString(Stream stream, string str, System.Text.Encoding enc)
        {
            ArgumentNullException.ThrowIfNull(enc);
            if (str == string.Empty) { return 0; }

            var temp_1 = System.Buffers.ArrayPool<byte>.Shared.RentByContext(2048);

            try
            {
                System.Text.Encoder encoder = enc.GetEncoder();

                int len = str.Length, chars_consumed = 0, bytes_written;
                long total_bytes = 0;

                bool completed;

                fixed (char* pstring = str)
                {
                    char* mutable = pstring;

                    do
                    {
                        // Process the string buffer, getting it by 2048 byte chunks and repeating if the string is too large to directly fit in 2048 characters.
                        fixed (byte* pdest = temp_1)
                            encoder.Convert(mutable, len, pdest, 2048, len == 0, out chars_consumed, out bytes_written, out completed);

                        mutable += chars_consumed;
                        len -= chars_consumed;

                        if (bytes_written > 0)
                        {
                            stream.Write(temp_1, 0, bytes_written);
                            total_bytes += bytes_written;
                        }
                    } while (!completed);
                }

                return total_bytes;
            }
            finally
            {
                temp_1.Dispose();
            }
        }

        /// <summary>
        /// Reads a string value previously written with the <see cref="WriteString(Stream, string, System.Text.Encoding)"/> method. <br />
        /// The string value is read as a string builder.
        /// </summary>
        /// <param name="stream">The data stream to read the specified string from.</param>
        /// <param name="enc">The character encoding under which the string will be read back.</param>
        /// <param name="nbytes">The number of bytes comprising the string data</param>
        /// <returns>The read string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enc"/> was <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="nbytes"/> was negative.</exception>
        public static System.Text.StringBuilder ReadStringAsBuilder(Stream stream, System.Text.Encoding enc, long nbytes)
        {
            ArgumentNullException.ThrowIfNull(enc);
            if (nbytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nbytes), "Number of bytes cannot be negative!!");
            }
            else if (nbytes == 0)
            {
                return new();
            }
            else
            {
                ArrayPoolExtensions.RentContext<byte> temp_1 = default;
                ArrayPoolExtensions.RentContext<char> temp_2 = default;

                try
                {
                    // Get the decoder to use
                    System.Text.Decoder dec = enc.GetDecoder();

                    // Allocate temporary processing buffers
                    temp_1 = System.Buffers.ArrayPool<byte>.Shared.RentByContext(2048);
                    temp_2 = System.Buffers.ArrayPool<char>.Shared.RentByContext(2048);

                    // OK. Now allocate our string builder.
                    // Compute the possible capacity that we need to allocate.
                    // As such, this would avoid further resizes later when we process the stream and adding buffers.
                    System.Text.StringBuilder sb = new(
                        (int)((nbytes / 2048) * enc.GetMaxCharCount(2048)) +
                        enc.GetMaxCharCount(unchecked((int)(nbytes % 2048)))
                    );

                    int temp_bytes_consumed = 0, temp_chars_used, proc_bytes_used, proc_byte_index;

                    // Assume that the end of stream is not reached yet.
                    // This is done so that the loop can enter the first time.
                    bool completed, end_of_stream = false;

                    // Read bytes to a temporary buffer, process the buffer through the decoder, and append the decoded data to the string builder.
                    // Continue doing that until: 
                    // -> End of stream is not reached yet
                    // -> The number of consumed bytes is less than the expected length in bytes of the string.
                    for (long consumed = 0; !end_of_stream && consumed < nbytes; consumed += temp_bytes_consumed)
                    {
                        end_of_stream = (temp_bytes_consumed = stream.Read(temp_1, 0, ComputeStreamBufferSize(consumed, nbytes, 2048))) < 1;

                        proc_byte_index = 0;

                        // Process string data
                        // If we reached end of stream, process decoder leftovers
                        do
                        {
                            dec.Convert(temp_1, proc_byte_index, temp_bytes_consumed - proc_byte_index
                                , temp_2, 0, 2048,
                                end_of_stream,
                                out proc_bytes_used,
                                out temp_chars_used,
                                out completed);

                            sb.Append(temp_2, 0, temp_chars_used);

                            proc_byte_index += proc_bytes_used;
                        } while (!completed);
                    }

                    // Return our string builder.
                    return sb;
                }
                finally
                {
                    temp_1.Dispose();
                    temp_2.Dispose();
                }
            }
        }

        /// <summary>
        /// Reads a string value previously written with the <see cref="WriteString(Stream, string, System.Text.Encoding)"/> method.
        /// </summary>
        /// <param name="stream">The data stream to read the specified string from.</param>
        /// <param name="enc">The character encoding under which the string will be read back.</param>
        /// <param name="nbytes">The number of bytes comprising the string data</param>
        /// <returns>The read string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enc"/> was <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="nbytes"/> was negative.</exception>
        public static System.String ReadString(Stream stream, System.Text.Encoding enc, long nbytes) => ReadStringAsBuilder(stream, enc, nbytes).ToString();

        #endregion
    }
}
