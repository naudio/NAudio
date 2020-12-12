using System;
using System.Linq;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class MusicSampleProvider : ISampleProvider
    {
        private int delayBy;
        private int position;
        private readonly SampleSource sampleSource;

        public MusicSampleProvider(SampleSource sampleSource)
        {
            this.sampleSource = sampleSource;
        }

        /// <summary>
        /// Samples to delay before returning anything
        /// </summary>
        public int DelayBy
        {
            get => delayBy;
            set 
            { 
                if (value < 0)
                {
                    throw new ArgumentException("Cannot delay by negative number of samples");
                }
                delayBy = value; 
            }
        }

        public WaveFormat WaveFormat => sampleSource.SampleWaveFormat;

        public int Read(Span<float> buffer)
        {
            int samplesWritten = 0;
            var count = buffer.Length;
            if (position < delayBy)
            {
                int zeroFill = Math.Min(delayBy - position, count);
                buffer.Slice(0, zeroFill).Clear();
                position += zeroFill;
                samplesWritten += zeroFill;
            }
            if (samplesWritten < count)
            {
                int samplesNeeded = count - samplesWritten;
                int samplesAvailable = sampleSource.Length - (position - delayBy);
                int samplesToCopy = Math.Min(samplesNeeded, samplesAvailable);
                SpanExtensions.ArrayCopy(sampleSource.SampleData, PositionInSampleSource, buffer.Slice(samplesWritten), samplesToCopy);
                //Array.Copy(sampleSource.SampleData, PositionInSampleSource, buffer, samplesWritten, samplesToCopy);
                position += samplesToCopy;
                samplesWritten += samplesToCopy;
            }
            return samplesWritten;
        }

        private int PositionInSampleSource => (position - delayBy) + sampleSource.StartIndex;
    }
}
