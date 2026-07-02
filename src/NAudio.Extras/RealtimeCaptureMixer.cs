using System;
using System.Collections.Generic;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.Extras;

/// <summary>
/// Live-mixes several capture sources (microphones, WASAPI loopback, etc.) that may each have
/// a different sample rate and channel count into a single stream in a common target format.
/// Each source is wrapped in a <see cref="CaptureMixerInput"/>, and the mixed output is paced to
/// the wall clock (anchored to the first captured sample) so its length matches real elapsed
/// time. That pacing is what keeps independently-clocked sources in sync: a source running
/// slightly slow is padded and one running slightly fast is drained, and a loopback source that
/// stops delivering while nothing is playing simply contributes silence until it resumes.
/// </summary>
/// <remarks>
/// <para>Typical use with <c>WasapiRecorder</c>:</para>
/// <code>
/// var mixer = new RealtimeCaptureMixer(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
/// var micInput = mixer.AddInput(micRecorder.WaveFormat);
/// micRecorder.DataAvailable += (data, flags, dev, qpc) => micInput.AddSamples(data);
/// // ...repeat for a loopback recorder...
/// mixer.Start();
/// micRecorder.StartRecording();
/// // pump on a background thread:
/// var buffer = new float[mixer.WaveFormat.SampleRate * mixer.WaveFormat.Channels];
/// while (recording) {
///     int n = mixer.Read(buffer, 0, buffer.Length);
///     if (n > 0) writer.WriteSamples(buffer, 0, n); else Thread.Sleep(5);
/// }
/// </code>
/// <para>
/// <see cref="Read"/> hands back only as many samples as the wall clock says should exist by
/// now, so a free-running pump loop stays real-time instead of racing ahead producing
/// silence — do not read the underlying <see cref="Output"/> mixer directly in a tight loop.
/// </para>
/// </remarks>
public class RealtimeCaptureMixer
{
    private readonly MixingSampleProvider mixer;
    private readonly List<CaptureMixerInput> inputs = new();
    private readonly Stopwatch clock = new();
    private readonly double preRollMs;
    private long framesRead;
    private bool started;

    /// <summary>The common target format everything is mixed into (32-bit IEEE float).</summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// The underlying mixer. Exposed for advanced composition; for real-time capture read via
    /// <see cref="Read"/> instead so the output stays paced to the wall clock.
    /// </summary>
    public ISampleProvider Output => mixer;

    /// <summary>The inputs added so far.</summary>
    public IReadOnlyList<CaptureMixerInput> Inputs => inputs;

    /// <summary>Total frames read from the mixer (paced output) since <see cref="Start"/>. Diagnostics.</summary>
    public long OutputFrames => framesRead;

    /// <summary>
    /// Creates a new mixer.
    /// </summary>
    /// <param name="targetFormat">The mixed output format. Must be 32-bit IEEE float.</param>
    /// <param name="preRoll">
    /// A small cushion of audio to build up (measured from the first captured sample, not from
    /// <see cref="Start"/>) before output begins, so the mixer doesn't immediately underrun.
    /// Default 50ms.
    /// </param>
    public RealtimeCaptureMixer(WaveFormat targetFormat, TimeSpan? preRoll = null)
    {
        if (targetFormat.Encoding != WaveFormatEncoding.IeeeFloat)
        {
            throw new ArgumentException("Target format must be 32-bit IEEE float", nameof(targetFormat));
        }
        WaveFormat = targetFormat;
        mixer = new MixingSampleProvider(targetFormat) { ReadFully = true };
        preRollMs = (preRoll ?? TimeSpan.FromMilliseconds(50)).TotalMilliseconds;
    }

    /// <summary>
    /// Adds a capture source in its native format and returns the input to feed captured bytes
    /// into. Optionally wrap the input's provider (e.g. with a <c>MeteringSampleProvider</c> for
    /// a level meter) before it reaches the mixer via <paramref name="tap"/>.
    /// </summary>
    /// <param name="sourceFormat">The native capture format of the source.</param>
    /// <param name="tap">
    /// Optional transform applied to the input's provider before it is added to the mixer. The
    /// mixer reads through whatever this returns, so metering/monitoring inserted here sees the
    /// audio as it is consumed.
    /// </param>
    /// <param name="bufferDuration">Internal buffer size for this input (default 2s).</param>
    public CaptureMixerInput AddInput(WaveFormat sourceFormat,
        Func<ISampleProvider, ISampleProvider> tap = null, TimeSpan? bufferDuration = null)
    {
        var input = new CaptureMixerInput(sourceFormat, WaveFormat, bufferDuration);
        inputs.Add(input);
        mixer.AddMixerInput(tap == null ? input.SampleProvider : tap(input.SampleProvider));
        return input;
    }

    /// <summary>Arms (or re-arms) the mixer for a new capture. Call just before starting the recorders.</summary>
    public void Start()
    {
        framesRead = 0;
        started = false;
    }

    /// <summary>
    /// Reads mixed audio, paced to the wall clock: returns at most as many samples as should
    /// have been produced by now, and 0 before the first audio arrives, during the pre-roll
    /// window, or when the caller has already caught up. Write whatever it returns and sleep
    /// briefly on 0.
    /// </summary>
    /// <returns>The number of samples (floats, i.e. frames × channels) written.</returns>
    public int Read(float[] buffer, int offset, int maxSamples)
    {
        if (!started)
        {
            // Anchor the output clock to the first captured audio rather than to Start(), so the
            // file begins at the first real sample with only a small cushion of latency — no long
            // leading gap while a device spins up, and no initial audio skipped.
            var hasData = false;
            foreach (var input in inputs)
            {
                if (input.FramesReceived > 0)
                {
                    hasData = true;
                    break;
                }
            }
            if (!hasData)
            {
                return 0;
            }
            clock.Restart();
            started = true;
        }

        var elapsedMs = clock.Elapsed.TotalMilliseconds;
        if (elapsedMs < preRollMs)
        {
            return 0;
        }
        var channels = WaveFormat.Channels;
        var targetFrames = (long)((elapsedMs - preRollMs) / 1000.0 * WaveFormat.SampleRate);
        var wantFrames = targetFrames - framesRead;
        if (wantFrames <= 0)
        {
            return 0;
        }
        var wantSamples = (int)Math.Min(wantFrames * channels, maxSamples);
        wantSamples -= wantSamples % channels; // stay on a frame boundary
        if (wantSamples <= 0)
        {
            return 0;
        }
        var read = mixer.Read(buffer.AsSpan(offset, wantSamples));
        framesRead += read / channels;
        return read;
    }
}
