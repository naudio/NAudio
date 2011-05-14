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
    public class MeteringStream : WaveStream
    {
        public WaveStream SourceStream { get; private set; }
        public int SamplesPerNotification { get; set; }

        float[] maxSamples;
        int sampleCount;

        public event EventHandler<StreamVolumeEventArgs> StreamVolume;

        public MeteringStream(WaveStream sourceStream) :
            this(sourceStream, sourceStream.WaveFormat.SampleRate / 10)
        {
        }

        public MeteringStream(WaveStream sourceStream, int samplesPerNotification)
        {
            SourceStream = sourceStream;
            if (sourceStream.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Metering Stream expects 32 bit floating point audio", "sourceStream");
            maxSamples = new float[sourceStream.WaveFormat.Channels];
            this.SamplesPerNotification = samplesPerNotification;
        }

        public override WaveFormat WaveFormat
        {
            get { return SourceStream.WaveFormat; }
        }

        public override long Length
        {
            get { return SourceStream.Length; }
        }

        public override long Position
        {
            get { return SourceStream.Position; }
            set { SourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {           
            int bytesRead = SourceStream.Read(buffer, offset, count);
            ProcessData(buffer, offset, bytesRead);
            return bytesRead;
        }

        private void ProcessData(byte[] buffer, int offset, int count)
        {
            int index = 0;
            while ( index < count)
            {
                for (int channel = 0; channel < maxSamples.Length; channel++)
                {
                    float sampleValue = Math.Abs(BitConverter.ToSingle(buffer, offset + index));
                    maxSamples[channel] = Math.Max(maxSamples[channel],sampleValue);
                    index += 4;
                }
                sampleCount++;
                if(sampleCount >= SamplesPerNotification)
                {
                    RaiseStreamVolumeNotification();
                    sampleCount = 0;
                    Array.Clear(maxSamples, 0, maxSamples.Length);
                    
                }
                
            }
        }

        private void RaiseStreamVolumeNotification()
        {
            if (StreamVolume != null)
            {
                StreamVolume(this, new StreamVolumeEventArgs() { MaxSampleValues = (float[])maxSamples.Clone() });
            }
        }
    }

    public class StreamVolumeEventArgs : EventArgs
    {
        public float[] MaxSampleValues { get; set; }
    }
}
