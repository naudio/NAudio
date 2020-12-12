using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    class TestWaveProvider : IWaveProvider
    {
        private int length;
        public int ConstValue { get; set; }

        public TestWaveProvider(WaveFormat format, int lengthInBytes = Int32.MaxValue)
        {
            this.WaveFormat = format;
            this.length = lengthInBytes;
            this.ConstValue = -1;
        }

        public int Read(Span<byte> buffer)
        {
            int n = 0;
            while (n < buffer.Length && Position < length)
            {
                buffer[n++] = (ConstValue == -1) ? (byte)Position : (byte)ConstValue;
                Position++;
            }
            return n;
        }

        public WaveFormat WaveFormat { get; set; }

        public int Position { get; set; }
    }
}
