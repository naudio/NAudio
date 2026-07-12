using System;

namespace NAudio.Dsp;

/// <summary>
/// Summary description for FastFourierTransform.
/// </summary>
public static class FastFourierTransform
{
    /// <summary>
    /// This computes an in-place complex-to-complex FFT
    /// x and y are the real and imaginary arrays of 2^m points.
    /// </summary>
    public static void FFT(bool forward, int m, Complex[] data)
        => FFT(forward, m, data.AsSpan());

    /// <summary>
    /// This computes an in-place complex-to-complex FFT over the first 2^m elements of the span.
    /// </summary>
    public static void FFT(bool forward, int m, Span<Complex> data)
    {
        int n, i, i1, j, k, i2, l, l1, l2;
        // The twiddle-factor recurrence (c1/c2 across stages, u1/u2 within a stage) is
        // kept in double precision. In single precision the rounding error compounded
        // across the O(N) within-stage recurrence and drifted the high-frequency bins
        // at large sizes (see issue #520). The butterfly arithmetic itself stays in
        // float: it only ever incurs one rounding step per stage, so it does not drift,
        // and keeping it in float avoids the float<->double conversions that would
        // otherwise slow the kernel several-fold. The double twiddle is narrowed to
        // float (fu1/fu2) once per column and reused across every butterfly in it.
        double c1, c2, u1, u2, z;
        float tx, ty, t1, t2, fu1, fu2;

        // Calculate the number of points
        n = 1 << m;

        // Do the bit reversal
        i2 = n >> 1;
        j = 0;
        for (i = 0; i < n - 1; i++)
        {
            if (i < j)
            {
                tx = data[i].X;
                ty = data[i].Y;
                data[i].X = data[j].X;
                data[i].Y = data[j].Y;
                data[j].X = tx;
                data[j].Y = ty;
            }
            k = i2;

            while (k <= j)
            {
                j -= k;
                k >>= 1;
            }
            j += k;
        }

        // Compute the FFT
        c1 = -1.0;
        c2 = 0.0;
        l2 = 1;
        for (l = 0; l < m; l++)
        {
            l1 = l2;
            l2 <<= 1;
            u1 = 1.0;
            u2 = 0.0;
            for (j = 0; j < l1; j++)
            {
                fu1 = (float)u1;
                fu2 = (float)u2;
                for (i = j; i < n; i += l2)
                {
                    i1 = i + l1;
                    t1 = fu1 * data[i1].X - fu2 * data[i1].Y;
                    t2 = fu1 * data[i1].Y + fu2 * data[i1].X;
                    data[i1].X = data[i].X - t1;
                    data[i1].Y = data[i].Y - t2;
                    data[i].X += t1;
                    data[i].Y += t2;
                }
                z = u1 * c1 - u2 * c2;
                u2 = u1 * c2 + u2 * c1;
                u1 = z;
            }
            c2 = Math.Sqrt((1.0 - c1) / 2.0);
            if (forward)
                c2 = -c2;
            c1 = Math.Sqrt((1.0 + c1) / 2.0);
        }

        // Scaling for forward transform
        if (forward)
        {
            float scale = 1.0f / n;
            for (i = 0; i < n; i++)
            {
                data[i].X *= scale;
                data[i].Y *= scale;
            }
        }
    }

    /// <summary>
    /// Applies a Hamming Window
    /// </summary>
    /// <param name="n">Index into frame</param>
    /// <param name="frameSize">Frame size (e.g. 1024)</param>
    /// <returns>Multiplier for Hamming window</returns>
    public static double HammingWindow(int n, int frameSize)
    {
        return 0.54 - 0.46 * Math.Cos((2 * Math.PI * n) / (frameSize - 1));
    }

    /// <summary>
    /// Applies a Hann Window
    /// </summary>
    /// <param name="n">Index into frame</param>
    /// <param name="frameSize">Frame size (e.g. 1024)</param>
    /// <returns>Multiplier for Hann window</returns>
    public static double HannWindow(int n, int frameSize)
    {
        return 0.5 * (1 - Math.Cos((2 * Math.PI * n) / (frameSize - 1)));
    }

    /// <summary>
    /// Applies a Blackman-Harris Window
    /// </summary>
    /// <param name="n">Index into frame</param>
    /// <param name="frameSize">Frame size (e.g. 1024)</param>
    /// <returns>Multiplier for Blackman-Harris window</returns>
    public static double BlackmanHarrisWindow(int n, int frameSize)
    {
        return 0.35875 - (0.48829 * Math.Cos((2 * Math.PI * n) / (frameSize - 1))) + (0.14128 * Math.Cos((4 * Math.PI * n) / (frameSize - 1))) - (0.01168 * Math.Cos((6 * Math.PI * n) / (frameSize - 1)));
    }
}
