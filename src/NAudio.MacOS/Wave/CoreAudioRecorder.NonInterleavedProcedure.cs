
using System;
using System.Runtime.CompilerServices;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioRecorder
{
    private sealed class NonInterleavedProcedure : RecorderProcedure
    {
        private uint bytesPerFrame;
        private byte[] temporaryBuffer;
        private uint targetBufferStride;
        private uint bytesPerSampleDerivedFromBits;

        public NonInterleavedProcedure(AudioDevice dev) : base(dev) { }

        protected unsafe override void OnProvideData(uint bufferCount, nint inInputData)
        {
            AudioBuffer buffer;
            // Find out the number of frames that the HAL gives to us.
            uint nFramesToGet = 0U;
            for (uint I = 0; I < bufferCount; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(inInputData, I);
                if (buffer.mData == IntPtr.Zero) { continue; }
                nFramesToGet = buffer.mDataByteSize / bytesPerFrame;
                break;
            }
            // If we need to enlarge our temporary buffer, we do so.
            if (temporaryBuffer.LongLength < targetBufferStride * nFramesToGet)
            {
                temporaryBuffer = new byte[targetBufferStride * nFramesToGet];
            }
            for (uint I = 0U, J = 0U; I < bufferCount; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(inInputData, I);
                if (buffer.mData == IntPtr.Zero) { continue; } // Skip any stream that is not used.
                // Copy the HAL data to our buffer that we build as an interleaved buffer.
                for (uint F = 0U, BI = J * targetBufferStride; F < nFramesToGet; F++, BI += targetBufferStride)
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

        public override AudioStreamBasicDescription VirtualFormat
        {
            set
            {
                bytesPerFrame = value.mBytesPerFrame;
                bytesPerSampleDerivedFromBits = value.mBitsPerChannel / 8U;
                targetBufferStride = value.mChannelsPerFrame * bytesPerSampleDerivedFromBits;
                temporaryBuffer = new byte[targetBufferStride * 15U];
                base.VirtualFormat = value;
            }
        }
    }
}