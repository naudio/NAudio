using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Bluestein (chirp-Z) wrapper that computes a complex DFT of arbitrary length on top of
    /// a small built-in double-precision radix-2 FFT. Built for sample-rate conversion
    /// algorithms (e.g. ARDFTSRC) where the transform length is dictated by the
    /// source/target rate ratio and is rarely a power of two.
    /// </summary>
    /// <remarks>
    /// Scaling matches NAudio's existing convention: <see cref="Forward"/> applies a
    /// <c>1/Length</c> scale, <see cref="Inverse"/> is unscaled, and the round-trip is the
    /// identity. Pre-allocates the chirp tables, the precomputed kernel spectrum, and a
    /// length-<c>L</c> scratch buffer where <c>L</c> is the smallest power of two satisfying
    /// <c>L &gt;= 2*Length - 1</c>; <see cref="Forward"/> and <see cref="Inverse"/> are then
    /// allocation-free.
    /// </remarks>
    /// <para>
    /// Why a private double FFT instead of the public <see cref="FastFourierTransform"/>: at
    /// the SRC block sizes we care about (e.g. <c>L = 32768</c> for a 96 -&gt; 44.1 inFftSize
    /// of 8960), float32 chirp arithmetic plus float32 FFT loses enough precision that bins
    /// that should be zero acquire ~1% noise. That manifests as an alias floor of only
    /// ~ -10 dB instead of the expected &lt; -120 dB. Computing the chirps and FFT in double
    /// keeps the noise floor below the float32-output rounding floor of the API.
    /// </para>
    internal sealed class BluesteinDft
    {
        private readonly int n;
        private readonly int l;
        private readonly int lLog2;
        private readonly double[] chirpRe;
        private readonly double[] chirpIm;
        private readonly double[] kernelRe;        // length L; FFT(B) in standard convention.
        private readonly double[] kernelIm;
        private readonly double[] scratchRe;       // length L.
        private readonly double[] scratchIm;
        private readonly double[] twRe;            // twiddle table of length L/2.
        private readonly double[] twIm;
        private readonly int[] bitRev;             // bit-reversal permutation of length L.

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

            chirpRe = new double[n];
            chirpIm = new double[n];
            long mod = 2L * n;
            for (int k = 0; k < n; k++)
            {
                long kk = (long)k * k % mod;
                double angle = -Math.PI * kk / n;
                chirpRe[k] = Math.Cos(angle);
                chirpIm[k] = Math.Sin(angle);
            }

            // Precompute twiddle factors and bit-reversal indices for the length-L FFT.
            twRe = new double[l / 2];
            twIm = new double[l / 2];
            for (int k = 0; k < l / 2; k++)
            {
                double angle = -2.0 * Math.PI * k / l;
                twRe[k] = Math.Cos(angle);
                twIm[k] = Math.Sin(angle);
            }
            bitRev = new int[l];
            for (int i = 0; i < l; i++)
            {
                int rev = 0;
                int bits = lLog2;
                int v = i;
                while (bits-- > 0) { rev = (rev << 1) | (v & 1); v >>= 1; }
                bitRev[i] = rev;
            }

            // Kernel B in the time domain: B[i] = conj(chirp[|i|]), embedded into length L
            // with negative indices wrapping (B is even, so B[L-i] = B[i]).
            kernelRe = new double[l];
            kernelIm = new double[l];
            kernelRe[0] = chirpRe[0];
            kernelIm[0] = -chirpIm[0];
            for (int i = 1; i < n; i++)
            {
                double bx = chirpRe[i];
                double by = -chirpIm[i];
                kernelRe[i] = bx; kernelIm[i] = by;
                kernelRe[l - i] = bx; kernelIm[l - i] = by;
            }
            FftInPlace(kernelRe, kernelIm, forward: true);

            scratchRe = new double[l];
            scratchIm = new double[l];
        }

        /// <summary>
        /// Length of the DFT. Input and output spans must have exactly this many elements.
        /// </summary>
        public int Length => n;

        /// <summary>
        /// In-place forward DFT, scaled by <c>1/Length</c> to match
        /// <see cref="FastFourierTransform.FFT(bool, int, Span{Complex})"/> convention.
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
            var aRe = scratchRe;
            var aIm = scratchIm;
            Array.Clear(aRe, 0, l);
            Array.Clear(aIm, 0, l);

            // a[i] = data[i] * chirp[i] for i in 0..N-1.
            for (int i = 0; i < n; i++)
            {
                double dx = data[i].X, dy = data[i].Y;
                double cx = chirpRe[i], cy = chirpIm[i];
                aRe[i] = dx * cx - dy * cy;
                aIm[i] = dx * cy + dy * cx;
            }

            FftInPlace(aRe, aIm, forward: true);

            // Pointwise multiply by the precomputed kernel FFT (standard convention).
            for (int i = 0; i < l; i++)
            {
                double ax = aRe[i], ay = aIm[i];
                double kx = kernelRe[i], ky = kernelIm[i];
                aRe[i] = ax * kx - ay * ky;
                aIm[i] = ax * ky + ay * kx;
            }

            FftInPlace(aRe, aIm, forward: false);

            // a[0..N-1] now holds the linear convolution (the new FFT applies 1/L on inverse,
            // matching standard convention, so the convolution comes out un-normalised). The
            // remaining 1/N is NAudio's forward-DFT convention applied while we post-multiply
            // by the chirp.
            double scale = 1.0 / n;
            for (int i = 0; i < n; i++)
            {
                double ax = aRe[i], ay = aIm[i];
                double cx = chirpRe[i], cy = chirpIm[i];
                data[i].X = (float)((ax * cx - ay * cy) * scale);
                data[i].Y = (float)((ax * cy + ay * cx) * scale);
            }
        }

        // Iterative radix-2 Cooley-Tukey, in place, double precision. Forward is unscaled;
        // inverse applies 1/L. Uses precomputed twiddle table and bit-reversal permutation.
        private void FftInPlace(double[] re, double[] im, bool forward)
        {
            // Bit-reverse permutation.
            for (int i = 0; i < l; i++)
            {
                int j = bitRev[i];
                if (i < j)
                {
                    double tr = re[i]; re[i] = re[j]; re[j] = tr;
                    double ti = im[i]; im[i] = im[j]; im[j] = ti;
                }
            }

            // Cooley-Tukey butterflies. For inverse, conjugate the twiddle on the fly.
            for (int size = 2; size <= l; size <<= 1)
            {
                int half = size >> 1;
                int step = l / size;
                for (int i = 0; i < l; i += size)
                {
                    int twi = 0;
                    for (int j = i; j < i + half; j++)
                    {
                        double wr = twRe[twi];
                        double wi = forward ? twIm[twi] : -twIm[twi];
                        double tre = wr * re[j + half] - wi * im[j + half];
                        double tim = wr * im[j + half] + wi * re[j + half];
                        re[j + half] = re[j] - tre;
                        im[j + half] = im[j] - tim;
                        re[j] += tre;
                        im[j] += tim;
                        twi += step;
                    }
                }
            }

            if (!forward)
            {
                double inv = 1.0 / l;
                for (int i = 0; i < l; i++) { re[i] *= inv; im[i] *= inv; }
            }
        }
    }
}
