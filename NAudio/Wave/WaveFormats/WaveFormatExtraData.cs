using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace NAudio.Wave
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    class WaveFormatExtraData : WaveFormat
    {
        // try with 100 bytes for now, increase if necessary
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        byte[] extraData = new byte[100];

        /// <summary>
        /// parameterless constructor for marshalling
        /// </summary>
        WaveFormatExtraData()
        {
        }

        public WaveFormatExtraData(BinaryReader reader)
            : base(reader)
        {
            if (this.extraSize > 0)
            {
                reader.Read(extraData,0, extraSize);
            }
        }

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
