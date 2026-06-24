using System.Runtime.InteropServices;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// ASIO 64 bit value
    /// Unfortunately the ASIO API was implemented it before compiler supported consistently 64 bit 
    /// integer types. By using the structure the data layout on a little-endian system like the 
    /// Intel x86 architecture will result in a "non native" storage of the 64 bit data. The most 
    /// significant 32 bit are stored first in memory, the least significant bits are stored in the 
    /// higher memory space. However each 32 bit is stored in the native little-endian fashion
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Asio64Bit
    {
        /// <summary>
        /// most significant bits (Bits 32..63)
        /// </summary>
        public uint hi;
        /// <summary>
        /// least significant bits (Bits 0..31)
        /// </summary>
        public uint lo;

        /// <summary>
        /// Converts to a 64-bit signed integer
        /// </summary>
        public long ToInt64() => ((long)hi << 32) | lo;

        /// <summary>
        /// Converts to a 64-bit unsigned integer
        /// </summary>
        public ulong ToUInt64() => ((ulong)hi << 32) | lo;

        /// <summary>
        /// Converts to a double by reinterpreting the 64-bit integer bits
        /// </summary>
        public double ToDouble() => System.BitConverter.Int64BitsToDouble(ToInt64());
    };
}