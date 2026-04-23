using System;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NSpeex;
using System.Diagnostics;

namespace NAudioDemo.NetworkChatDemo
{
    class NarrowBandSpeexCodec : SpeexChatCodec
    {
        public NarrowBandSpeexCodec() : 
            base(BandMode.Narrow, 8000, "Speex Narrow Band")
        {
        
        }
    }

    class WideBandSpeexCodec : SpeexChatCodec
    {
        public WideBandSpeexCodec() : 
            base(BandMode.Wide, 16000, "Speex Wide Band (16kHz)")
        {

        }
    }

    class UltraWideBandSpeexCodec : SpeexChatCodec
    {
        public UltraWideBandSpeexCodec() :
            base(BandMode.UltraWide, 32000, "Speex Ultra Wide Band (32kHz)")
        {

        }
    }

    abstract class SpeexChatCodec : INetworkChatCodec
    {
        private readonly WaveFormat recordingFormat;
        private readonly SpeexDecoder decoder;
        private readonly SpeexEncoder encoder;
        private readonly short[] encoderInputBuffer;
        private int encoderInputSamples;
        private readonly string description;

        protected SpeexChatCodec(BandMode bandMode, int sampleRate, string description)
        {
            decoder = new SpeexDecoder(bandMode);
            encoder = new SpeexEncoder(bandMode);
            recordingFormat = new WaveFormat(sampleRate, 16, 1);
            this.description = description;
            encoderInputBuffer = new short[recordingFormat.AverageBytesPerSecond / 2]; // more than enough
        }

        public string Name => description;

        public int BitsPerSecond => -1;

        public WaveFormat RecordFormat => recordingFormat;

        public byte[] Encode(byte[] data, int offset, int length)
        {
            FeedSamplesIntoEncoderInputBuffer(data, offset, length);
            int samplesToEncode = encoderInputSamples;
            if (samplesToEncode % encoder.FrameSize != 0)
            {
                samplesToEncode -= samplesToEncode % encoder.FrameSize;
            }
            var outputBufferTemp = new byte[length]; // contains more than enough space
            int bytesWritten = encoder.Encode(encoderInputBuffer, 0, samplesToEncode, outputBufferTemp, 0, length);
            var encoded = new byte[bytesWritten];
            Array.Copy(outputBufferTemp, 0, encoded, 0, bytesWritten);
            ShiftLeftoverSamplesDown(samplesToEncode);
            Debug.WriteLine(
                $"NSpeex: In {length} bytes, encoded {bytesWritten} bytes [enc frame size = {encoder.FrameSize}]");
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
            var decodedShorts = new short[length * 160]; // 320 bytes-per-input-byte / 2 bytes-per-short
            int samplesDecoded = decoder.Decode(data, offset, length, decodedShorts, 0, false);
            int bytesDecoded = samplesDecoded * 2;
            var decoded = new byte[bytesDecoded];
            MemoryMarshal.AsBytes(decodedShorts.AsSpan(0, samplesDecoded)).CopyTo(decoded);
            Debug.WriteLine(
                $"NSpeex: In {length} bytes, decoded {bytesDecoded} bytes [dec frame size = {decoder.FrameSize}]");
            return decoded;
        }

        public void Dispose()
        {
            // nothing to do
        }

        public bool IsAvailable => true;
    }
}
