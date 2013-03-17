using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudio.Codecs;
using System.Diagnostics;
using System.ComponentModel.Composition;

namespace NAudioDemo.NetworkChatDemo
{
    [Export(typeof(INetworkChatCodec))]
    class G722ChatCodec : INetworkChatCodec
    {
        private int bitrate;
        private G722CodecState encoderState;
        private G722CodecState decoderState;
        private WaveFormat recordingFormat;
        private G722Codec codec;

        public G722ChatCodec()
        {
            this.bitrate = 64000;
            this.encoderState = new G722CodecState(bitrate, G722Flags.None);
            this.decoderState = new G722CodecState(bitrate, G722Flags.None);
            this.codec = new G722Codec();
            this.recordingFormat = new WaveFormat(16000, 1);
        }

        public string Name
        {
            get { return "G.722 16kHz"; }
        }

        public int BitsPerSecond
        {
            get { return this.bitrate; }
        }

        public WaveFormat RecordFormat
        {
            get { return this.recordingFormat; }
        }

        public byte[] Encode(byte[] data, int offset, int length)
        {
            if (offset != 0)
            {
                throw new ArgumentException("G722 does not yet support non-zero offsets");
            }
            WaveBuffer wb = new WaveBuffer(data);
            int encodedLength = length / 4;
            byte[] outputBuffer = new byte[encodedLength];
            int encoded = this.codec.Encode(this.encoderState, outputBuffer, wb.ShortBuffer, length / 2);
            Debug.Assert(encodedLength == encoded);
            return outputBuffer;
        }

        public byte[] Decode(byte[] data, int offset, int length)
        {
            if (offset != 0)
            {
                throw new ArgumentException("G722 does not yet support non-zero offsets");
            }
            int decodedLength = length * 4;
            byte[] outputBuffer = new byte[decodedLength];
            WaveBuffer wb = new WaveBuffer(outputBuffer);
            int decoded = this.codec.Decode(this.decoderState, wb.ShortBuffer, data, length);
            Debug.Assert(decodedLength == decoded * 2);  // because decoded is a number of samples
            return outputBuffer;
        }

        public void Dispose()
        {
            // nothing to do
        }

        public bool IsAvailable { get { return true; } }
    }
}
