using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace NAudioDemo.NetworkChatDemo
{
    class UncompressedPcmChatCodec : INetworkChatCodec
    {
        public UncompressedPcmChatCodec()
        {
            this.RecordFormat = new WaveFormat(8000, 16, 1);
        }
        public string Name { get { return "PCM 8kHz 16 bit uncompressed (128kbps)"; } }
        public WaveFormat RecordFormat { get; private set; }
        public byte[] Encode(byte[] data, int offset, int length)
        {
            byte[] encoded = new byte[length];
            Array.Copy(data, offset, encoded, 0, length);
            return encoded;
        }
        public byte[] Decode(byte[] data) { return data; }
        public void Dispose() { }
    }
}
