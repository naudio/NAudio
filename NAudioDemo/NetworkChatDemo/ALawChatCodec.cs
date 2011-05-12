using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudio.Codecs;

namespace NAudioDemo.NetworkChatDemo
{
    class AcmALawChatCodec : AcmChatCodec
    {
        public AcmALawChatCodec()
            : base(new WaveFormat(8000, 16, 1), WaveFormat.CreateALawFormat(8000, 1))
        {
        }

        public override string Name
        {
            get { return "ACM G.711 a-law (64kbps)"; }
        }
    }


    class ALawChatCodec : INetworkChatCodec
    {
        public string Name
        {
            get { return "G.711 a-law (64kbps)"; }
        }

        public WaveFormat RecordFormat
        {
            get { return new WaveFormat(8000, 16, 1); }
        }

        public byte[] Encode(byte[] data, int offset, int length)
        {
            byte[] encoded = new byte[length / 2];
            int outIndex = 0;
            for (int n = 0; n < length; n += 2)
            {
                encoded[outIndex++] = ALawEncoder.LinearToALawSample(BitConverter.ToInt16(data, offset + n));
            }
            return encoded;
        }

        public byte[] Decode(byte[] data)
        {
            byte[] decoded = new byte[data.Length * 2];
            int outIndex = 0;
            for (int n = 0; n < data.Length; n++)
            {
                short decodedSample = ALawDecoder.ALawToLinearSample(data[n]);
                decoded[outIndex++] = (byte)(decodedSample & 0xFF);
                decoded[outIndex++] = (byte)(decodedSample >> 8);
            }
            return decoded;
        }

        public void Dispose()
        {
            // nothing to do
        }
    }
}
