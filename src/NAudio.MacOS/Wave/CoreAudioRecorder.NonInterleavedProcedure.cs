
using System;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioRecorder
{
    // A recorder procedure implementation that
    // is utilized when the HAL enforces that the
    // audio buffers should provide the data as non-interleaved,
    // that is, each sample frame is a channel on it's own.
    private sealed class NonInterleavedProcedure : RecorderProcedure
    {
        private readonly NAudio.Utils.AudioInterleaver interleaver;

        public NonInterleavedProcedure(AudioDevice dev) : base(dev) { interleaver = new(); }

        protected override unsafe void OnProvideData(uint bufferCount, nint inInputData)
        {
            int C = 0;
            AudioBuffer buffer;
            interleaver.PrepareNewBuffer();
            // Find out the number of frames that the HAL gives to us.
            for (uint I = 0U; I < bufferCount; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(inInputData, I);
                if (buffer.mData == IntPtr.Zero) { continue; }
                // Add the number of bytes found in this buffer.
                interleaver.AddBufferBytes(C++, (int)buffer.mDataByteSize);
            }
            // If we need to enlarge our temporary buffer, we do so.
            interleaver.BuildNewBuffer();

            C = 0;
            for (uint I = 0U; I < bufferCount; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(inInputData, I);
                if (buffer.mData == IntPtr.Zero) { continue; }
                interleaver.PutBytes(C++, buffer.GetSpan());
            }

            OnDataAvailable(interleaver.GetBuffer());
        }

        public override AudioStreamBasicDescription VirtualFormat
        {
            set
            {
                bool isNonInterleaved = value.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsNonInterleaved);
                interleaver.BytesPerFrame = (int)(isNonInterleaved
                    ? value.mBytesPerFrame
                    : value.mBytesPerFrame / value.mChannelsPerFrame);
                interleaver.Channels = (int)value.mChannelsPerFrame;
                base.VirtualFormat = value;
            }
        }
    }
}
