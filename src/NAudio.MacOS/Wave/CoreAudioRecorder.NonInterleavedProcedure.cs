
using System;
using System.Runtime.CompilerServices;

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
        private uint bytesPerFrame;
        private byte[] temporaryBuffer;
        private uint targetBufferStride;

        public NonInterleavedProcedure(AudioDevice dev) : base(dev) { }

        protected override unsafe void OnProvideData(uint bufferCount, nint inInputData)
        {
            AudioBuffer buffer;
            // Find out the number of frames that the HAL gives to us.
            uint nFramesToGet = 0U;
            for (uint I = 0; I < bufferCount; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(inInputData, I);
                if (buffer.mData == IntPtr.Zero) { continue; }
                // Compute number of frames to get from the HAL buffer.
                nFramesToGet = (buffer.mDataByteSize / bytesPerFrame) / buffer.mNumberChannels;
                break;
            }
            // If we need to enlarge our temporary buffer, we do so.
            ResizeBufferIfRequired(nFramesToGet);

            for (uint I = 0U, J = 0U; I < bufferCount; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(inInputData, I);
                if (buffer.mData == IntPtr.Zero) { continue; } // Skip any stream that is not used.
                // The HAL buffer rep for a single frame is the below on non-interleaved audio:
                //
                // |----|----|----|----|
                // |CH 0|CH 1|CH 2|CH n|
                // |----|----|----|----|
                // |---sampleStride----| // The size of the sample in bytes is sampleStride = bytesPerFrame * bufferChannelCount.
                //
                // To go to the next channel, a jump by bytesPerFrame bytes is required.
                // To go to first channel of the next sample, a jump by bytesPerFrame * bufferChannelCount is required.
                // So, accessing a sample at e.g. channel 2 in the fifth frame would require this formula:
                // 5 * bytesPerFrame * bufferChannelCount + (2 * bytesPerFrame)
                uint bufferChannelCount = buffer.mNumberChannels;
                uint sampleStride = bytesPerFrame * bufferChannelCount;
                for (uint nch = 0U; nch < bufferChannelCount; nch++, J++)
                {
                    // Copy the HAL data to our buffer that we build as an interleaved buffer.
                    // F: Number of frames iterated on the current HAL buffer iteration.
                    // BI: The interleaved buffer index. Jumps by the target buffer stride,
                    // and initialized by the number of channels iterated multiplied
                    // by the number of bytes deduced from the bits-per-sample field.
                    uint channelIndex = nch * bytesPerFrame;
                    for (uint F = 0U, BI = J * bytesPerFrame; F < nFramesToGet; F++, BI += targetBufferStride)
                    {
                        Unsafe.CopyBlockUnaligned(
                            ref temporaryBuffer[BI],
                            // Note that this reinterpretation is safe here:
                            // We are reinterpeting an unmanaged
                            // memory block which cannot be moved.
                            ref *((byte*)buffer.mData.ToPointer() + (F * sampleStride + channelIndex)), // Equivalent to: ref Unsafe.Add(ref Unsafe.AsRef<byte>(buffer.mData.ToPointer()), (int)(F * bytesPerFrame * bufferChannelCount))
                            bytesPerFrame
                        );
                    }
                }
            }
            OnDataAvailable(temporaryBuffer.AsSpan(0, (int)(nFramesToGet * targetBufferStride)));
        }

        private void ResizeBufferIfRequired(uint numberOfSamplesToEnsure)
        {
            var byteCount = targetBufferStride * numberOfSamplesToEnsure;
            if (temporaryBuffer is null || byteCount > temporaryBuffer.LongLength)
            {
                temporaryBuffer = new byte[byteCount];
            }
        }

        public override AudioStreamBasicDescription VirtualFormat
        {
            set
            {
                bool isNonInterleaved = value.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsNonInterleaved);
                bytesPerFrame = isNonInterleaved
                    ? value.mBytesPerFrame
                    : value.mBytesPerFrame / value.mChannelsPerFrame;
                targetBufferStride = isNonInterleaved ? value.mBytesPerFrame * value.mChannelsPerFrame : value.mBytesPerFrame;
                // Allocate 15 samples for each channel.
                // We will increase the size of this buffer if so required.
                ResizeBufferIfRequired(15U);
                base.VirtualFormat = value;
            }
        }
    }
}