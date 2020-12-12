using NAudio.Wave;
using System;

namespace NAudioWpfDemo.FireAndForgetPlayback
{
    class AutoDisposeFileReader : ISampleProvider
    {
        private readonly AudioFileReader reader;
        private bool isDisposed;
        public AutoDisposeFileReader(AudioFileReader reader)
        {
            this.reader = reader;
            WaveFormat = reader.WaveFormat;
        }

        public int Read(Span<float> buffer)
        {
            if (isDisposed)
                return 0;
            int read = reader.Read(buffer);
            if (read == 0)
            {
                reader.Dispose();
                isDisposed = true;
            }
            return read;
        }

        public WaveFormat WaveFormat { get; }
    }
}