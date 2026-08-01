
using System;

using NAudio.Utils;
using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private class ResamplerSource : IPlayerSource
    {
        private readonly LowLevelAudioConverter converter;

        public unsafe ResamplerSource(IWaveProvider wp, AudioStreamBasicDescription requiredDesc, ChannelLayoutHandle channelLayoutOut)
        {
            converter = new(new(wp.Read), MacUtils.ConstructASBDFromWaveFormat(wp.WaveFormat), requiredDesc);

            if (wp.WaveFormat is WaveFormatExtensible ext && ext.ChannelMask != 0)
            {
                var l = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)ext.ChannelMask);

                converter.AssignChannelLayout(
                    new(&l),
                    (uint)sizeof(AudioChannelLayout),
                    false
                );
            }

            if (channelLayoutOut is not null)
            {
                converter.AssignChannelLayout(
                    channelLayoutOut.DangerousGetHandle(),
                    channelLayoutOut.Size,
                    true
                );
            }

            converter.InitializeNativeBuffer();
        }

        public AudioStreamBasicDescription ReinterpretedFormat => converter.outputFormat;

        public int Read(Span<byte> HALbuffer) => converter.Read(HALbuffer);

        public void Dispose() => converter.Dispose();
    }
}