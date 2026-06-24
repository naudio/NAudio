using System;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Codecs;
using System.Diagnostics;

namespace NAudioDemo.NetworkChatDemo
{
    class G722ChatCodec : INetworkChatCodec
    {
        private readonly int bitrate;
        private readonly G722CodecState encoderState;
        private readonly G722CodecState decoderState;
        private readonly G722Codec codec;

        public G722ChatCodec()
        {
            bitrate = 64000;
            encoderState = new G722CodecState(bitrate, G722Flags.None);
            decoderState = new G722CodecState(bitrate, G722Flags.None);
            codec = new G722Codec();
            RecordFormat = new WaveFormat(16000, 1);
        }

        public string Name => "G.722 16kHz";

        public int BitsPerSecond => bitrate;

        public WaveFormat RecordFormat { get; }

        public byte[] Encode(byte[] data, int offset, int length)
        {
            if (offset != 0)
            {
                throw new ArgumentException("G722 does not yet support non-zero offsets");
            }
            int sampleCount = length / 2;
            var samples = new short[sampleCount];
            MemoryMarshal.Cast<byte, short>(data.AsSpan(0, length)).CopyTo(samples);
            int encodedLength = length / 4;
            var outputBuffer = new byte[encodedLength];
            int encoded = codec.Encode(encoderState, outputBuffer, samples, sampleCount);
            Debug.Assert(encodedLength == encoded);
            return outputBuffer;
        }

        public byte[] Decode(byte[] data, int offset, int length)
        {
            if (offset != 0)
            {
                throw new ArgumentException("G722 does not yet support non-zero offsets");
            }
            int decodedSampleCount = length * 2;
            var decodedSamples = new short[decodedSampleCount];
            int decoded = codec.Decode(decoderState, decodedSamples, data, length);
            Debug.Assert(decodedSampleCount == decoded);
            var outputBuffer = new byte[decoded * 2];
            MemoryMarshal.AsBytes(decodedSamples.AsSpan(0, decoded)).CopyTo(outputBuffer);
            return outputBuffer;
        }

        public void Dispose()
        {
            // nothing to do
        }

        public bool IsAvailable => true;
    }
}
