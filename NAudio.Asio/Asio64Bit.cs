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
        // TODO: IMPLEMENT AN EASY WAY TO CONVERT THIS TO double  AND long
    };
}