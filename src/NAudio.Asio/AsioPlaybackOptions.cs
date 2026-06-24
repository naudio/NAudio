using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Options for configuring an <see cref="AsioDevice"/> in playback-only mode via <see cref="AsioDevice.InitPlayback"/>.
    /// </summary>
    public sealed class AsioPlaybackOptions
    {
        /// <summary>
        /// Audio source to play. Required. For an <see cref="ISampleProvider"/> source, use <see cref="From"/> or call
        /// <c>source.ToWaveProvider()</c> yourself.
        /// </summary>
        public IWaveProvider Source { get; init; }

        /// <summary>
        /// Physical output channel indices to route the source to. Source channel <c>n</c> plays out of
        /// <c>OutputChannels[n]</c>. Must have exactly as many entries as <c>Source.WaveFormat.Channels</c>.
        /// If <c>null</c>, defaults to the contiguous range <c>[0, Source.WaveFormat.Channels - 1]</c>.
        /// </summary>
        public int[] OutputChannels { get; init; }

        /// <summary>
        /// Requested ASIO buffer size in frames. If <c>null</c>, uses the driver's preferred buffer size.
        /// </summary>
        public int? BufferSize { get; init; }

        /// <summary>
        /// When <c>true</c>, the device stops and raises <see cref="AsioDevice.Stopped"/> once the source reports end-of-stream.
        /// The stop is dispatched off the ASIO callback thread so user handlers may safely dispose the device.
        /// </summary>
        public bool AutoStopOnEndOfStream { get; init; } = true;

        /// <summary>
        /// Convenience constructor that wraps an <see cref="ISampleProvider"/> via <c>ToWaveProvider()</c>.
        /// </summary>
        public static AsioPlaybackOptions From(ISampleProvider source, int[] outputChannels = null, int? bufferSize = null)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            return new AsioPlaybackOptions
            {
                Source = source.ToWaveProvider(),
                OutputChannels = outputChannels,
                BufferSize = bufferSize
            };
        }
    }
}
