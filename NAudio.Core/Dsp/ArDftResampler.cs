using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Offline (batch) sample-rate converter implementing the ARDFTSRC algorithm of
    /// mycroft @ hydrogenaudio.org (C++ port by dBpoweramp). Each input chunk of
    /// <c>N = sourceRate / gcd * factor</c> samples is zero-padded into a 2N FFT,
    /// the spectrum is bin-truncated/-padded and tapered to a 2M spectrum, and the
    /// result is inverse-DFT'd to 2M samples; consecutive 2M frames are overlap-added
    /// at hop M to produce the resampled output. Built on <see cref="BluesteinDft"/>
    /// so the (rarely power-of-two) block sizes 2N and 2M are handled directly.
    /// </summary>
    /// <remarks>
    /// This is a research / spike implementation. It does not stream — callers
    /// supply a whole signal and get a whole output array back, mono only.
    /// Multichannel and an <c>ISampleProvider</c> wrapper come later if the
    /// quality / performance evaluation justifies shipping.
    ///
    /// The algorithm has an inherent half-block-of-input latency (the input chunk
    /// is centred in a 2N-size FFT) which manifests as a half-block delay in the
    /// output relative to the input. The reference implementation tolerates this
    /// rather than trimming it, and so do we.
    /// </remarks>
    internal sealed class ArDftResampler
    {
        private readonly int inChunk;
        private readonly int outChunk;
        private readonly int inFftSize;
        private readonly int outFftSize;
        private readonly int inOffset;
        private readonly BluesteinDft inDft;
        private readonly BluesteinDft outDft;
        private readonly float[] taper;
        private readonly Complex[] inBuf;
        private readonly Complex[] outBuf;

        public int SourceRate { get; }
        public int TargetRate { get; }

        /// <summary>
        /// Number of samples consumed from the input per processing step. Matches
        /// the reference implementation's <c>in_nb_samples</c>.
        /// </summary>
        public int InputChunkSize => inChunk;

        /// <summary>
        /// Number of samples produced per processing step. Matches the reference
        /// implementation's <c>out_nb_samples</c>.
        /// </summary>
        public int OutputChunkSize => outChunk;

        /// <summary>
        /// Construct a resampler for the given source/target rates.
        /// </summary>
        /// <param name="sourceRate">Source sample rate (Hz).</param>
        /// <param name="targetRate">Target sample rate (Hz).</param>
        /// <param name="quality">Minimum effective output block size; larger values
        /// give a sharper transition band at the cost of more memory and CPU per chunk.
        /// Default 2048 matches the reference implementation.</param>
        /// <param name="bandwidth">Fraction of Nyquist that is passed flat before the
        /// taper begins. Default 0.95 matches the reference.</param>
        public ArDftResampler(int sourceRate, int targetRate, int quality = 2048, double bandwidth = 0.95)
        {
            if (sourceRate <= 0) throw new ArgumentOutOfRangeException(nameof(sourceRate));
            if (targetRate <= 0) throw new ArgumentOutOfRangeException(nameof(targetRate));
            if (quality < 16) throw new ArgumentOutOfRangeException(nameof(quality));
            if (bandwidth <= 0.0 || bandwidth > 1.0) throw new ArgumentOutOfRangeException(nameof(bandwidth));

            SourceRate = sourceRate;
            TargetRate = targetRate;

            int gcd = Gcd(sourceRate, targetRate);
            long inNb = sourceRate / gcd;
            long outNb = targetRate / gcd;

            // Reference: factor = 2 * ceil(quality / (2 * out_nb_samples)). Forces an even
            // multiplier so the post-multiplication block sizes are even (we rely on that
            // for the 50% overlap-add and the symmetric Nyquist bin).
            long factor = (long)(2.0 * Math.Ceiling(quality / (2.0 * outNb)));
            if (factor < 1) factor = 1;
            inNb *= factor;
            outNb *= factor;

            // Sanity bound — keeps the FFT scratch buffers below ~256 MB even at 32-bit
            // output. Real audio rate ratios will never come close.
            const int maxChunk = 1 << 24;
            if (inNb > maxChunk || outNb > maxChunk)
                throw new ArgumentOutOfRangeException(nameof(quality), "Resulting block size exceeds the safety limit.");

            inChunk = (int)inNb;
            outChunk = (int)outNb;
            inFftSize = inChunk * 2;
            outFftSize = outChunk * 2;
            inOffset = (inFftSize - inChunk) / 2;

            inDft = new BluesteinDft(inFftSize);
            outDft = new BluesteinDft(outFftSize);

            // Taper acts as the anti-alias / band-limit filter, applied directly to the
            // forward-DFT bins. Width is (1 - bandwidth) * min(inNb, outNb) bins; shape
            // is the reference's sigmoid-style roll-off.
            long trCutoff = Math.Min(inNb, outNb);
            long taperSamples = (long)(trCutoff * (1.0 - bandwidth));
            int taperLen = inFftSize / 2 + 1;
            taper = new float[taperLen];
            for (int k = 0; k < taperLen; k++)
            {
                if (k < trCutoff - taperSamples)
                {
                    taper[k] = 1.0f;
                }
                else if (k < trCutoff - 1)
                {
                    double n = k - (trCutoff - taperSamples);
                    double t = taperSamples;
                    double zbk = t / ((t - n) - 1.0) - t / (n + 1.0);
                    taper[k] = (float)(1.0 / (Math.Exp(zbk) + 1.0));
                }
                else
                {
                    taper[k] = 0.0f;
                }
            }

            inBuf = new Complex[inFftSize];
            outBuf = new Complex[outFftSize];
        }

        /// <summary>
        /// Resample a mono signal in one go. The output length is
        /// <c>ceil(input.Length / InputChunkSize) * OutputChunkSize</c> — the input is
        /// implicitly zero-padded up to a multiple of <see cref="InputChunkSize"/>.
        /// </summary>
        public float[] Process(ReadOnlySpan<float> input)
        {
            int chunks = input.Length == 0 ? 0 : (input.Length + inChunk - 1) / inChunk;
            long outLen = (long)chunks * outChunk;
            var output = new float[outLen];
            if (chunks == 0) return output;

            var prev = new float[outChunk];

            for (int i = 0; i < chunks; i++)
            {
                int chunkStart = i * inChunk;
                int chunkLen = Math.Min(inChunk, input.Length - chunkStart);

                // Centre the input chunk in the 2N FFT buffer; the surrounding zeros are
                // what give the spectrum-domain taper room to band-limit cleanly.
                Array.Clear(inBuf, 0, inFftSize);
                for (int k = 0; k < chunkLen; k++)
                {
                    inBuf[inOffset + k].X = input[chunkStart + k];
                }

                inDft.Forward(inBuf);

                // Copy the non-negative-frequency bins (with taper) from the inFft spectrum
                // to the outFft spectrum, then restore Hermitian symmetry on the
                // negative-frequency side so the inverse DFT produces a real signal.
                Array.Clear(outBuf, 0, outFftSize);
                int binsToCopy = Math.Min(inFftSize / 2 + 1, outFftSize / 2 + 1);
                for (int k = 0; k < binsToCopy; k++)
                {
                    float t = taper[k];
                    outBuf[k].X = inBuf[k].X * t;
                    outBuf[k].Y = inBuf[k].Y * t;
                }
                for (int k = 1; k < outFftSize / 2; k++)
                {
                    outBuf[outFftSize - k].X = outBuf[k].X;
                    outBuf[outFftSize - k].Y = -outBuf[k].Y;
                }

                outDft.Inverse(outBuf);

                // Overlap-add: top M samples of this 2M frame combine with the saved
                // bottom M samples of the previous frame; bottom M is held over for next.
                int outStart = i * outChunk;
                for (int k = 0; k < outChunk; k++)
                {
                    output[outStart + k] = outBuf[k].X + prev[k];
                }
                for (int k = 0; k < outChunk; k++)
                {
                    prev[k] = outBuf[outChunk + k].X;
                }
            }

            return output;
        }

        private static int Gcd(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }
            return a;
        }
    }
}
</content>
</invoke>