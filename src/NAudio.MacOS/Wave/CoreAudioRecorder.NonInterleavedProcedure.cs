
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

        public override void DrainBuffers()
        {
            while (interleaver.Drain())
            {
                OnDataAvailable(interleaver.GetBuffer());
            }
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
                // Pre-size the building buffer to avoid as many resizes as possible.
                // If there is the need, however, at some time to do so, it is done by the interleaver
                // when the new buffer is being built.
                interleaver.SetBuildingBufferCapacity(
                    (int)((isNonInterleaved ? value.mChannelsPerFrame : 1U) * value.mBytesPerFrame)
                    * MaximumIOBufferSizeInFrames
                );
                base.VirtualFormat = value;
            }
        }
    }
}
