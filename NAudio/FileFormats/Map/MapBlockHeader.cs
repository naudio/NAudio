using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NAudio.FileFormats.Map
{
    class MapBlockHeader
    {
        int length; // surely this is length
        int value2;
        short value3;
        short value4;

        public static MapBlockHeader Read(BinaryReader reader)
        {
            MapBlockHeader header = new MapBlockHeader();
            header.length = reader.ReadInt32(); // usually first 2 bytes have a value
            header.value2 = reader.ReadInt32(); // usually 0
            header.value3 = reader.ReadInt16(); // 0,1,2,3
            header.value4 = reader.ReadInt16(); // 0x1017 (sometimes 0x1018
            return header;
        }

        public override string ToString()
        {
            return String.Format("{0} {1:X8} {2:X4} {3:X4}",
                length, value2, value3, value4);
        }

        public int Length
        {
            get { return length; }
        }
    }
}
