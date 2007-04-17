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
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dsp
{
    class BiQuadFilter
    {
        // coefficients
        double a0;
        double a1;
        double a2;
        double b0;
        double b1;
        double b2;

        public void Transform(float[] inBuffer, float[] outBuffer)
        {
            float[] x = inBuffer;
            float[] y = outBuffer;

            for(int n = 0; n < inBuffer.Length; n++)
            {
                y[n] = (float) (
                    (b0/a0)*x[n] + (b1/a0)*x[n-1] + (b2/a0)*x[n-2]
                    - (a1/a0)*y[n-1] - (a2/a0)*y[n-2]);
            }
        }

        /// <summary>
        /// H(s) = 1 / (s^2 + s/Q + 1)
        /// </summary>
        public static BiQuadFilter LowPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            double w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double alpha = Math.Sin(w0) / (2 * q);

            BiQuadFilter filter = new BiQuadFilter();
            
            filter.b0 =  (1 - cosw0) / 2;
            filter.b1 =   1 - cosw0;
            filter.b2 =  (1 - cosw0)/2;
            filter.a0 =   1 + alpha;
            filter.a1 =  -2 * cosw0;
            filter.a2 =   1 - alpha;
            return filter;
        }

        /// <summary>
        /// H(s) = s^2 / (s^2 + s/Q + 1)
        /// </summary>
        public static BiQuadFilter HighPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            double w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double alpha = Math.Sin(w0) / (2 * q);

            BiQuadFilter filter = new BiQuadFilter();

            filter.b0 =  (1 + Math.Cos(w0))/2;
            filter.b1 = -(1 + Math.Cos(w0));
            filter.b2 =  (1 + Math.Cos(w0))/2;
            filter.a0 =   1 + alpha;
            filter.a1 =  -2*Math.Cos(w0);
            filter.a2 =   1 - alpha;
            return filter;
        }
        
        /// <summary>
        /// H(s) = s / (s^2 + s/Q + 1)  (constant skirt gain, peak gain = Q)
        /// </summary>
        public static BiQuadFilter BandPassFilterConstantSkirtGain(float sampleRate, float centreFrequency, float q)
        {
            double w0 = 2 * Math.PI * centreFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);
            double alpha = sinw0 / (2 * q);

            BiQuadFilter filter = new BiQuadFilter();
            filter.b0 =   sinw0 / 2; // =   Q*alpha
            filter.b1 =   0;
            filter.b2 =  -sinw0 / 2; // =  -Q*alpha
            filter.a0 =   1 + alpha;
            filter.a1 =  -2 * cosw0;
            filter.a2 =   1 - alpha;
            return filter;
        }

        /// <summary>
        /// H(s) = (s/Q) / (s^2 + s/Q + 1)      (constant 0 dB peak gain)
        /// </summary>
        public static BiQuadFilter BandPassFilterConstantPeakGain(float sampleRate, float centreFrequency, float q)
        {
            double w0 = 2 * Math.PI * centreFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);
            double alpha = sinw0 / (2 * q);

            BiQuadFilter filter = new BiQuadFilter();
            filter.b0 =   alpha;
            filter.b1 =   0;
            filter.b2 =  -alpha;
            filter.a0 =   1 + alpha;
            filter.a1 =  -2*cosw0;
            filter.a2 =   1 - alpha;
            return filter;
        }

        /// <summary>
        /// H(s) = (s^2 + 1) / (s^2 + s/Q + 1)
        /// </summary>
        public static BiQuadFilter NotchFilter(float sampleRate, float centreFrequency, float q)
        {
            double w0 = 2 * Math.PI * centreFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);
            double alpha = sinw0 / (2 * q);

            BiQuadFilter filter = new BiQuadFilter();
            filter.b0 =   1;
            filter.b1 =  -2*cosw0;
            filter.b2 =   1;
            filter.a0 =   1 + alpha;
            filter.a1 =  -2*cosw0;
            filter.a2 =   1 - alpha;
            return filter;
        }

        /// <summary>
        /// H(s) = (s^2 - s/Q + 1) / (s^2 + s/Q + 1)
        /// </summary>
        public static BiQuadFilter AllPassFilter(float sampleRate, float centreFrequency, float q)
        {
            double w0 = 2 * Math.PI * centreFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);
            double alpha = sinw0 / (2 * q);

            BiQuadFilter filter = new BiQuadFilter();
            filter.b0 =   1 - alpha;
            filter.b1 =  -2 * cosw0;
            filter.b2 =   1 + alpha;
            filter.a0 =   1 + alpha;
            filter.a1 =  -2 * cosw0;
            filter.a2 =   1 - alpha;
            return filter;
        }

        /// <summary>
        /// H(s) = (s^2 + s*(A/Q) + 1) / (s^2 + s/(A*Q) + 1)
        /// </summary>
        public static BiQuadFilter PeakingEQ(float sampleRate, float centreFrequency, float q, float dbGain)
        {
            double w0 = 2 * Math.PI * centreFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);
            double alpha = sinw0 / (2 * q);
            double A = Math.Pow(10, dbGain / 40);     // TODO: should we square root this value?

            BiQuadFilter filter = new BiQuadFilter();
            filter.b0 =   1 + alpha*A;
            filter.b1 =  -2*cosw0;
            filter.b2 =   1 - alpha*A;
            filter.a0 =   1 + alpha/A;
            filter.a1 =  -2*cosw0;
            filter.a2 =   1 - alpha/A;
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
            double w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);
            double A = Math.Pow(10, dbGain / 40);     // TODO: should we square root this value?
            double alpha = sinw0 / 2 * Math.Sqrt((A + 1 / A) * (1 / shelfSlope - 1) + 2);
            double temp = 2 * Math.Sqrt(A) * alpha;
            BiQuadFilter filter = new BiQuadFilter();
            filter.b0 =    A*( (A+1) - (A-1)*cosw0 + temp );
            filter.b1 =  2*A*( (A-1) - (A+1)*cosw0        );
            filter.b2 =    A*( (A+1) - (A-1)*cosw0 - temp );
            filter.a0 =        (A+1) + (A-1)*cosw0 + temp;
            filter.a1 =   -2*( (A-1) + (A+1)*cosw0        );
            filter.a2 =        (A+1) + (A-1)*cosw0 - temp;
            return filter;
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
            double w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);
            double A = Math.Pow(10, dbGain / 40);     // TODO: should we square root this value?
            double alpha = sinw0 / 2 * Math.Sqrt((A + 1 / A) * (1 / shelfSlope - 1) + 2);
            double temp = 2 * Math.Sqrt(A) * alpha;
            
            BiQuadFilter filter = new BiQuadFilter();
            filter.b0 =    A*( (A+1) + (A-1)*cosw0 + temp );
            filter.b1 = -2*A*( (A-1) + (A+1)*cosw0        );
            filter.b2 =    A*( (A+1) + (A-1)*cosw0 - temp );
            filter.a0 =        (A+1) - (A-1)*cosw0 + temp;
            filter.a1 =    2*( (A-1) - (A+1)*cosw0        );
            filter.a2 =        (A+1) - (A-1)*cosw0 - temp;
            return filter;
        }

        private BiQuadFilter()
        {
        }
    }
}

