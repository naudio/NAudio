using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// ASIO Sample Type
    /// </summary>
    public enum AsioSampleType
    {
        /// <summary>
        /// Int 16 MSB
        /// </summary>
        Int16MSB = 0,
        /// <summary>
        /// Int 24 MSB (used for 20 bits as well)
        /// </summary>
        Int24MSB = 1,
        /// <summary>
        /// Int 32 MSB
        /// </summary>
        Int32MSB = 2,
        /// <summary>
        /// IEEE 754 32 bit float
        /// </summary>
        Float32MSB = 3,
        /// <summary>
        /// IEEE 754 64 bit double float
        /// </summary>
        Float64MSB = 4,
        /// <summary>
        /// 32 bit data with 16 bit alignment
        /// </summary>
        Int32MSB16 = 8,
        /// <summary>
        /// 32 bit data with 18 bit alignment
        /// </summary>
        Int32MSB18 = 9, // 
        /// <summary>
        /// 32 bit data with 20 bit alignment
        /// </summary>
        Int32MSB20 = 10,
        /// <summary>
        /// 32 bit data with 24 bit alignment
        /// </summary>
        Int32MSB24 = 11,
        /// <summary>
        /// Int 16 LSB
        /// </summary>
        Int16LSB = 16,
        /// <summary>
        /// Int 24 LSB
        /// used for 20 bits as well
        /// </summary>
        Int24LSB = 17,
        /// <summary>
        /// Int 32 LSB
        /// </summary>
        Int32LSB = 18,
        /// <summary>
        /// IEEE 754 32 bit float, as found on Intel x86 architecture
        /// </summary>
        Float32LSB = 19,
        /// <summary>
        /// IEEE 754 64 bit double float, as found on Intel x86 architecture
        /// </summary>
        Float64LSB = 20,
        /// <summary>
        /// 32 bit data with 16 bit alignment
        /// </summary>
        Int32LSB16 = 24,
        /// <summary>
        /// 32 bit data with 18 bit alignment
        /// </summary>
        Int32LSB18 = 25,
        /// <summary>
        /// 32 bit data with 20 bit alignment
        /// </summary>
        Int32LSB20 = 26,
        /// <summary>
        /// 32 bit data with 24 bit alignment
        /// </summary>
        Int32LSB24 = 27,
        /// <summary>
        /// DSD 1 bit data, 8 samples per byte. First sample in Least significant bit.
        /// </summary>
        DSDInt8LSB1 = 32,
        /// <summary>
        /// DSD 1 bit data, 8 samples per byte. First sample in Most significant bit.
        /// </summary>
        DSDInt8MSB1 = 33,
        /// <summary>
        /// DSD 8 bit data, 1 sample per byte. No Endianness required.
        /// </summary>
        DSDInt8NER8 = 40,
    }
}
