using System;
using NAudio.Wave;

namespace NAudio.Extras
{
    public class LoopStream : WaveStream
    {
        readonly WaveStream sourceStream;

        public LoopStream(WaveStream source)
        {
            sourceStream = source;
        }

        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        public override long Length
        {
            get { return long.MaxValue / 32; }
        }

        public override long Position
        {
            get
            {
                return sourceStream.Position;
            }
            set
            {
                sourceStream.Position = value;
            }
        }

        public override bool HasData(int count)
        {
            // infinite loop
            return true;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int required = count - read;
                int readThisTime = sourceStream.Read(buffer, offset + read, required);
                if (readThisTime < required)
                {
                    sourceStream.Position = 0;
                }

                if (sourceStream.Position >= sourceStream.Length)
                {
                    sourceStream.Position = 0;
                }
                read += readThisTime;
            }
            return read;
        }

        protected override void Dispose(bool disposing)
        {
            sourceStream.Dispose();
            base.Dispose(disposing);
        }
    }
}
