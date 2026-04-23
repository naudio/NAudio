using System;
using NAudio.Dsp;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Author: Freefall
    /// Date: 05.08.16
    /// Based on: the port of Stephan M. Bernsee´s pitch shifting class
    /// Port site: https://sites.google.com/site/mikescoderama/pitch-shifting
    /// Test application and github site: https://github.com/Freefall63/NAudio-Pitchshifter
    ///
    /// NOTE: I strongly advice to add a Limiter for post-processing.
    /// For my needs the FastAttackCompressor1175 provides acceptable results:
    /// https://github.com/Jiyuu/SkypeFX/blob/master/JSNet/FastAttackCompressor1175.cs
    ///
    /// UPDATE: Added a simple Limiter based on the pydirac implementation.
    /// https://github.com/echonest/remix/blob/master/external/pydirac225/source/Dirac_LE.cpp
    ///
    ///</summary>
    public class SmbPitchShiftingSampleProvider : ISampleProvider
    {
        //Shifter objects
        private readonly ISampleProvider sourceStream;
        private readonly WaveFormat waveFormat;
        private float pitch = 1f;
        private readonly int fftSize;
        private readonly long osamp;
        private readonly SmbPitchShifter shifterLeft = new SmbPitchShifter();
        private readonly SmbPitchShifter shifterRight = new SmbPitchShifter();

        // Reused across Read calls on the stereo path to avoid per-Read allocations.
        private float[] leftChannelBuffer;
        private float[] rightChannelBuffer;

        //Limiter constants
        const float LIM_THRESH = 0.95f;
        const float LIM_RANGE = (1f - LIM_THRESH);
        const float M_PI_2 = (float) (Math.PI/2);

        /// <summary>
        /// Creates a new SMB Pitch Shifting Sample Provider with default settings
        /// </summary>
        /// <param name="sourceProvider">Source provider</param>
        public SmbPitchShiftingSampleProvider(ISampleProvider sourceProvider)
            : this(sourceProvider, 4096, 4L, 1f)
        {
        }

        /// <summary>
        /// Creates a new SMB Pitch Shifting Sample Provider with custom settings
        /// </summary>
        /// <param name="sourceProvider">Source provider</param>
        /// <param name="fftSize">FFT Size (any power of two &lt;= 4096: 4096, 2048, 1024, 512, ...)</param>
        /// <param name="osamp">Oversampling (number of overlapping windows)</param>
        /// <param name="initialPitch">Initial pitch (0.5f = octave down, 1.0f = normal, 2.0f = octave up)</param>
        public SmbPitchShiftingSampleProvider(ISampleProvider sourceProvider, int fftSize, long osamp,
            float initialPitch)
        {
            sourceStream = sourceProvider;
            waveFormat = sourceProvider.WaveFormat;
            this.fftSize = fftSize;
            this.osamp = osamp;
            PitchFactor = initialPitch;
        }

        /// <summary>
        /// Read from this sample provider
        /// </summary>
        public int Read(Span<float> buffer)
        {
            int sampRead = sourceStream.Read(buffer);
            if (pitch == 1f)
            {
                //Nothing to do.
                return sampRead;
            }
            if (waveFormat.Channels == 1)
            {
                // Mono: PitchShift operates in place on the caller's span — no intermediate buffer.
                var mono = buffer.Slice(0, sampRead);
                shifterLeft.PitchShift(pitch, sampRead, fftSize, osamp, waveFormat.SampleRate, mono);
                for (var sample = 0; sample < sampRead; sample++)
                {
                    mono[sample] = Limiter(mono[sample]);
                }
                return sampRead;
            }
            if (waveFormat.Channels == 2)
            {
                int perChannel = sampRead >> 1;
                leftChannelBuffer = BufferHelpers.Ensure(leftChannelBuffer, perChannel);
                rightChannelBuffer = BufferHelpers.Ensure(rightChannelBuffer, perChannel);
                var left = leftChannelBuffer.AsSpan(0, perChannel);
                var right = rightChannelBuffer.AsSpan(0, perChannel);

                // Deinterleave
                for (int sample = 0, index = 0; sample < sampRead; sample += 2, index++)
                {
                    left[index] = buffer[sample];
                    right[index] = buffer[sample + 1];
                }
                shifterLeft.PitchShift(pitch, perChannel, fftSize, osamp, waveFormat.SampleRate, left);
                shifterRight.PitchShift(pitch, perChannel, fftSize, osamp, waveFormat.SampleRate, right);
                // Reinterleave with limiter
                for (int sample = 0, index = 0; sample < sampRead; sample += 2, index++)
                {
                    buffer[sample] = Limiter(left[index]);
                    buffer[sample + 1] = Limiter(right[index]);
                }
                return sampRead;
            }
            throw new InvalidOperationException("Shifting of more than 2 channels is currently not supported.");
        }

        /// <summary>
        /// WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Pitch Factor (0.5f = octave down, 1.0f = normal, 2.0f = octave up)
        /// </summary>
        public float PitchFactor
        {
            get { return pitch; }
            set { pitch = value; }
        }

        private float Limiter(float sample)
        {
            float res;
            if ((LIM_THRESH < sample))
            {
                res = (sample - LIM_THRESH)/LIM_RANGE;
                res = (float) ((Math.Atan(res)/M_PI_2)*LIM_RANGE + LIM_THRESH);
            }
            else if ((sample < -LIM_THRESH))
            {
                res = -(sample + LIM_THRESH)/LIM_RANGE;
                res = -(float) ((Math.Atan(res)/M_PI_2)*LIM_RANGE + LIM_THRESH);
            }
            else
            {
                res = sample;
            }
            return res;
        }
    }
}
