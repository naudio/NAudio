// based on Cookbook formulae for audio EQ biquad filter coefficients
// http://www.musicdsp.org/files/Audio-EQ-Cookbook.txt
// by Robert Bristow-Johnson  <rbj@audioimagination.com>

//    alpha = sin(w0)/(2*Q)                                       (case: Q)
//          = sin(w0)*sinh( ln(2)/2 * BW * w0/sin(w0) )           (case: BW)
//          = sin(w0)/2 * sqrt( (A + 1/A)*(1/S - 1) + 2 )         (case: S)
// Q: (the EE kind of definition, except for peakingEQ in which A*Q is
// the classic EE Q.  That adjustment in definition was made so that
// a boost of N dB followed by a cut of N dB for identical Q and
// f0/Fs results in a precisely flat unity gain filter or "wire".)
//
// BW: the bandwidth in octaves (between -3 dB frequencies for BPF
// and notch or between midpoint (dBgain/2) gain frequencies for
// peaking EQ)
//
// S: a "shelf slope" parameter (for shelving EQ only).  When S = 1,
// the shelf slope is as steep as it can be and remain monotonically
// increasing or decreasing gain with frequency.  The shelf slope, in
// dB/octave, remains proportional to S for all other values for a
// fixed f0/Fs and dBgain.

