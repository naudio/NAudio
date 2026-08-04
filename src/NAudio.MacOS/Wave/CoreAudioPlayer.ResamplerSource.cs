
using System;

using NAudio.Utils;
using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private class ResamplerSource : IPlayerSource
    {
        private readonly uint streamsLatency;
        private readonly LowLevelAudioConverter converter;

        public unsafe ResamplerSource(IWaveProvider wp, AudioStreamBasicDescription requiredDesc, ChannelLayoutHandle channelLayoutOut, uint streamsLatency)
        {
            this.streamsLatency = streamsLatency;
            converter = new(new(wp.Read), MacUtils.ConstructASBDFromWaveFormat(wp.WaveFormat), requiredDesc);

            try
            {
                // If we have a channel mask in the source format, provide it to the converter.
                if (wp.WaveFormat is WaveFormatExtensible ext && ext.ChannelMask != 0)
                {
                    var l = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)ext.ChannelMask);

                    converter.AssignChannelLayout(
                        new(&l),
                        (uint)sizeof(AudioChannelLayout),
                        false
                    );
                }

                // If we have a target channel layout, provide it to the converter.
                if (channelLayoutOut is not null)
                {
                    converter.AssignChannelLayout(
                        channelLayoutOut.DangerousGetHandle(),
                        channelLayoutOut.Size,
                        true
                    );
                }

                // Special case: If the converter's source is single-channel
                // and the player requires multi-channel, copy the mono output to all the channels.
                if (converter.sourceFormat.mChannelsPerFrame == 1U && requiredDesc.mChannelsPerFrame > 1U)
                {
                    int[] chMap = new int[requiredDesc.mChannelsPerFrame];
                    Array.Fill(chMap, 0);
                    converter.SetChannelMap(chMap);
                }

                converter.InitializeNativeBuffer();
            }
            catch
            {
                converter.Dispose();
                throw;
            }
        }

        public uint StreamsLatency => streamsLatency;

        public int Read(Span<byte> HALbuffer) => converter.Read(HALbuffer);

        public AudioStreamBasicDescription ReinterpretedFormat => converter.outputFormat;

        public void Dispose() => converter.Dispose();
    }
}