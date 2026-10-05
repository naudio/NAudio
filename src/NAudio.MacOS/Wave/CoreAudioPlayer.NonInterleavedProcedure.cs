
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
                if (buffer.mNumberChannels == 1U)
                {
                    _ = deinterleaver.GetChannelBytes(C++, buffer.GetSpan());
                }
                else
                {
                    // The deinterleaver works only with single-channel buffers
                    for (uint M = 0U; M < buffer.mNumberChannels; M++, C++)
                    {
                        deinterleaver.ClearChannelBuffer(C);
                    }
                }
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
                // Allocate 15 samples for each channel.
                // We will increase the size of this buffer if so required.
                ResizeTemporaryBuffer(15 * deinterleaver.BytesPerFrame);
                base.Source = value;
            }
        }
    }
}
