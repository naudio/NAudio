
using System;
using System.Runtime.CompilerServices;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private sealed class NonInterleavedProcedure : PlayerProcedure
    {
        private uint channelCount;
        private uint bytesPerFrame;
        private bool bufferSizeChecked;
        private byte[] nonInterleavedBuffer;

        public NonInterleavedProcedure(AudioDevice dev) : base(dev) { }

        protected unsafe override bool ProvideData(uint cBuffers, nint outOutputData, IPlayerSource source)
        {
            AudioBuffer buffer;
            // stride: Number of bytes to skip in order to access the next 
            // sample of the same channel.
            uint stride = channelCount * bytesPerFrame;
            for (uint I = 0U, J = 0U; I < cBuffers; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(outOutputData, I);
                if (buffer.mData == IntPtr.Zero)
                {
                    // Unused stream, move to the next one
                    continue;
                }
                // Compute number of frames to put into the HAL buffer.
                uint framesToFill = buffer.mDataByteSize / bytesPerFrame;
                // Resize the buffer if required, if this is the first stream for this invocation.
                ResizeBufferIfRequired(framesToFill);
                // Read from the source into our temporary non-interleaved buffer.
                uint read = (uint)source.Read(nonInterleavedBuffer) / bytesPerFrame;
                if (read == 0U) { bufferSizeChecked = false; return true; }
                uint index = J * bytesPerFrame;
                // Compute the actual number of samples to provide into the HAL buffer.
                read = Math.Min(framesToFill, read);
                while (read > 0U)
                {
                    Unsafe.CopyBlockUnaligned(
                        // Note that this reinterpretation is safe here:
                        // We are reinterpeting an unmanaged 
                        // memory block which cannot be moved.
                        ref *(byte*)buffer.mData.ToPointer(), // Equivalent to: ref Unsafe.AsRef<byte>(buffer.mData.ToPointer())
                        ref nonInterleavedBuffer[index],
                        bytesPerFrame
                    );
                    index += stride;
                    read--;
                }
                J = (++J) % channelCount;
            }
            bufferSizeChecked = false;
            return false;
        }

        private void ResizeBufferIfRequired(uint numberOfSamplesToEnsure)
        {
            if (bufferSizeChecked) { return; }
            var byteCount = channelCount * (long)bytesPerFrame * numberOfSamplesToEnsure;
            if (nonInterleavedBuffer is null || byteCount > nonInterleavedBuffer.LongLength)
            {
                nonInterleavedBuffer = new byte[byteCount];
            }
            bufferSizeChecked = true;
        }

        public override IPlayerSource Source
        {
            set
            {
                var asbd = value.ReinterpretedFormat;
                channelCount = asbd.mChannelsPerFrame;
                bytesPerFrame = asbd.mBytesPerFrame;
                // Allocate 15 samples for each channel.
                // We will increase the size of this buffer if so required.
                ResizeBufferIfRequired(15U);
                bufferSizeChecked = false;
                base.Source = value;
            }
        }
    }
}