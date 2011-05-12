using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace NAudio.Wave
{
      /*WaveFormat: DspGroupTruespeech 8000Hz Channels: 1 Bits: 1 Block Align: 32, AverageBytesPerSecond: 1067 (8.5 kbps), Extra Size: 32
      Extra Bytes:
      01 00 F0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 */    

    /// <summary>
    /// GSM 610
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class TrueSpeechWaveFormat : WaveFormat
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        short[] unknown;

        /// <summary>
        /// DSP Group TrueSpeech WaveFormat
        /// </summary>
        public TrueSpeechWaveFormat()
        {
            this.waveFormatTag = WaveFormatEncoding.DspGroupTrueSpeech;
            this.channels = 1;
            this.averageBytesPerSecond = 1067;
            this.bitsPerSample = 1;
            this.blockAlign = 32;
            this.sampleRate = 8000;

            this.extraSize = 32;
            this.unknown = new short[16];
            this.unknown[0] = 1;
            this.unknown[1] = 0xF0;
        }

        /// <summary>
        /// Writes this structure to a BinaryWriter
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            foreach (short val in unknown)
            {
                writer.Write(val);
            }
        }
    }
}
