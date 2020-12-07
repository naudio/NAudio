using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// IMA/DVI ADPCM Wave Format
    /// Work in progress
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class ImaAdpcmWaveFormat : WaveFormat
    {
        short samplesPerBlock;

        /// <summary>
        /// parameterless constructor for Marshalling
        /// </summary>
        ImaAdpcmWaveFormat()
        {
        }

        /// <summary>
        /// Creates a new IMA / DVI ADPCM Wave Format
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="channels">Number of channels</param>
        /// <param name="bitsPerSample">Bits Per Sample</param>
        public ImaAdpcmWaveFormat(int sampleRate, int channels, int bitsPerSample)
        {
            this.waveFormatTag = WaveFormatEncoding.DviAdpcm; // can also be ImaAdpcm - they are the same
            this.sampleRate = sampleRate;
            this.channels = (short)channels;
            this.bitsPerSample = (short)bitsPerSample; // TODO: can be 3 or 4
            this.extraSize = 2;            
            this.blockAlign = 0; //TODO
            this.averageBytesPerSecond = 0; //TODO
            this.samplesPerBlock = 0; // TODO
        }
    }
}
