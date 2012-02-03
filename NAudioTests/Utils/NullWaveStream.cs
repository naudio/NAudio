using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;

namespace NAudioTests.Utils
{
    class NullWaveStream : WaveStream
    {
        WaveFormat format;
        long position = 0;
        long length;
        
        public NullWaveStream(WaveFormat format, long length)
        {
            this.format = format;
            this.length = length;
        }

        public override WaveFormat WaveFormat
        {
            get { return format; }
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
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position > length)
            {
                return 0;
            }
            count = (int)Math.Min(count, length - position);
            Array.Clear(buffer, offset, count);
            position += count;
            return count;
        }
    }
}
