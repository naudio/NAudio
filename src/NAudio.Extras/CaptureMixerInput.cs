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
/// <para>
/// The source is device-agnostic: feed it from any capture callback via one of the
/// <c>AddSamples</c> overloads. For a <c>WasapiRecorder</c> the zero-copy
/// <c>DataAvailable</c> callback provides a <see cref="ReadOnlySpan{T}"/> plus the packet's
/// device position and QPC timestamp; for a legacy <see cref="IWaveIn"/> device use the
/// plain <see cref="AddSamples(ReadOnlySpan{byte})"/> overload.
/// </para>
/// <para>
/// When the timestamped overload is used, the input keeps itself aligned on a shared
/// <see cref="CaptureTimeline"/>: the first packet is offset by its QPC distance from the
/// shared origin (so a device that starts later, or a loopback that only begins delivering
/// once audio plays, lines up in time), and mid-stream <c>devicePosition</c> gaps caused by
/// glitches are back-filled with silence (so a dropout does not permanently shift the source
/// earlier relative to the others). This is best-effort alignment intended to stop
/// independently-clocked sources drifting apart over a recording; it is not a sample-accurate
/// resampling clock. Slow/fast device-clock differences are absorbed by pacing the mixer
/// output to the wall clock — see <see cref="RealtimeCaptureMixer"/>.
/// </para>
/// </remarks>
public class CaptureMixerInput
{
    private const long HundredNanosecondsPerSecond = 10_000_000L;

    private readonly BufferedWaveProvider buffer;
    private readonly CaptureTimeline timeline;
    private readonly int sourceBytesPerFrame;
    private readonly int sourceSampleRate;
    private readonly int maxAlignmentFrames;

    private bool firstPacket = true;
    private bool haveDeviceEnd;
    private long lastDeviceEnd;
    private long timelineFrames;
    private long packetsReceived;
    private long framesReceived;
    private long silenceFramesInserted;
    private long lastDevicePosition;
    private long lastQpcPosition;
    private readonly byte[] silence = new byte[16 * 1024]; // reusable all-zero chunk

    /// <summary>
    /// The adapted provider, already in the target format, to add to a mixer.
    /// </summary>
    public ISampleProvider SampleProvider { get; }

    /// <summary>The native (capture) format of the source.</summary>
    public WaveFormat SourceFormat => buffer.WaveFormat;

    /// <summary>
    /// Total number of source frames placed on the timeline so far, including any silence
    /// inserted for alignment. Exposed for diagnostics.
    /// </summary>
    public long TimelineFrames => timelineFrames;

    /// <summary>Number of packets handed to <c>AddSamples</c>. Diagnostics.</summary>
    public long PacketsReceived => packetsReceived;

    /// <summary>Number of real (non-silence) source frames added. Diagnostics.</summary>
    public long FramesReceived => framesReceived;

    /// <summary>Number of silence frames inserted for alignment. Diagnostics.</summary>
    public long SilenceFramesInserted => silenceFramesInserted;

    /// <summary>Source frames currently buffered and waiting to be mixed. Diagnostics.</summary>
    public int BufferedFrames => buffer.BufferedBytes / sourceBytesPerFrame;

    /// <summary>The most recent packet's device position, as reported by the source. Diagnostics.</summary>
    public long LastDevicePosition => lastDevicePosition;

    /// <summary>The most recent packet's QPC timestamp (100ns units). Diagnostics.</summary>
    public long LastQpcPosition => lastQpcPosition;

