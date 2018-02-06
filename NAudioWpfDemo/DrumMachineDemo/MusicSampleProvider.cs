using System;
using System.Linq;
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

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesWritten = 0;
            if (position < delayBy)
            {
                int zeroFill = Math.Min(delayBy - position, count);
                Array.Clear(buffer, offset, zeroFill);
                position += zeroFill;
                samplesWritten += zeroFill;
            }
            if (samplesWritten < count)
            {
                int samplesNeeded = count - samplesWritten;
                int samplesAvailable = sampleSource.Length - (position - delayBy);
                int samplesToCopy = Math.Min(samplesNeeded, samplesAvailable);
                Array.Copy(sampleSource.SampleData, PositionInSampleSource, buffer, samplesWritten, samplesToCopy);
                position += samplesToCopy;
                samplesWritten += samplesToCopy;
            }
            return samplesWritten;
        }

        private int PositionInSampleSource => (position - delayBy) + sampleSource.StartIndex;
    }
}
