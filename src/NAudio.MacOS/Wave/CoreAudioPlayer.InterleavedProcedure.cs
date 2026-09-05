
using System;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private sealed class InterleavedProcedure : PlayerProcedure
    {
        private uint enabledBufferIndex;

        public InterleavedProcedure(AudioDevice dev) : base(dev)
        {
            enabledBufferIndex = 0U;
        }

        // Index of the one buffer this procedure is allowed to fill, matching
        // the single stream enabled through SetStreamUsage. The output
        // AudioBufferList always carries a buffer per stream of the device,
        // disabled streams included, and the HAL discards anything written
        // into those - so filling them all would consume the source once per
        // stream and throw away everything but this one.
        public uint EnabledBufferIndex
        {
            get => enabledBufferIndex;
            set => enabledBufferIndex = value;
        }

        protected override bool ProvideData(uint cBuffers, nint outOutputData, IPlayerSource source)
        {
            int read;
            // Make sure that during streams change, we won't attempt
            // to write bytes somewhere we cannot write.
            if (enabledBufferIndex > cBuffers) { return true; }
            var buffer = AudioBufferList
                .GetAudioBufferFromPointer(outOutputData, enabledBufferIndex);
            // Otherwise, we could get an access violation.
            if (buffer.mData == IntPtr.Zero) { return true; }
            // Get the buffer to write data to.
            Span<byte> allocatedSpan =
                AudioBufferList
                .GetAudioBufferFromPointer(outOutputData, enabledBufferIndex)
                .GetSpan();
            // Make sure that the buffer is completely filled.
            while (allocatedSpan.Length > 0)
            {
                read = source.Read(allocatedSpan);
                if (read == 0) { return true; }
                allocatedSpan = allocatedSpan.Slice(read);
            }
            return false;
        }
    }
}