    /// <summary>
    /// Creates a new capture input.
    /// </summary>
    /// <param name="sourceFormat">The native format the source delivers.</param>
    /// <param name="targetFormat">
    /// The common mixer format. Must be 32-bit IEEE float. The input is resampled and
    /// channel-converted to this format.
    /// </param>
    /// <param name="timeline">
    /// Optional shared timeline for cross-source alignment. Pass the same instance to every
    /// input you want mutually aligned. If null, the input still aligns to its own first
    /// packet but cannot align to other sources.
    /// </param>
    /// <param name="bufferDuration">How much audio the internal buffer holds (default 2s).</param>
    public CaptureMixerInput(WaveFormat sourceFormat, WaveFormat targetFormat,
        CaptureTimeline timeline = null, TimeSpan? bufferDuration = null)
    {
        if (targetFormat.Encoding != WaveFormatEncoding.IeeeFloat)
        {
            throw new ArgumentException("Target format must be 32-bit IEEE float", nameof(targetFormat));
        }
        this.timeline = timeline;
        buffer = new BufferedWaveProvider(sourceFormat, bufferDuration ?? TimeSpan.FromSeconds(2))
        {
            // The capture callback and the mixer read run on different threads, and a capture
            // callback may briefly outrun the mixer; drop the oldest audio rather than throw.
            DiscardOnBufferOverflow = true,
            // Hand back silence when empty so the mixer never starves waiting on this source.
            ReadFully = true,
        };
        sourceBytesPerFrame = sourceFormat.BlockAlign;
        sourceSampleRate = sourceFormat.SampleRate;
        // Cap any single alignment correction (start lead or gap fill) to one second. This
        // bounds the effect of an implausible timestamp so a misbehaving driver can never flood
        // the stream with silence.
        maxAlignmentFrames = sourceSampleRate;

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
    /// Adds captured bytes with no timeline alignment (appended in arrival order). Use this
    /// for sources that do not provide timestamps, e.g. a legacy <see cref="IWaveIn"/> device.
    /// </summary>
    public void AddSamples(ReadOnlySpan<byte> data)
    {
        var frames = data.Length / sourceBytesPerFrame;
        packetsReceived++;
        buffer.AddSamples(data);
        framesReceived += frames;
        timelineFrames += frames;
    }

    /// <summary>
    /// Adds captured bytes tagged with the packet's QPC capture time and device position, both
    /// as delivered by <c>WasapiRecorder.DataAvailable</c>. The QPC timestamp aligns the start
    /// of the source against the shared <see cref="CaptureTimeline"/>; the device position is
    /// used to detect and back-fill mid-stream glitches so the source stays aligned.
    /// </summary>
    /// <param name="data">The captured audio for this packet.</param>
    /// <param name="qpcPosition">Packet capture time (QPC value, 100-nanosecond units).</param>
    /// <param name="devicePosition">Device frame position of the first frame in this packet.</param>
    public void AddSamples(ReadOnlySpan<byte> data, long qpcPosition, long devicePosition)
    {
        var frames = data.Length / sourceBytesPerFrame;
        packetsReceived++;
        lastQpcPosition = qpcPosition;
        lastDevicePosition = devicePosition;

        if (firstPacket)
        {
            firstPacket = false;
            var origin = timeline?.GetOrSetOrigin(qpcPosition) ?? qpcPosition;
            // Offset the first real audio so it sits at its true capture time relative to the
            // shared origin. A source that started earliest (origin) gets no lead; later
            // starters get a silence lead equal to their QPC distance from the origin. Only a
            // sane, bounded lead is applied — see the note on alignment below.
            var lead = (qpcPosition - origin) * sourceSampleRate / HundredNanosecondsPerSecond;
            if (lead > 0 && lead <= maxAlignmentFrames)
            {
                InsertSilence(lead);
            }
        }
        else if (haveDeviceEnd)
        {
            // A continuous stream reports devicePosition == end of the previous packet. A jump
            // ahead means the device counted frames it never delivered (a glitch): fill the hole
            // with silence so downstream audio keeps its timing. We deliberately only correct a
            // small, plausible *forward* gap. A backwards or overlapping position, an
            // implausibly large jump, or a driver that reports a static/zero device position is
            // ignored and the packet is simply appended — captured audio is never dropped.
            var gap = devicePosition - lastDeviceEnd;
            if (gap > 0 && gap <= maxAlignmentFrames)
            {
                InsertSilence(gap);
            }
        }

        lastDeviceEnd = devicePosition + frames;
        haveDeviceEnd = true;
        buffer.AddSamples(data);
        framesReceived += frames;
        timelineFrames += frames;
    }

    private void InsertSilence(long frames)
    {
        var remaining = (int)frames * sourceBytesPerFrame;
        while (remaining > 0)
        {
            var chunk = Math.Min(remaining, silence.Length);
            buffer.AddSamples(silence, 0, chunk);
            remaining -= chunk;
        }
        silenceFramesInserted += frames;
        timelineFrames += frames;
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
