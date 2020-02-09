using System;
using System.Runtime.InteropServices;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// This class used for marshalling from unmanaged code
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    public class WaveFormatExtraData : WaveFormat
    {
        // try with 100 bytes for now, increase if necessary
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        private byte[] extraData = new byte[100];

        /// <summary>
        /// Allows the extra data to be read
        /// </summary>
        public byte[] ExtraData => extraData;

        /// <summary>
        /// parameterless constructor for marshalling
        /// </summary>
        internal WaveFormatExtraData()
        {
        }

        /// <summary>
        /// Reads this structure from a BinaryReader
        /// </summary>
        public WaveFormatExtraData(BinaryReader reader)
            : base(reader)
        {
            ReadExtraData(reader);
        }

        internal void ReadExtraData(BinaryReader reader)
        {
            if (this.extraSize > 0)
            {
                reader.Read(extraData, 0, extraSize);
            }
        }

        /// <summary>
        /// Writes this structure to a BinaryWriter
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            if (extraSize > 0)
            {
                writer.Write(extraData, 0, extraSize);
            }
        }
    }
}
