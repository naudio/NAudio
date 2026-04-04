using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Helper class allowing us to modify the volume of a 16 bit stream without converting to IEEE float
    /// </summary>
    public class VolumeWaveProvider16 : IWaveProvider
    {
        private readonly IWaveProvider sourceProvider;
        private float volume;

        /// <summary>
        /// Constructs a new VolumeWaveProvider16
        /// </summary>
        /// <param name="sourceProvider">Source provider, must be 16 bit PCM</param>
        public VolumeWaveProvider16(IWaveProvider sourceProvider)
        {
            this.Volume = 1.0f;
            this.sourceProvider = sourceProvider;
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                throw new ArgumentException("Expecting PCM input");
            if (sourceProvider.WaveFormat.BitsPerSample != 16)
                throw new ArgumentException("Expecting 16 bit");
        }

        /// <summary>
        /// Gets or sets volume.
        /// 1.0 is full scale, 0.0 is silence, anything over 1.0 will amplify but potentially clip
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }

        /// <summary>
        /// WaveFormat of this WaveProvider
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return sourceProvider.WaveFormat; }
        }

        /// <summary>
        /// Read bytes from this WaveProvider
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <returns>Bytes read</returns>
        public int Read(Span<byte> buffer)
        {
            // always read from the source
            int bytesRead = sourceProvider.Read(buffer);
            if (this.volume == 0.0f)
            {
                buffer.Slice(0, bytesRead).Clear();
            }
            else if (this.volume != 1.0f)
            {
                for (int n = 0; n < bytesRead; n += 2)
                {
                    short sample = (short)((buffer[n + 1] << 8) | buffer[n]);
                    var newSample = sample * this.volume;
                    sample = (short)newSample;
                    // clip if necessary
                    if (this.Volume > 1.0f)
                    {
                        if (newSample > Int16.MaxValue) sample = Int16.MaxValue;
                        else if (newSample < Int16.MinValue) sample = Int16.MinValue;
                    }

                    buffer[n] = (byte)(sample & 0xFF);
                    buffer[n + 1] = (byte)(sample >> 8);
                }
            }
            return bytesRead;
        }
    }
}
