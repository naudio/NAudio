using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Window function to apply to time-domain samples before a forward real FFT.
    /// </summary>
    public enum FftWindowType
    {
        /// <summary>No window (rectangular).</summary>
        None,
        /// <summary>Hann window — good general-purpose choice for spectrum analysis.</summary>
        Hann,
        /// <summary>Hamming window — classic analysis window with slightly better side-lobe suppression than Hann.</summary>
        Hamming,
        /// <summary>Blackman-Harris 4-term window — best side-lobe rejection but wider main lobe.</summary>
        BlackmanHarris
    }

    /// <summary>
    /// A reusable FFT processor specialised for fixed-size real-input audio work. Compared to
    /// calling <see cref="FastFourierTransform.FFT(bool, int, Complex[])"/> directly, this class:
    /// <list type="bullet">
    ///   <item>Uses an N/2-point complex FFT plus an unpack pass to compute an N-point real FFT,
    ///     returning an <c>N/2 + 1</c>-bin half-spectrum. Roughly half the work of the full
    ///     complex FFT for the same N.</item>
    ///   <item>Precomputes the window coefficients (if configured) so the time-domain window is
    ///     applied with one multiply per sample instead of a fresh <c>cos</c> evaluation.</item>
    ///   <item>Exposes <see cref="Span{T}"/> APIs so callers can pass stack-allocated or
    ///     pooled buffers.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Scaling matches <see cref="FastFourierTransform.FFT(bool, int, Complex[])"/>:
    /// <see cref="RealForward"/> applies a <c>1/N</c> scale (so a DC signal of magnitude 1
    /// yields <c>spectrum[0].Real = 1</c>). <see cref="RealInverse"/> is un-scaled, so
    /// <c>RealInverse(RealForward(x))</c> recovers <c>x</c>.
    /// </remarks>
    public sealed class FftProcessor
    {
        private readonly int fftSize;
        private readonly int halfSize;
        private readonly int mHalf;                 // log2(halfSize) — the m parameter for the inner N/2-point FFT
        private readonly float[] windowTable;       // null if FftWindowType.None
        private readonly float[] realTwiddleCos;    // cos(-2πk/N) for k = 0..halfSize
        private readonly float[] realTwiddleSin;    // sin(-2πk/N) for k = 0..halfSize
        private readonly Complex[] halfSpectrum;    // reusable scratch for the N/2-point complex FFT

        /// <summary>
        /// Creates an FFT processor for a fixed power-of-two size, optionally baking in a
        /// time-domain window.
        /// </summary>
        /// <param name="fftSize">FFT size in samples. Must be a power of two and at least 2.</param>
        /// <param name="window">Window function applied to input samples by <see cref="RealForward"/>.
        /// Use <see cref="FftWindowType.None"/> to skip windowing.</param>
        public FftProcessor(int fftSize, FftWindowType window = FftWindowType.None)
        {
            if (fftSize < 2 || (fftSize & (fftSize - 1)) != 0)
                throw new ArgumentException("FftSize must be a power of two and at least 2.", nameof(fftSize));

            this.fftSize = fftSize;
            halfSize = fftSize / 2;
            mHalf = Log2(halfSize);

            if (window != FftWindowType.None)
            {
                windowTable = new float[fftSize];
                for (int i = 0; i < fftSize; i++)
                {
                    windowTable[i] = window switch
                    {
                        FftWindowType.Hann => (float)FastFourierTransform.HannWindow(i, fftSize),
                        FftWindowType.Hamming => (float)FastFourierTransform.HammingWindow(i, fftSize),
                        FftWindowType.BlackmanHarris => (float)FastFourierTransform.BlackmanHarrisWindow(i, fftSize),
                        _ => 1f
                    };
                }
            }

            realTwiddleCos = new float[halfSize + 1];
            realTwiddleSin = new float[halfSize + 1];
            for (int k = 0; k <= halfSize; k++)
            {
                double angle = -2.0 * Math.PI * k / fftSize;
                realTwiddleCos[k] = (float)Math.Cos(angle);
                realTwiddleSin[k] = (float)Math.Sin(angle);
            }

            halfSpectrum = new Complex[halfSize];
        }

        /// <summary>FFT size in samples (time-domain length).</summary>
        public int FftSize => fftSize;

        /// <summary>Length of the complex half-spectrum produced by <see cref="RealForward"/>: <c>FftSize/2 + 1</c>.</summary>
        public int SpectrumLength => halfSize + 1;

        /// <summary>
        /// Forward real FFT. Applies the configured window (if any), then computes the complex
        /// spectrum for positive frequencies <c>k = 0 .. N/2</c>. Negative frequencies are the
        /// complex conjugate of the positive ones and are not emitted.
        /// </summary>
        /// <param name="samples">Time-domain samples. Must be <see cref="FftSize"/> long.</param>
        /// <param name="spectrum">Output complex half-spectrum. Must be <see cref="SpectrumLength"/> long.
        /// <c>spectrum[0].Imaginary</c> and <c>spectrum[FftSize/2].Imaginary</c> are both zero by construction.</param>
        public void RealForward(ReadOnlySpan<float> samples, Span<Complex> spectrum)
        {
            if (samples.Length != fftSize)
                throw new ArgumentException($"samples must be length {fftSize}", nameof(samples));
            if (spectrum.Length != halfSize + 1)
                throw new ArgumentException($"spectrum must be length {halfSize + 1}", nameof(spectrum));

            // Pack even-indexed samples into the real part and odd-indexed samples into the imaginary
            // part of an N/2-point complex sequence, optionally applying the window at the same time.
            if (windowTable != null)
            {
                var win = windowTable;
                for (int i = 0; i < halfSize; i++)
                {
                    int e = 2 * i;
                    halfSpectrum[i].X = samples[e] * win[e];
                    halfSpectrum[i].Y = samples[e + 1] * win[e + 1];
                }
            }
            else
            {
                for (int i = 0; i < halfSize; i++)
                {
                    halfSpectrum[i].X = samples[2 * i];
                    halfSpectrum[i].Y = samples[2 * i + 1];
                }
            }

            // N/2-point complex forward FFT. The existing FFT applies a 1/(N/2) scale, i.e. 2/N —
            // we compensate with a 0.5 multiplier in the unpack pass so the final output matches
            // the 1/N scaling of the full-size FastFourierTransform.FFT.
            FastFourierTransform.FFT(true, mHalf, halfSpectrum);

            // Unpack the N/2-point complex FFT of the packed sequence into the N-point real FFT result.
            // DC and Nyquist are derived from halfSpectrum[0], whose real part holds Y[0] (FFT of
            // even samples) and imaginary part holds Z[0] (FFT of odd samples), both already scaled
            // by the inner FFT's 2/N normalisation. The extra 0.5 multiplier brings the total to
            // 1/N so the output matches FastFourierTransform.FFT's scaling convention.
            spectrum[0].X = (halfSpectrum[0].X + halfSpectrum[0].Y) * 0.5f;
            spectrum[0].Y = 0f;
            spectrum[halfSize].X = (halfSpectrum[0].X - halfSpectrum[0].Y) * 0.5f;
            spectrum[halfSize].Y = 0f;

            // For k = 1..N/2-1: combine halfSpectrum[k] with halfSpectrum[N/2-k] using the real-FFT
            // trick. Derivation: if Y = FFT of evens, Z = FFT of odds, then X[k] = Y[k] + W^k * Z[k]
            // where W = e^(-j 2π/N). Recovering Y and Z from halfSpectrum (which is FFT(y + jz))
            // gives the formulas below.
            for (int k = 1; k < halfSize; k++)
            {
                float a = halfSpectrum[k].X;
                float b = halfSpectrum[k].Y;
                float c = halfSpectrum[halfSize - k].X;
                float d = halfSpectrum[halfSize - k].Y;

                // Y[k] = (halfSpectrum[k] + halfSpectrum[N/2-k]*) / 2  →  FFT of even samples
                float yR = (a + c) * 0.5f;
                float yI = (b - d) * 0.5f;

                // Z[k] = -j * (halfSpectrum[k] - halfSpectrum[N/2-k]*) / 2  →  FFT of odd samples
                float zR = (b + d) * 0.5f;
                float zI = (c - a) * 0.5f;

                // Apply twiddle W^k = cos - j sin (forward FFT sign convention)
                float tc = realTwiddleCos[k];
                float ts = realTwiddleSin[k];
                // (zR + j zI) * (tc + j ts) where ts is already negative (sin of a negative angle).
                float zTwR = zR * tc - zI * ts;
                float zTwI = zR * ts + zI * tc;

                // X[k] = Y[k] + W^k * Z[k], multiplied by an additional 0.5 to match the 1/N
                // normalisation convention of FastFourierTransform.FFT.
                spectrum[k].X = (yR + zTwR) * 0.5f;
                spectrum[k].Y = (yI + zTwI) * 0.5f;
            }
        }

        /// <summary>
        /// Inverse real FFT. Given the complex half-spectrum (<see cref="SpectrumLength"/> bins)
        /// produced by <see cref="RealForward"/>, reconstructs the <see cref="FftSize"/> time-domain
        /// samples. No window is applied on the inverse path — if the consumer applied a window on
        /// the forward path they must undo it themselves.
        /// </summary>
        /// <param name="spectrum">Complex half-spectrum. Must be <see cref="SpectrumLength"/> long.</param>
        /// <param name="samples">Output time-domain samples. Must be <see cref="FftSize"/> long.</param>
        public void RealInverse(ReadOnlySpan<Complex> spectrum, Span<float> samples)
        {
            if (spectrum.Length != halfSize + 1)
                throw new ArgumentException($"spectrum must be length {halfSize + 1}", nameof(spectrum));
            if (samples.Length != fftSize)
                throw new ArgumentException($"samples must be length {fftSize}", nameof(samples));

            // Invert the unpack pass: from the full N-bin real FFT result, derive the N/2-point
            // complex FFT of the packed sequence, then do an N/2-point inverse complex FFT, then
            // unpack the real/imag interleave back into sequential real samples.

            // Pack halfSpectrum[0] from X[0] and X[N/2]
            halfSpectrum[0].X = spectrum[0].X + spectrum[halfSize].X;
            halfSpectrum[0].Y = spectrum[0].X - spectrum[halfSize].X;

            for (int k = 1; k < halfSize; k++)
            {
                float xR = spectrum[k].X;
                float xI = spectrum[k].Y;
                float xRsym = spectrum[halfSize - k].X;
                float xIsym = spectrum[halfSize - k].Y;

                // Y[k] = (X[k] + X[N/2-k]*)
                float yR = xR + xRsym;
                float yI = xI - xIsym;

                // Z[k] * W^k = (X[k] - X[N/2-k]*)
                // Multiply by W^-k (conjugate of the forward twiddle) to recover Z[k]
                float tc = realTwiddleCos[k];
                float ts = realTwiddleSin[k];
                float dR = xR - xRsym;
                float dI = xI + xIsym;
                // Z[k] = (dR + j dI) * conj(W^k) = (dR + j dI) * (tc - j ts)
                //      = (dR*tc + dI*ts) + j(dI*tc - dR*ts)
                float zR = dR * tc + dI * ts;
                float zI = dI * tc - dR * ts;

                // halfSpectrum[k] = Y[k] + j * Z[k]
                halfSpectrum[k].X = yR - zI;
                halfSpectrum[k].Y = yI + zR;
            }

            // Inverse N/2-point complex FFT (un-scaled in the existing FastFourierTransform.FFT
            // implementation, which is what we want here).
            FastFourierTransform.FFT(false, mHalf, halfSpectrum);

            // Unpack: even samples from real parts, odd samples from imaginary parts.
            for (int i = 0; i < halfSize; i++)
            {
                samples[2 * i] = halfSpectrum[i].X;
                samples[2 * i + 1] = halfSpectrum[i].Y;
            }
        }

        /// <summary>
        /// In-place complex forward FFT. Thin forwarder to
        /// <see cref="FastFourierTransform.FFT(bool, int, Span{Complex})"/> for consumers that hold
        /// an <see cref="FftProcessor"/> instance and want symmetrical method names.
        /// </summary>
        public void ComplexForward(Span<Complex> data)
        {
            if (data.Length != fftSize)
                throw new ArgumentException($"data must be length {fftSize}", nameof(data));
            FastFourierTransform.FFT(true, mHalf + 1, data);
        }

        /// <summary>
        /// In-place complex inverse FFT. Thin forwarder to
        /// <see cref="FastFourierTransform.FFT(bool, int, Span{Complex})"/>.
        /// </summary>
        public void ComplexInverse(Span<Complex> data)
        {
            if (data.Length != fftSize)
                throw new ArgumentException($"data must be length {fftSize}", nameof(data));
            FastFourierTransform.FFT(false, mHalf + 1, data);
        }

        private static int Log2(int value)
        {
            int log = 0;
            while (value > 1) { value >>= 1; log++; }
            return log;
        }
    }
}
