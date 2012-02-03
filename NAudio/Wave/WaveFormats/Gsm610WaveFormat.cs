using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace NAudio.Wave
{
    /// <summary>
    /// GSM 610
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class Gsm610WaveFormat : WaveFormat
    {
        private short samplesPerBlock;

        /// <summary>
        /// Creates a GSM 610 WaveFormat
        /// For now hardcoded to 13kbps
        /// </summary>
        public Gsm610WaveFormat()
        {
            this.waveFormatTag = WaveFormatEncoding.Gsm610;
            this.channels = 1;
            this.averageBytesPerSecond = 1625;
            this.bitsPerSample = 0; // must be zero
            this.blockAlign = 65;
            this.sampleRate = 8000;

            this.extraSize = 2;
            this.samplesPerBlock = 320;
        }

        /// <summary>
        /// Samples per block
        /// </summary>
        public short SamplesPerBlock { get { return this.samplesPerBlock; } }

        /// <summary>
        /// Writes this structure to a BinaryWriter
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(samplesPerBlock);
        }
    }
}
