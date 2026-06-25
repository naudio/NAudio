using System;

namespace NAudio.Dsp;

/// <summary>
/// Wraps two <see cref="BiQuadFilter"/> instances to retune a running filter without
/// the click an in-place coefficient change causes. <see cref="BiQuadFilter"/>
/// deliberately resets its state on every coefficient change (to recover from a
/// divergent NaN/Inf run), so retuning it in place is discontinuous. Instead,
/// reconfigure the <see cref="Standby"/> filter to the new settings, call
/// <see cref="BeginCrossfade"/>, and the output linearly crossfades from the old
/// response to the new one over a short window.
/// </summary>
public sealed class CrossfadingBiQuadFilter
{
    private readonly int crossfadeSamples;
    private BiQuadFilter active;
    private BiQuadFilter standby;
    private int crossfadePosition = -1;

    /// <summary>
    /// Creates a crossfading filter from an initially-audible
    /// <paramref name="active"/> filter and a <paramref name="standby"/> filter
    /// (a second instance used to stage the next set of coefficients). Both are
    /// created by the caller through the public <see cref="BiQuadFilter"/> factory
    /// methods.
    /// </summary>
    /// <param name="active">The filter that is initially audible.</param>
    /// <param name="standby">A second filter instance used to stage the next settings.</param>
    /// <param name="crossfadeSamples">Length of the retune crossfade in samples. Must be at least 1.</param>
    public CrossfadingBiQuadFilter(BiQuadFilter active, BiQuadFilter standby, int crossfadeSamples)
    {
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(standby);
        if (crossfadeSamples < 1)
            throw new ArgumentOutOfRangeException(nameof(crossfadeSamples), "Crossfade length must be at least 1 sample");
        this.active = active;
        this.standby = standby;
        this.crossfadeSamples = crossfadeSamples;
    }

    /// <summary>
    /// The filter that stages the next settings. Reconfigure this (via its
    /// <c>Set…</c> methods) before calling <see cref="BeginCrossfade"/>, or replace
    /// it wholesale with <see cref="ReplaceStandby"/> for filter shapes that have no
    /// in-place setter (shelves, notch, band-pass, all-pass).
    /// </summary>
    public BiQuadFilter Standby => standby;

    /// <summary>
    /// Swaps in a freshly-built filter as the next settings. Use this when retuning
    /// to a shape that has no in-place <c>Set…</c> method (build it with the
    /// <see cref="BiQuadFilter"/> factories), then call <see cref="BeginCrossfade"/>.
    /// Intended for parameter-change events, not the audio thread's steady state.
    /// </summary>
    public void ReplaceStandby(BiQuadFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        standby = filter;
    }

    /// <summary>
    /// True while a retune crossfade is in progress.
    /// </summary>
    public bool IsCrossfading => crossfadePosition >= 0;

    /// <summary>
    /// Starts crossfading from the currently-audible filter to the
    /// <see cref="Standby"/> filter. If a crossfade is already running it restarts
    /// from the beginning; the output stays continuous because both filters are run.
    /// </summary>
    public void BeginCrossfade() => crossfadePosition = 0;

    /// <summary>
    /// Processes one sample.
    /// </summary>
    public float Transform(float input)
    {
        if (crossfadePosition < 0)
            return active.Transform(input);

        // Run both filters so the incoming one is warm by the time it takes over.
        var a = active.Transform(input);
        var b = standby.Transform(input);
        var t = (float)crossfadePosition / crossfadeSamples;
        var output = a + (b - a) * t;

        if (++crossfadePosition >= crossfadeSamples)
        {
            (active, standby) = (standby, active);
            crossfadePosition = -1;
        }
        return output;
    }

    /// <summary>
    /// Processes a block of samples in place.
    /// </summary>
    public void Process(Span<float> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = Transform(buffer[i]);
    }

    /// <summary>
    /// Cancels any crossfade in progress and clears both wrapped filters'
    /// sample history, so the next input is filtered as if from silence.
    /// Coefficients are left unchanged.
    /// </summary>
    public void Reset()
    {
        crossfadePosition = -1;
        active.ResetState();
        standby.ResetState();
    }
}
