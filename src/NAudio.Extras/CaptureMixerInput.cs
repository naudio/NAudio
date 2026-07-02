using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.Extras;

/// <summary>
/// Adapts a single capture source (a microphone, WASAPI loopback, etc.) into an
/// <see cref="ISampleProvider"/> in a common target format, ready to feed a
/// <see cref="MixingSampleProvider"/>. It buffers the incoming bytes, converts them to
/// 32-bit IEEE float, and matches the channel count and sample rate of the target format.
/// </summary>
/// <remarks>
/// The source is device-agnostic: feed captured bytes in via <see cref="AddSamples"/> from any
/// capture callback — a <c>WasapiRecorder</c>'s zero-copy <c>DataAvailable</c>
/// (<c>input.AddSamples(data)</c>) or a legacy <see cref="IWaveIn"/>
/// (<c>input.AddSamples(a.Buffer.AsSpan(0, a.BytesRecorded))</c>). Packets are appended in
/// arrival order; keeping independently-clocked sources in sync is handled downstream by
/// <see cref="RealtimeCaptureMixer"/>, which paces its output to the wall clock.
/// </remarks>
public class CaptureMixerInput
{
    private readonly BufferedWaveProvider buffer;
    private readonly int sourceBytesPerFrame;

    private long packetsReceived;
    private long framesReceived;

    /// <summary>
    /// The adapted provider, already in the target format, to add to a mixer.
    /// </summary>
    public ISampleProvider SampleProvider { get; }

    /// <summary>The native (capture) format of the source.</summary>
    public WaveFormat SourceFormat => buffer.WaveFormat;

    /// <summary>Number of packets handed to <see cref="AddSamples"/>. Diagnostics.</summary>
    public long PacketsReceived => packetsReceived;

    /// <summary>Number of source frames added so far. Diagnostics.</summary>
    public long FramesReceived => framesReceived;

    /// <summary>Source frames currently buffered and waiting to be mixed. Diagnostics.</summary>
    public int BufferedFrames => buffer.BufferedBytes / sourceBytesPerFrame;

    /// <summary>
    /// Creates a new capture input.
    /// </summary>
    /// <param name="sourceFormat">The native format the source delivers.</param>
    /// <param name="targetFormat">
    /// The common mixer format. Must be 32-bit IEEE float. The input is resampled and
    /// channel-converted to this format.
    /// </param>
    /// <param name="bufferDuration">How much audio the internal buffer holds (default 2s).</param>
    public CaptureMixerInput(WaveFormat sourceFormat, WaveFormat targetFormat, TimeSpan? bufferDuration = null)
    {
        if (targetFormat.Encoding != WaveFormatEncoding.IeeeFloat)
        {
            throw new ArgumentException("Target format must be 32-bit IEEE float", nameof(targetFormat));
        }
        buffer = new BufferedWaveProvider(sourceFormat, bufferDuration ?? TimeSpan.FromSeconds(2))
        {
            // The capture callback and the mixer read run on different threads, and a capture
            // callback may briefly outrun the mixer; drop the oldest audio rather than throw.
            DiscardOnBufferOverflow = true,
            // Hand back silence when empty so the mixer never starves waiting on this source.
            ReadFully = true,
        };
        sourceBytesPerFrame = sourceFormat.BlockAlign;

        // normalise bit depth -> float, then channels, then sample rate
        ISampleProvider provider = buffer.ToSampleProvider();
        provider = MatchChannels(provider, targetFormat.Channels);
        if (provider.WaveFormat.SampleRate != targetFormat.SampleRate)
        {
            provider = new WdlResamplingSampleProvider(provider, targetFormat.SampleRate);
        }
        SampleProvider = provider;
    }

    /// <summary>
    /// Adds captured bytes, appended in arrival order. Safe to call from the capture thread.
    /// </summary>
    public void AddSamples(ReadOnlySpan<byte> data)
    {
        packetsReceived++;
        buffer.AddSamples(data);
        framesReceived += data.Length / sourceBytesPerFrame;
    }

    private static ISampleProvider MatchChannels(ISampleProvider provider, int channels)
    {
        if (provider.WaveFormat.Channels == channels)
        {
            return provider;
        }
        if (provider.WaveFormat.Channels == 1 && channels == 2)
        {
            return new MonoToStereoSampleProvider(provider);
        }
        if (provider.WaveFormat.Channels == 2 && channels == 1)
        {
            return new StereoToMonoSampleProvider(provider);
        }
        throw new NotSupportedException(
            $"No channel conversion from {provider.WaveFormat.Channels} to {channels} channels");
    }
}
