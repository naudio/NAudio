using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;

namespace NAudioDemo.AudioPlaybackDemo
{
    /// <summary>
    /// basic metering stream
    /// n.b. does not close its source stream
    /// </summary>
    public class MeteringStream : WaveProvider32
    {
        public IWaveProviderFloat SourceProvider { get; private set; }
        public int SamplesPerNotification { get; set; }

        float[] maxSamples;
        int sampleCount;
        private int channels;

        public event EventHandler<StreamVolumeEventArgs> StreamVolume;

        public MeteringStream(IWaveProviderFloat sourceProvider) :
            this(sourceProvider, sourceProvider.WaveFormat.SampleRate / 10)
        {
        }

        public MeteringStream(IWaveProviderFloat sourceProvider, int samplesPerNotification)
        {
            this.SourceProvider = sourceProvider;
            this.SetWaveFormat(this.SourceProvider.WaveFormat.SampleRate, this.SourceProvider.WaveFormat.Channels);
            this.channels = SourceProvider.WaveFormat.Channels;
            maxSamples = new float[channels];
            this.SamplesPerNotification = samplesPerNotification;
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = SourceProvider.Read(buffer, offset, count);
            ProcessData(buffer, offset, samplesRead);
            return samplesRead;
        }

        private void ProcessData(float[] buffer, int offset, int count)
        {
            for (int index = 0; index < count; index += channels)
            {
                for (int channel = 0; channel < channels; channel++)
                {
                    float sampleValue = Math.Abs(buffer[offset + index + channel]);
                    maxSamples[channel] = Math.Max(maxSamples[channel], sampleValue);
                }
                sampleCount++;
                if (sampleCount >= SamplesPerNotification)
                {
                    RaiseStreamVolumeNotification();
                    sampleCount = 0;
                    maxSamples = new float[channels];
                }
            }
        }

        private void RaiseStreamVolumeNotification()
        {
            if (StreamVolume != null)
            {
                StreamVolume(this, new StreamVolumeEventArgs() { MaxSampleValues = maxSamples });
            }
        }
    }

    public class StreamVolumeEventArgs : EventArgs
    {
        public float[] MaxSampleValues { get; set; }
    }
}
