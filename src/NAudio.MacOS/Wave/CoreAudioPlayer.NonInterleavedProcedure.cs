
using System;
using System.Runtime.CompilerServices;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private sealed class NonInterleavedProcedure : PlayerProcedure
    {
        // bufferStride: Number of bytes to skip in order 
        // to access the next sample of the same channel.
        private uint bufferStride;
        private uint channelCount;
        private uint bytesPerFrame;
        private bool bufferSizeChecked;
        private byte[] interleavedBuffer;

        public NonInterleavedProcedure(AudioDevice dev) : base(dev) { }

        protected unsafe override bool ProvideData(uint cBuffers, nint outOutputData, IPlayerSource source)
        {
            AudioBuffer buffer;
            uint framesToFill = 0U;
            for (uint I = 0U; I < cBuffers; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(outOutputData, I);
                if (buffer.mData == IntPtr.Zero)
                {
                    // Unused stream, move to the next one
                    continue;
                }
                // Compute number of frames to put into the HAL buffer.
                framesToFill = buffer.mDataByteSize / bytesPerFrame;
                // Resize the buffer if required, if this is the first stream for this invocation.
                ResizeBufferIfRequired(framesToFill);
                // Read from the source into our temporary interleaved buffer.
                uint read = ReadFromProvider(source, framesToFill);
                if (read == 0U) { bufferSizeChecked = false; return true; }
                // Reassign frames to fill to bound it by the number of read frames.
                framesToFill = Math.Min(framesToFill, read / bufferStride);
                break;
            }
            for (uint I = 0U, J = 0U; I < cBuffers; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(outOutputData, I);
                if (buffer.mData == IntPtr.Zero)
                {
                    // Unused stream, move to the next one
                    continue;
                }
                // Pass the data to the current HAL buffer.
                for (uint K = 0U, BI = J * bytesPerFrame; K < framesToFill; K++, BI += bufferStride)
                {
                    Unsafe.CopyBlockUnaligned(
                        // Note that this reinterpretation is safe here:
                        // We are reinterpeting an unmanaged 
                        // memory block which cannot be moved.
                        ref *((byte*)buffer.mData.ToPointer() + (K * bytesPerFrame)), // Equivalent to: ref Unsafe.Add(ref Unsafe.AsRef<byte>(buffer.mData.ToPointer()), (int)(K * bytesPerFrame))
                        ref interleavedBuffer[BI],
                        bytesPerFrame
                    );
                }
                J++;
            }
            bufferSizeChecked = false;
            return false;
        }

        // Reads interleaved data from the given source,
        // ensuring that the interleaved buffer is appropriately filled.
        private uint ReadFromProvider(IPlayerSource source, uint framesToFill)
        {
            int read;
            uint totallyRead = 0U;
            Span<byte> bufferTarget = interleavedBuffer.AsSpan(0, (int)(framesToFill * bufferStride));
            // Make sure to fill the entire intermediate buffer, if possible.
            while (bufferTarget.Length > 0)
            {
                read = source.Read(bufferTarget);
                if (read == 0) { break; }
                totallyRead += (uint)read;
                bufferTarget = bufferTarget.Slice(read);
            }
            return totallyRead;
        }

        private void ResizeBufferIfRequired(uint numberOfSamplesToEnsure)
        {
            if (bufferSizeChecked) { return; }
            var byteCount = bufferStride * numberOfSamplesToEnsure;
            if (interleavedBuffer is null || byteCount > interleavedBuffer.LongLength)
            {
                interleavedBuffer = new byte[byteCount];
            }
            bufferSizeChecked = true;
        }

        public override IPlayerSource Source
        {
            set
            {
                var asbd = value.ReinterpretedFormat;
                bufferStride = asbd.mBytesPerFrame;
                channelCount = asbd.mChannelsPerFrame;
                bytesPerFrame = bufferStride / channelCount;
                // Allocate 15 samples for each channel.
                // We will increase the size of this buffer if so required.
                ResizeBufferIfRequired(15U);
                bufferSizeChecked = false;
                base.Source = value;
            }
        }
    }
}