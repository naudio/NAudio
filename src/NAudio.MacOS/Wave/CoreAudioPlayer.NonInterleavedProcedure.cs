
using System;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private sealed class NonInterleavedProcedure : PlayerProcedure
    {
        private uint nonInterleavedStride;
        private byte[] temporaryInterleavedBuffer;
        private readonly NAudio.Utils.AudioDeinterleaver deinterleaver;

        public NonInterleavedProcedure(AudioDevice dev)
            : base(dev)
        {
            deinterleaver = new();
            nonInterleavedStride = 0U;
            temporaryInterleavedBuffer = null;
        }

        protected override unsafe bool ProvideData(uint cBuffers, nint outOutputData, IPlayerSource source)
        {
            int framesToFill = 0; // Maximum number of frames to fill.
            AudioBuffer buffer;
            for (uint I = 0U; I < cBuffers; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(outOutputData, I);
                if (buffer.mData == IntPtr.Zero)
                {
                    // Unused stream, move to the next one
                    continue;
                }
                // Compute number of frames to put into the HAL buffer.
                // Note that we are doing a max frames run because
                // we want to fill the HAL buffers as much as possible.
                // If we didn't had that target the buffers wouldn't be completely filled,
                // and as such, at the end of each buffer there would be silence instead of valid samples.
                var fs = (int)(buffer.mDataByteSize / nonInterleavedStride / buffer.mNumberChannels);

                if (fs > framesToFill) { framesToFill = fs; }
            }
            // Resize the buffer if required.
            ResizeTemporaryBuffer(framesToFill * deinterleaver.BytesPerFrame);

            // Read data.
            int read = ReadFromProvider(source, framesToFill);
            if (read == 0) { return true; }
            deinterleaver.FillFromInterleaved(temporaryInterleavedBuffer.AsSpan(0, read));

            int C = 0;
            for (uint I = 0U; I < cBuffers; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(outOutputData, I);
                if (buffer.mData == IntPtr.Zero)
                {
                    // Unused stream, move to the next one
                    continue;
                }
                // Since we have excluded from the initialization stage
                // the multiple-channel buffers, we can go ahead and read the channel's bytes directly.
                _ = deinterleaver.GetChannelBytes(C++, buffer.GetSpan());
            }

            return false;
        }

        // Reads interleaved data from the given source,
        // ensuring that the interleaved buffer is appropriately filled.
        private int ReadFromProvider(IPlayerSource source, int framesToFill)
        {
            int read;
            int totallyRead = 0;
            Span<byte> bufferTarget = temporaryInterleavedBuffer.AsSpan(0, framesToFill * deinterleaver.BytesPerFrame);
            // Make sure to fill the entire intermediate buffer, if possible.
            while (bufferTarget.Length > 0)
            {
                read = source.Read(bufferTarget);
                if (read == 0) { break; }
                totallyRead += read;
                bufferTarget = bufferTarget.Slice(read);
            }
            return totallyRead;
        }

        private void ResizeTemporaryBuffer(int newSize)
        {
            if (temporaryInterleavedBuffer is null || newSize > temporaryInterleavedBuffer.Length)
            {
                temporaryInterleavedBuffer = GC.AllocateUninitializedArray<byte>(newSize);
            }
        }

        public override IPlayerSource Source
        {
            set
            {
                var asbd = value.ReinterpretedFormat;
                // We will always recieve a format that is interleaved -
                // so no need to check for the non-interleaved flag.
                deinterleaver.Channels = (int)asbd.mChannelsPerFrame;
                deinterleaver.BytesPerFrame = (int)asbd.mBytesPerFrame;
                nonInterleavedStride = asbd.mBytesPerFrame / asbd.mChannelsPerFrame;
                // Resize the temporary buffer in the maximum number of frames.
                // We will increase the size of this buffer if so required.
                ResizeTemporaryBuffer((int)(MaximumIOBufferSizeInFrames * asbd.mBytesPerFrame));
                base.Source = value;
            }
        }
    }
}
