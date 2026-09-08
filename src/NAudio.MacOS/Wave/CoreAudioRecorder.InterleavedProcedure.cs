
using System;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioRecorder
{
    private sealed class InterleavedProcedure : RecorderProcedure
    {
        public InterleavedProcedure(AudioDevice dev) : base(dev) { }

        protected override void OnProvideData(uint bufferCount, nint inInputData)
        {
            AudioBuffer buffer;
            for (uint I = 0U; I < bufferCount; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(inInputData, I);
                if (buffer.mData == IntPtr.Zero) { continue; }
                OnDataAvailable(buffer.GetSpan());
            }
        }
    }
}