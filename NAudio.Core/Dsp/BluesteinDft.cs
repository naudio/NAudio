using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Bluestein (chirp-Z) wrapper that computes a complex DFT of arbitrary length on top of
    /// the existing radix-2 <see cref="FastFourierTransform"/>. Built for sample-rate
    /// conversion algorithms (e.g. ARDFTSRC) where the transform length is dictated by the
    /// source/target rate ratio and is rarely a power of two.
    /// </summary>
    /// <remarks>
    /// Scaling matches NAudio's existing convention: <see cref="Forward"/> applies a
    /// <c>1/Length</c> scale, <see cref="Inverse"/> is unscaled, and the round-trip is the
    /// identity. Pre-allocates the chirp tables and a length-L scratch buffer where
    /// <c>L</c> is the smallest power of two satisfying <c>L &gt;= 2*Length - 1</c>;
    /// <see cref="Forward"/> and <see cref="Inverse"/> are then allocation-free.
    /// </remarks>
    internal sealed class BluesteinDft
    {
        private readonly int n;
        private readonly int l;
        private readonly int lLog2;
        private readonly Complex[] chirp;
        private readonly Complex[] kernelFft;
        private readonly Complex[] scratch;

        public BluesteinDft(int length)
        {
            if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));
            n = length;

            int twoNMinus1 = 2 * n - 1;
            int len = 1;
            int log2 = 0;
            while (len < twoNMinus1) { len <<= 1; log2++; }
            l = len;
            lLog2 = log2;

            // chirp[k] = exp(-i pi k^2 / N).  Reduce k^2 mod 2N before the trig call so we keep
            // angle precision for large k (k can run into the thousands for SRC block sizes).
            chirp = new Complex[n];
            long mod = 2L * n;
            for (int k = 0; k < n; k++)
            {
                long kk = (long)k * k % mod;
                double angle = -Math.PI * kk / n;
                chirp[k].X = (float)Math.Cos(angle);
                chirp[k].Y = (float)Math.Sin(angle);
            }

            // Kernel B in the time domain: B[i] = conj(chirp[|i|]), embedded into length L
            // with negative indices wrapping (B is even, so B[L-i] = B[i]).
            var b = new Complex[l];
            b[0].X = chirp[0].X;
            b[0].Y = -chirp[0].Y;
            for (int i = 1; i < n; i++)
            {
                float bx = chirp[i].X;
                float by = -chirp[i].Y;
                b[i].X = bx; b[i].Y = by;
                b[l - i].X = bx; b[l - i].Y = by;
            }

            // We want F_std(B); NAudio's forward FFT produces F_std(B) / L. We bake the L
            // factor into the kernel here so the Forward() hot path can pointwise-multiply
            // and then call NAudio's unscaled inverse without further bookkeeping.
            FastFourierTransform.FFT(true, lLog2, b);
            for (int i = 0; i < l; i++)
            {
                b[i].X *= l;
                b[i].Y *= l;
            }
            kernelFft = b;

            scratch = new Complex[l];
        }

        /// <summary>
        /// Length of the DFT. Input and output spans must have exactly this many elements.
        /// </summary>
        public int Length => n;

        /// <summary>
        /// In-place forward DFT, scaled by <c>1/Length</c> to match
        /// <see cref="FastFourierTransform.FFT"/> convention.
        /// </summary>
        public void Forward(Span<Complex> data)
        {
            if (data.Length != n)
                throw new ArgumentException($"data length must equal {nameof(Length)} ({n})", nameof(data));
            ForwardCore(data);
        }

        /// <summary>
        /// In-place inverse DFT, unscaled (round-trips with <see cref="Forward"/>).
        /// </summary>
        public void Inverse(Span<Complex> data)
        {
            if (data.Length != n)
                throw new ArgumentException($"data length must equal {nameof(Length)} ({n})", nameof(data));

            // Inverse via conjugation: ifft_naudio(X) = N * conj( fft_naudio( conj(X) ) ).
            // The two scale factors (NAudio's 1/N inside Forward and the explicit *N here)
            // cancel, leaving the unscaled inverse the caller expects.
            for (int i = 0; i < n; i++) data[i].Y = -data[i].Y;
            ForwardCore(data);
            for (int i = 0; i < n; i++)
            {
                data[i].X *= n;
                data[i].Y = -data[i].Y * n;
            }
        }

        private void ForwardCore(Span<Complex> data)
        {
            var a = scratch;
            Array.Clear(a, 0, l);

            // a[i] = data[i] * chirp[i] for i in 0..N-1.
            for (int i = 0; i < n; i++)
            {
                float dx = data[i].X, dy = data[i].Y;
                float cx = chirp[i].X, cy = chirp[i].Y;
                a[i].X = dx * cx - dy * cy;
                a[i].Y = dx * cy + dy * cx;
            }

            FastFourierTransform.FFT(true, lLog2, a);

            // Pointwise multiply by the L-scaled kernel FFT.
            var k = kernelFft;
            for (int i = 0; i < l; i++)
            {
                float ax = a[i].X, ay = a[i].Y;
                float kx = k[i].X, ky = k[i].Y;
                a[i].X = ax * kx - ay * ky;
                a[i].Y = ax * ky + ay * kx;
            }

            FastFourierTransform.FFT(false, lLog2, a);

            // a[0..N-1] now holds the linear convolution (the L baked into the kernel cancels
            // the 1/L from NAudio's forward FFT). The remaining 1/N is NAudio's forward-DFT
            // scaling convention applied as we post-multiply by the chirp.
            float scale = 1.0f / n;
            for (int i = 0; i < n; i++)
            {
                float ax = a[i].X, ay = a[i].Y;
                float cx = chirp[i].X, cy = chirp[i].Y;
                data[i].X = (ax * cx - ay * cy) * scale;
                data[i].Y = (ax * cy + ay * cx) * scale;
            }
        }
    }
}
