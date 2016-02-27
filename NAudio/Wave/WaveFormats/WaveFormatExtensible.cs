using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using NAudio.Dmo;

namespace NAudio.Wave
{
    /// <summary>
    /// WaveFormatExtensible
    /// http://www.microsoft.com/whdc/device/audio/multichaud.mspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]	
    public class WaveFormatExtensible : WaveFormat
    {        
        short wValidBitsPerSample; // bits of precision, or is wSamplesPerBlock if wBitsPerSample==0
        int dwChannelMask; // which channels are present in stream
        Guid subFormat;

        /// <summary>
        /// Parameterless constructor for marshalling
        /// </summary>
        WaveFormatExtensible()
        {
        }

        /// <summary>
        /// Creates a new WaveFormatExtensible for PCM or IEEE
        /// </summary>
        public WaveFormatExtensible(int rate, int bits, int channels)
            : base(rate, bits, channels)
        {
            waveFormatTag = WaveFormatEncoding.Extensible;
            extraSize = 22;
            wValidBitsPerSample = (short) bits;
            dwChannelMask = channelMaskForChannels(channels);
            if (bits == 32)
            {
                // KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
                subFormat = AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT; // new Guid("00000003-0000-0010-8000-00aa00389b71");
            }
            else
            {
                // KSDATAFORMAT_SUBTYPE_PCM
                subFormat = AudioMediaSubtypes.MEDIASUBTYPE_PCM; // new Guid("00000001-0000-0010-8000-00aa00389b71");
            }

        }

        /// <summary>
        /// WaveFormatExtensible for PCM or floating point can be awkward to work with
        /// This creates a regular WaveFormat structure representing the same audio format
        /// Returns the WaveFormat unchanged for non PCM or IEEE float
        /// </summary>
        /// <returns></returns>
        public WaveFormat ToStandardWaveFormat()
        {
            if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT && bitsPerSample == 32)
                return CreateIeeeFloatWaveFormat(sampleRate, channels);
            if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM)
                return new WaveFormat(sampleRate,bitsPerSample,channels);
            return this;
            //throw new InvalidOperationException("Not a recognised PCM or IEEE float format");
        }

        /// <summary>
        /// SubFormat (may be one of AudioMediaSubtypes)
        /// </summary>
        public Guid SubFormat { get { return subFormat; } }

        /// <summary>
        /// Serialize
        /// </summary>
        /// <param name="writer"></param>
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(wValidBitsPerSample);
            writer.Write(dwChannelMask);
            byte[] guid = subFormat.ToByteArray();
            writer.Write(guid, 0, guid.Length);
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0} wBitsPerSample:{1} dwChannelMask:{2} subFormat:{3} extraSize:{4}",
                base.ToString(),
                wValidBitsPerSample,
                dwChannelMask,
                subFormat,
                extraSize);
        }

        /// <summary>
        /// Picks a channel mask for a specified number of channels.
        /// </summary>
        /// <param name="channels">Number of channels.</param>
        /// <returns>Channel Mask</returns>
        private static int channelMaskForChannels(int channels)
        {
            if (channels == 8 && System.Environment.OSVersion.Version.Major >= 6)
            {
                // For 8 channels, the below logic would return 0xFF (KSAUDIO_SPEAKER_7POINT1) which is
                // not supported in Vista and later. Using 0x63F (KSAUDIO_SPEAKER_7POINT1_SURROUND) instead.
                // https://msdn.microsoft.com/en-us/library/windows/hardware/ff536440(v=vs.85).aspx
                return 0x63F;
            }
            else
            {
                int channelMask = 0;
                for (int n = 0; n < channels; n++)
                {
                    channelMask |= (1 << n);
                }
                return channelMask;
            }
        }
    }
}
