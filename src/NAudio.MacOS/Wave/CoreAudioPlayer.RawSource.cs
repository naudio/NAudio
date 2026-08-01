
using System;

using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private class RawSource : IPlayerSource
    {
        private readonly IWaveProvider provider;
        private readonly AudioStreamBasicDescription playerDescription;

        public RawSource(IWaveProvider provider, AudioStreamBasicDescription pd)
        {
            playerDescription = pd;
            this.provider = provider;
        }

        public AudioStreamBasicDescription ReinterpretedFormat => playerDescription;

        public int Read(Span<byte> HALbuffer) => provider.Read(HALbuffer);

        public void Dispose() { }
    }
}
