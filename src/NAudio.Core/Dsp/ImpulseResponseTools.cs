using System;

namespace NAudio.Dsp;

/// <summary>
/// Offline preparation of impulse responses for convolution: band-limited
/// resampling to the stream rate, trimming silent tails and level normalisation.
/// These run when an impulse response is loaded, never on the audio thread.
/// </summary>
internal static class ImpulseResponseTools
{
    // Windowed-sinc kernel: zero crossings each side of centre, Kaiser window.
    private const int ZeroCrossings = 32;
    private const double KaiserBeta = 9.0;     // about -90 dB sidelobes
    private const double CutoffMargin = 0.95;  // keeps the transition band below the new Nyquist

    /// <summary>
    /// Resamples an impulse response with a windowed-sinc interpolator. The kernel
    /// is centred on each output instant, so no delay is added, and the result is
    /// scaled by <c>fromRate / toRate</c> so the frequency response keeps its level.
    /// The output starts at time zero, so for an IR with energy in its very first
    /// samples the band-limited pre-ringing that would precede it is dropped; that
    /// keeps the IR causal (no latency) and changes the response by well under 0.2 dB.
    /// </summary>
    public static float[] Resample(ReadOnlySpan<float> ir, int fromRate, int toRate)
    {
        if (fromRate <= 0 || toRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(fromRate), "Sample rates must be positive.");
        if (fromRate == toRate)
            return ir.ToArray();

        var ratio = (double)toRate / fromRate;            // output samples per input sample
        var cutoff = Math.Min(1.0, ratio) * CutoffMargin; // relative to the input Nyquist
        var halfWidth = ZeroCrossings / cutoff;           // kernel half-width in input samples
        var outLength = (int)Math.Ceiling(ir.Length * ratio);
        var output = new float[outLength];
        var windowNorm = 1.0 / BesselI0(KaiserBeta);
        var gain = 1.0 / ratio;

        for (var n = 0; n < outLength; n++)
        {
            var t = n / ratio; // output instant in input samples
            var first = Math.Max(0, (int)Math.Ceiling(t - halfWidth));
            var last = Math.Min(ir.Length - 1, (int)Math.Floor(t + halfWidth));
            var sum = 0.0;
            for (var k = first; k <= last; k++)
            {
                var d = t - k;
                var x = d / halfWidth;
                var window = BesselI0(KaiserBeta * Math.Sqrt(Math.Max(0.0, 1.0 - x * x))) * windowNorm;
                sum += ir[k] * cutoff * Sinc(cutoff * d) * window;
            }
            output[n] = (float)(sum * gain);
        }
        return output;
    }

    /// <summary>
    /// Returns the length of <paramref name="ir"/> without its trailing samples that
    /// are below <paramref name="thresholdDb"/> relative to the peak.
    /// </summary>
    public static int TrimmedLength(ReadOnlySpan<float> ir, double thresholdDb)
    {
        var peak = 0f;
        foreach (var v in ir)
            peak = Math.Max(peak, Math.Abs(v));
        if (peak == 0f)
            return Math.Min(1, ir.Length);
        var threshold = peak * (float)Math.Pow(10.0, thresholdDb / 20.0);
        var end = ir.Length;
        while (end > 1 && Math.Abs(ir[end - 1]) < threshold)
            end--;
        return end;
    }

    /// <summary>
    /// Shortens <paramref name="ir"/> to <paramref name="length"/> samples, fading the
    /// last <paramref name="fadeLength"/> samples out with a raised cosine so the
    /// truncation does not add a click to the response.
    /// </summary>
    public static float[] Truncate(ReadOnlySpan<float> ir, int length, int fadeLength)
    {
        var result = ir[..Math.Min(length, ir.Length)].ToArray();
        if (result.Length < ir.Length)
        {
            fadeLength = Math.Min(fadeLength, result.Length);
            for (var i = 0; i < fadeLength; i++)
            {
                var g = 0.5 * (1.0 + Math.Cos(Math.PI * (i + 1) / (fadeLength + 1)));
                result[result.Length - fadeLength + i] *= (float)g;
            }
        }
        return result;
    }

    /// <summary>
    /// Scales a set of per-channel impulse responses by one common gain so their mean
    /// energy (sum of squares) is 1: white noise then passes at unchanged RMS level,
    /// and the balance between channels is kept.
    /// </summary>
    public static void NormalizeEnergy(float[][] irs)
    {
        var energy = 0.0;
        foreach (var ir in irs)
        {
            foreach (var v in ir)
                energy += (double)v * v;
        }
        energy /= irs.Length;
        if (energy <= 0.0)
            return;
        var gain = (float)(1.0 / Math.Sqrt(energy));
        foreach (var ir in irs)
        {
            for (var i = 0; i < ir.Length; i++)
                ir[i] *= gain;
        }
    }

    private static double Sinc(double x) => x == 0.0 ? 1.0 : Math.Sin(Math.PI * x) / (Math.PI * x);

    // Modified Bessel function of the first kind, order zero (power series).
    private static double BesselI0(double x)
    {
        var sum = 1.0;
        var term = 1.0;
        var halfX = x / 2.0;
        for (var k = 1; k < 50; k++)
        {
            term *= halfX / k;
            var t2 = term * term;
            sum += t2;
            if (t2 < sum * 1e-16)
                break;
        }
        return sum;
    }
}
