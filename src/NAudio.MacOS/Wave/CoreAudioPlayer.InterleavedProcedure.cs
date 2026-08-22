
using System;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private sealed class InterleavedProcedure : PlayerProcedure
    {
        public InterleavedProcedure(AudioDevice dev) : base(dev) { }

        protected override bool ProvideData(uint cBuffers, nint outOutputData, IPlayerSource source)
        {
            AudioBuffer buffer;
            for (uint I = 0; I < cBuffers; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(outOutputData, I);
                if (buffer.mData == IntPtr.Zero)
                {
                    // Unused stream, move to the next one
                    continue;
                }
                int read;
                Span<byte> allocatedSpan = buffer.GetSpan();
                while (allocatedSpan.Length > 0)
                {
                    read = source.Read(allocatedSpan);
                    if (read == 0) { return true; }
                    allocatedSpan = allocatedSpan.Slice(read);
                }
            }
            return false;
        }
    }
}