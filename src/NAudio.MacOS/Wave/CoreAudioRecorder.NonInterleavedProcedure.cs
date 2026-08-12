
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
        private uint bytesPerSampleDerivedFromBits;

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
                nFramesToGet = buffer.mDataByteSize / bytesPerFrame;
                break;
            }
            // If we need to enlarge our temporary buffer, we do so.
            ResizeBufferIfRequired(nFramesToGet);

            for (uint I = 0U, J = 0U; I < bufferCount; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(inInputData, I);
                if (buffer.mData == IntPtr.Zero) { continue; } // Skip any stream that is not used.
                // Copy the HAL data to our buffer that we build as an interleaved buffer.
                // F: Number of frames iterated on the current HAL buffer iteration.
                // BI: The interleaved buffer index. Jumps by the target buffer stride,
                // and initialized by the number of channels iterated multiplied
                // by the number of bytes deduced from the bits-per-sample field.
                for (uint F = 0U, BI = J * bytesPerSampleDerivedFromBits; F < nFramesToGet; F++, BI += targetBufferStride)
                {
                    Unsafe.CopyBlockUnaligned(
                        ref temporaryBuffer[BI],
                        // Note that this reinterpretation is safe here:
                        // We are reinterpeting an unmanaged
                        // memory block which cannot be moved.
                        ref *((byte*)buffer.mData.ToPointer() + (F * bytesPerFrame)), // Equivalent to: ref Unsafe.Add(ref Unsafe.AsRef<byte>(buffer.mData.ToPointer()), (int)(F * bytesPerFrame))
                        bytesPerSampleDerivedFromBits
                    );
                }
                J++;
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
                bytesPerFrame = value.mBytesPerFrame;
                bytesPerSampleDerivedFromBits = value.mBitsPerChannel / 8U;
                targetBufferStride = value.mChannelsPerFrame * bytesPerSampleDerivedFromBits;
                // Allocate 15 samples for each channel.
                // We will increase the size of this buffer if so required.
                ResizeBufferIfRequired(15U);
                base.VirtualFormat = value;
            }
        }
    }
}