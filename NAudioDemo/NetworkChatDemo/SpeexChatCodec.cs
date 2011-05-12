using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NSpeex;

namespace NAudioDemo.NetworkChatDemo
{
    class SpeexChatCodec : INetworkChatCodec
    {
        private WaveFormat recordingFormat;
        private SpeexDecoder decoder;
        private SpeexEncoder encoder;

        public SpeexChatCodec()
        {
            this.decoder = new SpeexDecoder(BandMode.Narrow);
            this.encoder = new SpeexEncoder(BandMode.Narrow);
            this.recordingFormat = new WaveFormat(8000, 16, 1);
        }

        public string Name
        {
            get { return "Speex Narrow Band"; }
        }

        public int BitsPerSecond
        {
            // don't know yet
            get { return 8000; }
        }

        public WaveFormat RecordFormat
        {
            get { return recordingFormat; }
        }

        public byte[] Encode(byte[] data, int offset, int length)
        {
            byte[] outputBufferTemp = new byte[length]; // easily big enough
            WaveBuffer wb = new WaveBuffer(data);
            int bytesWritten = encoder.Encode(wb.ShortBuffer, offset / 2, length / 2, outputBufferTemp, 0, length);
            byte[] encoded = new byte[bytesWritten];
            Array.Copy(outputBufferTemp, 0, encoded, 0, bytesWritten);
            return encoded;
        }

        public byte[] Decode(byte[] data)
        {
            byte[] outputBufferTemp = new byte[data.Length * 16]; // easily big enough
            WaveBuffer wb = new WaveBuffer(outputBufferTemp);
            int samplesWritten = decoder.Decode(data, 0, data.Length, wb.ShortBuffer, 0, false);
            int bytesWritten = samplesWritten * 2;
            byte[] decoded = new byte[bytesWritten];
            Array.Copy(outputBufferTemp, 0, decoded, 0, bytesWritten);
            return decoded;
        }

        public void Dispose()
        {
            // nothing to do
        }
    }
}
