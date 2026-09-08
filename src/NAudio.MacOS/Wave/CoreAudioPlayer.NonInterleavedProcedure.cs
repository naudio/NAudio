
using System;
using System.Runtime.CompilerServices;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private sealed class NonInterleavedProcedure : PlayerProcedure
    {
        private uint bytesPerFrame;
        private byte[] interleavedBuffer;
        // interleavedBufferStride: Number of bytes to skip in order 
        // to access the next sample of the same channel in the
        // interleaved buffer.
        private uint interleavedBufferStride;

        public NonInterleavedProcedure(AudioDevice dev) : base(dev) { }

        protected override unsafe bool ProvideData(uint cBuffers, nint outOutputData, IPlayerSource source)
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
                framesToFill = (buffer.mDataByteSize / bytesPerFrame) / buffer.mNumberChannels;
                break;
            }
            // Resize the buffer if required.
            ResizeBufferIfRequired(framesToFill);
            // Read from the source into our temporary interleaved buffer.
            uint read = ReadFromProvider(source, framesToFill);
            if (read == 0U) { return true; }
            // Reassign frames to fill to bound it by the number of read frames.
            framesToFill = Math.Min(framesToFill, read / interleavedBufferStride);

            // Now place non-interleaved data into the player's buffers.
            for (uint I = 0U, J = 0U; I < cBuffers; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(outOutputData, I);
                if (buffer.mData == IntPtr.Zero)
                {
                    // Unused stream, move to the next one
                    continue;
                }
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
                    // Pass the data to the current HAL buffer.
                    uint channelIndex = nch * bytesPerFrame;
                    for (uint K = 0U, BI = J * bytesPerFrame; K < framesToFill; K++, BI += interleavedBufferStride)
                    {
                        Unsafe.CopyBlockUnaligned(
                            // Note that this reinterpretation is safe here:
                            // We are reinterpeting an unmanaged 
                            // memory block which cannot be moved.
                            ref *((byte*)buffer.mData.ToPointer() + (K * sampleStride + channelIndex)), // Equivalent to: ref Unsafe.Add(ref Unsafe.AsRef<byte>(buffer.mData.ToPointer()), (int)(K * channelStride + channelIndex))
                            ref interleavedBuffer[BI],
                            bytesPerFrame
                        );
                    }
                }
            }
            return false;
        }

        // Reads interleaved data from the given source,
        // ensuring that the interleaved buffer is appropriately filled.
        private uint ReadFromProvider(IPlayerSource source, uint framesToFill)
        {
            int read;
            uint totallyRead = 0U;
            Span<byte> bufferTarget = interleavedBuffer.AsSpan(0, (int)(framesToFill * interleavedBufferStride));
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
            var byteCount = interleavedBufferStride * numberOfSamplesToEnsure;
            if (interleavedBuffer is null || byteCount > interleavedBuffer.LongLength)
            {
                interleavedBuffer = new byte[byteCount];
            }
        }

        public override IPlayerSource Source
        {
            set
            {
                var asbd = value.ReinterpretedFormat;
                // We will always receieve a format that is interleaved -
                // so no need to check for the non-interleaved flag.
                interleavedBufferStride = asbd.mBytesPerFrame;
                bytesPerFrame = interleavedBufferStride / asbd.mChannelsPerFrame;
                // Allocate 15 samples for each channel.
                // We will increase the size of this buffer if so required.
                ResizeBufferIfRequired(15U);
                base.Source = value;
            }
        }
    }
}