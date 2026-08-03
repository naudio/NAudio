
using System;

using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private class RawSource : IPlayerSource
    {
        private readonly uint streamsLatency;
        private readonly IWaveProvider provider;
        private readonly AudioStreamBasicDescription playerDescription;

        public RawSource(IWaveProvider provider, AudioStreamBasicDescription pd, uint streamsLatency)
        {
            playerDescription = pd;
            this.provider = provider;
            this.streamsLatency = streamsLatency;
        }

        public uint StreamsLatency => streamsLatency;

        public AudioStreamBasicDescription ReinterpretedFormat => playerDescription;

        public int Read(Span<byte> halBuffer) => provider.Read(halBuffer);

        public void Dispose() { }
    }
}