using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// BiQuad filter
    /// </summary>
    public class BiQuadFilter
    {
        // coefficients
        private double a0;
        private double a1;
        private double a2;
        private double a3;
        private double a4;

        // state
        private float x1;
        private float x2;
        private float y1;
        private float y2;

        /// <summary>
        /// Passes a single sample through the filter
        /// </summary>
        /// <param name="inSample">Input sample</param>
        /// <returns>Output sample</returns>
        public float Transform(float inSample)
        {
            // compute result
            var result = a0 * inSample + a1 * x1 + a2 * x2 - a3 * y1 - a4 * y2;

            // shift x1 to x2, sample to x1
            x2 = x1;
            x1 = inSample;

            // shift y1 to y2, result to y1
            y2 = y1;
            y1 = (float)result;

            return y1;
        }

        /// <summary>
        /// Passes a block of samples through the filter. Equivalent to — and produces byte-identical
        /// output to — calling <see cref="Transform(float)"/> on each element of
        /// <paramref name="source"/> in order, but keeps the coefficients and state variables in
        /// locals so the JIT can hold them in registers across the loop.
        /// </summary>
        /// <param name="source">Input samples.</param>
        /// <param name="destination">Output samples. May be the same span as <paramref name="source"/>
        /// (in-place filtering) or a separate buffer at least as long as <paramref name="source"/>.</param>
        /// <remarks>
        /// A biquad has a forward-only dependency (the next output depends on previous inputs AND
        /// outputs), so the inner loop can't be vectorised; the speedup over the single-sample form
        /// comes entirely from not having to reload field values each iteration.
        /// </remarks>
        public void Transform(ReadOnlySpan<float> source, Span<float> destination)
        {
            if (destination.Length < source.Length)
                throw new ArgumentException("Destination must be at least as long as source.", nameof(destination));

            // Hoist fields into locals — field reads inside the loop force the JIT to assume they
            // might be mutated by anything else sharing `this` and prevent register allocation.
            double la0 = a0, la1 = a1, la2 = a2, la3 = a3, la4 = a4;
            float lx1 = x1, lx2 = x2, ly1 = y1, ly2 = y2;

            for (int i = 0; i < source.Length; i++)
            {
                float inSample = source[i];
                double result = la0 * inSample + la1 * lx1 + la2 * lx2 - la3 * ly1 - la4 * ly2;
                lx2 = lx1;
                lx1 = inSample;
                ly2 = ly1;
                ly1 = (float)result;
                destination[i] = ly1;
            }

            x1 = lx1;
            x2 = lx2;
            y1 = ly1;
            y2 = ly2;
        }

        /// <summary>
        /// Clears the filter's sample history (the x/y delay elements) without
        /// changing its coefficients, so the next input is filtered as if from
        /// silence. Use when reusing a filter on a new, unrelated signal (e.g. an
        /// effect's <c>Reset()</c>).
        /// </summary>
        public void ResetState()
        {
            x1 = x2 = 0f;
            y1 = y2 = 0f;
        }

        private void SetCoefficients(double aa0, double aa1, double aa2, double b0, double b1, double b2)
        {
            // precompute the coefficients
            a0 = b0/aa0;
            a1 = b1/aa0;
            a2 = b2/aa0;
            a3 = aa1/aa0;
            a4 = aa2/aa0;

            // reset state so a previously divergent run (Infinity/NaN latched into
            // y1/y2) cannot survive a coefficient change and permanently kill the filter
            x1 = x2 = 0;
            y1 = y2 = 0;
        }

        private static void ValidateParameters(float sampleRate, float frequency, string frequencyParamName, float qOrSlope, string qOrSlopeParamName)
        {
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
            if (frequency <= 0 || frequency >= sampleRate / 2)
                throw new ArgumentOutOfRangeException(frequencyParamName, $"Frequency must be greater than 0 and less than sampleRate/2 (Nyquist = {sampleRate / 2} Hz)");
            if (qOrSlope <= 0)
                throw new ArgumentOutOfRangeException(qOrSlopeParamName, $"{qOrSlopeParamName} must be positive");
        }

        /// <summary>
        /// Set this up as a low pass filter
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="cutoffFrequency">Cut-off Frequency</param>
        /// <param name="q">Q (quality factor). Use 1/sqrt(2) ≈ 0.707 for a Butterworth response
        /// (maximally flat passband, no peaking) — the recommended default for a clean low-pass.
        /// Larger values produce a resonant peak at the cutoff; smaller values give a more
        /// gradual roll-off into the cutoff. The slope above the cutoff is ~12 dB/octave
        /// regardless of Q — cascade biquads in series for a steeper roll-off.</param>
        public void SetLowPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            ValidateParameters(sampleRate, cutoffFrequency, nameof(cutoffFrequency), q, nameof(q));
            // H(s) = 1 / (s^2 + s/Q + 1)
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            var b0 = (1 - cosw0) / 2;
            var b1 = 1 - cosw0;
            var b2 = (1 - cosw0) / 2;
            var aa0 = 1 + alpha;
            var aa1 = -2 * cosw0;
            var aa2 = 1 - alpha;
            SetCoefficients(aa0,aa1,aa2,b0,b1,b2);
        }

        /// <summary>
        /// Retunes this filter to a new low-pass cutoff and Q <em>without</em>
        /// clearing its sample history, so a running filter can be modulated
        /// every sample/block (e.g. a synth filter envelope or LFO, an auto-wah)
        /// without the click that <see cref="SetLowPassFilter"/> causes by
        /// resetting state. Use only on an already-running, non-divergent filter;
        /// for a fresh filter or after a seek use <see cref="SetLowPassFilter"/>
        /// (or <see cref="ResetState"/>) so latched NaN/Infinity can't survive.
        /// </summary>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="cutoffFrequency">New cut-off frequency.</param>
        /// <param name="q">New Q (quality factor).</param>
        public void UpdateLowPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            ValidateParameters(sampleRate, cutoffFrequency, nameof(cutoffFrequency), q, nameof(q));
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            var b0 = (1 - cosw0) / 2;
            var b1 = 1 - cosw0;
            var b2 = (1 - cosw0) / 2;
            var aa0 = 1 + alpha;
            var aa1 = -2 * cosw0;
            var aa2 = 1 - alpha;

            // recompute coefficients only — preserve x1/x2/y1/y2
            a0 = b0 / aa0;
            a1 = b1 / aa0;
            a2 = b2 / aa0;
            a3 = aa1 / aa0;
            a4 = aa2 / aa0;
        }

        /// <summary>
        /// Retunes this filter as a high-pass without clearing its delay state, so it can be
        /// modulated per block/sample without the click <see cref="SetHighPassFilter"/> causes.
        /// </summary>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="cutoffFrequency">New cut-off frequency.</param>
        /// <param name="q">New Q (quality factor).</param>
        public void UpdateHighPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            ValidateParameters(sampleRate, cutoffFrequency, nameof(cutoffFrequency), q, nameof(q));
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            UpdateNormalisedCoefficients(1 + alpha, -2 * cosw0, 1 - alpha,
                (1 + cosw0) / 2, -(1 + cosw0), (1 + cosw0) / 2);
        }

        /// <summary>
        /// Retunes this filter as a band-pass (constant 0 dB peak gain) without clearing its
        /// delay state, so it can be modulated per block/sample without a click.
        /// </summary>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="centreFrequency">New centre frequency.</param>
        /// <param name="q">New Q (quality factor).</param>
        public void UpdateBandPassFilter(float sampleRate, float centreFrequency, float q)
        {
            ValidateParameters(sampleRate, centreFrequency, nameof(centreFrequency), q, nameof(q));
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            UpdateNormalisedCoefficients(1 + alpha, -2 * cosw0, 1 - alpha, alpha, 0, -alpha);
        }

        /// <summary>
        /// Retunes this filter as a notch (band-reject) without clearing its delay state, so it
        /// can be modulated per block/sample without a click.
        /// </summary>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="centreFrequency">New centre frequency.</param>
        /// <param name="q">New Q (quality factor).</param>
        public void UpdateNotchFilter(float sampleRate, float centreFrequency, float q)
        {
            ValidateParameters(sampleRate, centreFrequency, nameof(centreFrequency), q, nameof(q));
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            UpdateNormalisedCoefficients(1 + alpha, -2 * cosw0, 1 - alpha, 1, -2 * cosw0, 1);
        }

        // recompute coefficients only — preserve x1/x2/y1/y2 (state-preserving retune)
        private void UpdateNormalisedCoefficients(double aa0, double aa1, double aa2,
            double b0, double b1, double b2)
        {
            a0 = b0 / aa0;
            a1 = b1 / aa0;
            a2 = b2 / aa0;
            a3 = aa1 / aa0;
            a4 = aa2 / aa0;
        }
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="centreFrequency">Centre Frequency</param>
        /// <param name="q">Q (quality factor). Higher Q gives a narrower peak around the centre
        /// frequency; lower Q gives a wider, gentler bell.</param>
        /// <param name="dbGain">Gain in decibels</param>
        public void SetPeakingEq(float sampleRate, float centreFrequency, float q, float dbGain)
        {
            ValidateParameters(sampleRate, centreFrequency, nameof(centreFrequency), q, nameof(q));
            // H(s) = (s^2 + s*(A/Q) + 1) / (s^2 + s/(A*Q) + 1)
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);
            var a = Math.Pow(10, dbGain / 40);

            var b0 = 1 + alpha * a;
            var b1 = -2 * cosw0;
            var b2 = 1 - alpha * a;
            var aa0 = 1 + alpha / a;
            var aa1 = -2 * cosw0;
            var aa2 = 1 - alpha / a;
            SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        /// <summary>
        /// Set this as a high pass filter
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="cutoffFrequency">Cut-off Frequency</param>
        /// <param name="q">Q (quality factor). Use 1/sqrt(2) ≈ 0.707 for a Butterworth response
        /// (maximally flat passband, no peaking) — the recommended default for a clean high-pass.
        /// Larger values produce a resonant peak at the cutoff; smaller values give a more
        /// gradual roll-off into the cutoff. The slope below the cutoff is ~12 dB/octave
        /// regardless of Q — cascade biquads in series for a steeper roll-off.</param>
        public void SetHighPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            ValidateParameters(sampleRate, cutoffFrequency, nameof(cutoffFrequency), q, nameof(q));
            // H(s) = s^2 / (s^2 + s/Q + 1)
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            var b0 = (1 + cosw0) / 2;
            var b1 = -(1 + cosw0);
            var b2 = (1 + cosw0) / 2;
            var aa0 = 1 + alpha;
            var aa1 = -2 * cosw0;
            var aa2 = 1 - alpha;
            SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        /// <summary>
        /// Create a low pass filter
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="cutoffFrequency">Cut-off Frequency</param>
        /// <param name="q">Q (quality factor). Use 1/sqrt(2) ≈ 0.707 for a Butterworth response
        /// (maximally flat passband, no peaking). The slope above the cutoff is ~12 dB/octave
        /// regardless of Q — cascade biquads in series for a steeper roll-off.</param>
        public static BiQuadFilter LowPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            var filter = new BiQuadFilter();
            filter.SetLowPassFilter(sampleRate,cutoffFrequency,q);
            return filter;
        }

        /// <summary>
        /// Create a High pass filter
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="cutoffFrequency">Cut-off Frequency</param>
        /// <param name="q">Q (quality factor). Use 1/sqrt(2) ≈ 0.707 for a Butterworth response
        /// (maximally flat passband, no peaking). The slope below the cutoff is ~12 dB/octave
        /// regardless of Q — cascade biquads in series for a steeper roll-off.</param>
        public static BiQuadFilter HighPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            var filter = new BiQuadFilter();
            filter.SetHighPassFilter(sampleRate, cutoffFrequency, q);
            return filter;
        }

        /// <summary>
        /// Create a bandpass filter with constant skirt gain
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="centreFrequency">Centre Frequency</param>
        /// <param name="q">Q (quality factor). Higher Q gives a narrower band; lower Q gives a
        /// wider band. Peak gain at the centre frequency equals Q.</param>
        public static BiQuadFilter BandPassFilterConstantSkirtGain(float sampleRate, float centreFrequency, float q)
        {
            ValidateParameters(sampleRate, centreFrequency, nameof(centreFrequency), q, nameof(q));
            // H(s) = s / (s^2 + s/Q + 1)  (constant skirt gain, peak gain = Q)
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = sinw0 / 2; // =   Q*alpha
            var b1 = 0;
            var b2 = -sinw0 / 2; // =  -Q*alpha
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Create a bandpass filter with constant peak gain
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="centreFrequency">Centre Frequency</param>
        /// <param name="q">Q (quality factor). Higher Q gives a narrower band; lower Q gives a
        /// wider band. Peak gain at the centre frequency is 0 dB regardless of Q.</param>
        public static BiQuadFilter BandPassFilterConstantPeakGain(float sampleRate, float centreFrequency, float q)
        {
            ValidateParameters(sampleRate, centreFrequency, nameof(centreFrequency), q, nameof(q));
            // H(s) = (s/Q) / (s^2 + s/Q + 1)      (constant 0 dB peak gain)
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = alpha;
            var b1 = 0;
            var b2 = -alpha;
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Creates a notch filter
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="centreFrequency">Centre Frequency</param>
        /// <param name="q">Q (quality factor). Higher Q gives a narrower notch; lower Q gives a
        /// wider notch.</param>
        public static BiQuadFilter NotchFilter(float sampleRate, float centreFrequency, float q)
        {
            ValidateParameters(sampleRate, centreFrequency, nameof(centreFrequency), q, nameof(q));
            // H(s) = (s^2 + 1) / (s^2 + s/Q + 1)
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = 1;
            var b1 = -2 * cosw0;
            var b2 = 1;
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Creates an all pass filter
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="centreFrequency">Centre Frequency</param>
        /// <param name="q">Q (quality factor). Controls how sharply the phase transitions
        /// around the centre frequency.</param>
        public static BiQuadFilter AllPassFilter(float sampleRate, float centreFrequency, float q)
        {
            ValidateParameters(sampleRate, centreFrequency, nameof(centreFrequency), q, nameof(q));
            //H(s) = (s^2 - s/Q + 1) / (s^2 + s/Q + 1)
            var w0 = 2 * Math.PI * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = 1 - alpha;
            var b1 = -2 * cosw0;
            var b2 = 1 + alpha;
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Create a Peaking EQ
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="centreFrequency">Centre Frequency</param>
        /// <param name="q">Q (quality factor). Higher Q gives a narrower peak around the centre
        /// frequency; lower Q gives a wider, gentler bell.</param>
        /// <param name="dbGain">Gain in decibels</param>
        public static BiQuadFilter PeakingEQ(float sampleRate, float centreFrequency, float q, float dbGain)
        {
            var filter = new BiQuadFilter();
            filter.SetPeakingEq(sampleRate, centreFrequency, q, dbGain);
            return filter;
        }

        /// <summary>
        /// H(s) = A * (s^2 + (sqrt(A)/Q)*s + A)/(A*s^2 + (sqrt(A)/Q)*s + 1)
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="cutoffFrequency"></param>
        /// <param name="shelfSlope">a "shelf slope" parameter (for shelving EQ only).  
        /// When S = 1, the shelf slope is as steep as it can be and remain monotonically
        /// increasing or decreasing gain with frequency.  The shelf slope, in dB/octave, 
        /// remains proportional to S for all other values for a fixed f0/Fs and dBgain.</param>
        /// <param name="dbGain">Gain in decibels</param>
        public static BiQuadFilter LowShelf(float sampleRate, float cutoffFrequency, float shelfSlope, float dbGain)
        {
            ValidateParameters(sampleRate, cutoffFrequency, nameof(cutoffFrequency), shelfSlope, nameof(shelfSlope));
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var a = Math.Pow(10, dbGain / 40);
            var alpha = sinw0 / 2 * Math.Sqrt((a + 1 / a) * (1 / shelfSlope - 1) + 2);
            var temp = 2 * Math.Sqrt(a) * alpha;
            
            var b0 = a * ((a + 1) - (a - 1) * cosw0 + temp);
            var b1 = 2 * a * ((a - 1) - (a + 1) * cosw0);
            var b2 = a * ((a + 1) - (a - 1) * cosw0 - temp);
            var a0 = (a + 1) + (a - 1) * cosw0 + temp;
            var a1 = -2 * ((a - 1) + (a + 1) * cosw0);
            var a2 = (a + 1) + (a - 1) * cosw0 - temp;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// H(s) = A * (A*s^2 + (sqrt(A)/Q)*s + 1)/(s^2 + (sqrt(A)/Q)*s + A)
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="cutoffFrequency"></param>
        /// <param name="shelfSlope"></param>
        /// <param name="dbGain"></param>
        /// <returns></returns>
        public static BiQuadFilter HighShelf(float sampleRate, float cutoffFrequency, float shelfSlope, float dbGain)
        {
            ValidateParameters(sampleRate, cutoffFrequency, nameof(cutoffFrequency), shelfSlope, nameof(shelfSlope));
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var a = Math.Pow(10, dbGain / 40);
            var alpha = sinw0 / 2 * Math.Sqrt((a + 1 / a) * (1 / shelfSlope - 1) + 2);
            var temp = 2 * Math.Sqrt(a) * alpha;

            var b0 = a * ((a + 1) + (a - 1) * cosw0 + temp);
            var b1 = -2 * a * ((a - 1) + (a + 1) * cosw0);
            var b2 = a * ((a + 1) + (a - 1) * cosw0 - temp);
            var a0 = (a + 1) - (a - 1) * cosw0 + temp;
            var a1 = 2 * ((a - 1) - (a + 1) * cosw0);
            var a2 = (a + 1) - (a - 1) * cosw0 - temp;
            return new BiQuadFilter(a0, a1, a2, b0, b1, b2);
        }

        private BiQuadFilter()
        {
            // zero initial samples
            x1 = x2 = 0;
            y1 = y2 = 0;
        }

        private BiQuadFilter(double a0, double a1, double a2, double b0, double b1, double b2)
        {
            SetCoefficients(a0,a1,a2,b0,b1,b2);

            // zero initial samples
            x1 = x2 = 0;
            y1 = y2 = 0;
        }
    }
}
