using System;
using System.Linq;
using NAudio.Wave;
using NAudio.Codecs;

namespace NAudioDemo.NetworkChatDemo
{
    class AcmMuLawChatCodec : AcmChatCodec
    {
        public AcmMuLawChatCodec()
            : base (new WaveFormat(8000,16,1), WaveFormat.CreateMuLawFormat(8000,1))
        {
        }

        public override string Name => "ACM G.711 mu-law";
    }


    class MuLawChatCodec : INetworkChatCodec
    {
        public string Name => "G.711 mu-law";

        public int BitsPerSecond => RecordFormat.SampleRate * 8;

        public WaveFormat RecordFormat => new WaveFormat(8000, 16, 1);

        public byte[] Encode(byte[] data, int offset, int length)
        {
            var encoded = new byte[length / 2];
            int outIndex = 0;
            for(int n = 0; n < length; n+=2)
            {
                encoded[outIndex++] = MuLawEncoder.LinearToMuLawSample(BitConverter.ToInt16(data, offset + n));
            }
            return encoded;
        }

        public byte[] Decode(byte[] data, int offset, int length)
        {
            var decoded = new byte[length * 2];
            int outIndex = 0;
            for (int n = 0; n < length; n++)
            {
                short decodedSample = MuLawDecoder.MuLawToLinearSample(data[n + offset]);
                decoded[outIndex++] = (byte)(decodedSample & 0xFF);
                decoded[outIndex++] = (byte)(decodedSample >> 8);
            }
            return decoded;
        }

        public void Dispose()
        {
            // nothing to do
        }

        public bool IsAvailable { get { return true; } }

    }
}
