using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;

namespace NAudioTests.Utils
{
    class BlockAlignedWaveStream : WaveStream
    {
        long position;
        long length;
        WaveFormat waveFormat;
        public BlockAlignedWaveStream(int blockAlignment, long length)
        {
            waveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, 8000, 1, 16000, blockAlignment, 16);
            this.length = length;  
        }

        public override int BlockAlign
        {
            get
            {
                return waveFormat.BlockAlign;
            }
        }

        public override WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        public override long Length
        {
            get { return length; }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                if (position % BlockAlign != 0)
                {
                    throw new ArgumentException("Must position block aligned");
                }
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count % BlockAlign != 0)
            {
                throw new ArgumentException("Must read block aligned");
            }
            if (count > length - position)
            {
                count = (int)(length - position);
            }
            for (int n = 0; n < count; n++)
            {
                buffer[n + offset] = (byte) ((position + n) % 256);
            }
            position += count;
            return count;
        }
    }
}
