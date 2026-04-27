using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Concentus;
using Concentus.Enums;
using NAudio.Wave;

namespace NAudioDemo.NetworkChatDemo
{
    class NarrowBandOpusCodec : OpusChatCodec
    {
        public NarrowBandOpusCodec() : base(8000, 16000, "Opus Narrow Band (8kHz)")
        {
        }
    }

    class WideBandOpusCodec : OpusChatCodec
    {
        public WideBandOpusCodec() : base(16000, 24000, "Opus Wide Band (16kHz)")
        {
        }
    }

    class FullBandOpusCodec : OpusChatCodec
    {
        public FullBandOpusCodec() : base(48000, 48000, "Opus Full Band (48kHz)")
        {
        }
    }

    abstract class OpusChatCodec : INetworkChatCodec
    {
        // 20ms frames are the de-facto standard for VoIP and supported by all valid Opus sample rates.
        private const int FrameDurationMs = 20;
        // Opus packets are guaranteed to fit in 1275 bytes for a single 20ms frame.
        private const int MaxEncodedPacketBytes = 1275;

        private readonly WaveFormat recordingFormat;
        private readonly IOpusEncoder encoder;
        private readonly IOpusDecoder decoder;
        private readonly int frameSizeSamples;
        private readonly short[] encoderInputBuffer;
        private int encoderInputSamples;
        private readonly string description;
        private readonly int bitrate;

        protected OpusChatCodec(int sampleRate, int bitrate, string description)
        {
            recordingFormat = new WaveFormat(sampleRate, 16, 1);
            encoder = OpusCodecFactory.CreateEncoder(sampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
            encoder.Bitrate = bitrate;
            decoder = OpusCodecFactory.CreateDecoder(sampleRate, 1);
            frameSizeSamples = sampleRate * FrameDurationMs / 1000;
            encoderInputBuffer = new short[frameSizeSamples * 4]; // generous headroom
            this.description = description;
            this.bitrate = bitrate;
        }

        public string Name => description;

        public int BitsPerSecond => bitrate;

        public WaveFormat RecordFormat => recordingFormat;

        public byte[] Encode(byte[] data, int offset, int length)
        {
            FeedSamplesIntoEncoderInputBuffer(data, offset, length);

            // Opus encodes one frame per call. For typical chat buffer sizes one Encode() call yields
            // at most a couple of frames, so concatenating into a small list keeps things simple.
            using var output = new System.IO.MemoryStream();
            var packetBuffer = new byte[MaxEncodedPacketBytes];
            int consumedSamples = 0;
            while (encoderInputSamples - consumedSamples >= frameSizeSamples)
            {
                int encodedBytes = encoder.Encode(
                    encoderInputBuffer.AsSpan(consumedSamples, frameSizeSamples),
                    frameSizeSamples,
                    packetBuffer,
                    packetBuffer.Length);

                // Length-prefix each packet so the decoder can split a multi-frame payload back apart.
                output.WriteByte((byte)(encodedBytes >> 8));
                output.WriteByte((byte)(encodedBytes & 0xFF));
                output.Write(packetBuffer, 0, encodedBytes);
                consumedSamples += frameSizeSamples;
            }
            ShiftLeftoverSamplesDown(consumedSamples);

            var encoded = output.ToArray();
            Debug.WriteLine($"Opus: In {length} bytes, encoded {encoded.Length} bytes [frame size = {frameSizeSamples} samples]");
            return encoded;
        }

        private void ShiftLeftoverSamplesDown(int samplesEncoded)
        {
            int leftoverSamples = encoderInputSamples - samplesEncoded;
            Array.Copy(encoderInputBuffer, samplesEncoded, encoderInputBuffer, 0, leftoverSamples);
            encoderInputSamples = leftoverSamples;
        }

        private void FeedSamplesIntoEncoderInputBuffer(byte[] data, int offset, int length)
        {
            var incomingShorts = MemoryMarshal.Cast<byte, short>(data.AsSpan(offset, length));
            incomingShorts.CopyTo(encoderInputBuffer.AsSpan(encoderInputSamples));
            encoderInputSamples += incomingShorts.Length;
        }

        public byte[] Decode(byte[] data, int offset, int length)
        {
            // Decode one packet at a time into a per-packet buffer so the destination span is always
            // large enough; accumulate the bytes into a single MemoryStream regardless of how many
            // frames are in the payload.
            var perPacketBuffer = new short[frameSizeSamples];
            using var output = new System.IO.MemoryStream();
            int cursor = offset;
            int end = offset + length;
            while (cursor + 2 <= end)
            {
                int packetLength = (data[cursor] << 8) | data[cursor + 1];
                cursor += 2;
                if (cursor + packetLength > end) break;
                int samples = decoder.Decode(
                    data.AsSpan(cursor, packetLength),
                    perPacketBuffer,
                    frameSizeSamples,
                    false);
                output.Write(MemoryMarshal.AsBytes(perPacketBuffer.AsSpan(0, samples)));
                cursor += packetLength;
            }

            var decoded = output.ToArray();
            Debug.WriteLine($"Opus: In {length} bytes, decoded {decoded.Length} bytes [frame size = {frameSizeSamples} samples]");
            return decoded;
        }

        public void Dispose()
        {
            // nothing to do
        }

        public bool IsAvailable => true;
    }
}
