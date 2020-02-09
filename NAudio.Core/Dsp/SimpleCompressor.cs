// based on SimpleComp v1.10 © 2006, ChunkWare Music Software, OPEN-SOURCE
using System;
using NAudio.Utils;

namespace NAudio.Dsp
{
    class SimpleCompressor : AttRelEnvelope
    {
        // runtime variables
        private double envdB; // over-threshold envelope (dB)

        public SimpleCompressor(double attackTime, double releaseTime, double sampleRate)
            : base(attackTime, releaseTime, sampleRate)
        {
            this.Threshold = 0.0;
            this.Ratio = 1.0;
            this.MakeUpGain = 0.0;
            this.envdB = DC_OFFSET;
        }

        public SimpleCompressor()
            : this(10.0, 10.0, 44100.0)
        {
        }

        public double MakeUpGain { get; set; }

        public double Threshold { get; set; }

        public double Ratio { get; set; }

        // call before runtime (in resume())
        public void InitRuntime()
        {
            this.envdB = DC_OFFSET;
        }
    
        // // compressor runtime process
        public void Process(ref double in1, ref double in2)
        {
            // sidechain

            // rectify input
            double rect1 = Math.Abs(in1); // n.b. was fabs
            double rect2 = Math.Abs(in2); // n.b. was fabs

            // if desired, one could use another EnvelopeDetector to smooth
            // the rectified signal.

            double link = Math.Max( rect1, rect2 );	// link channels with greater of 2

            link += DC_OFFSET; // add DC offset to avoid log( 0 )
            double keydB = Decibels.LinearToDecibels(link); // convert linear -> dB

            // threshold
            double overdB = keydB - Threshold; // delta over threshold
            if (overdB < 0.0)
                overdB = 0.0;

            // attack/release

            overdB += DC_OFFSET; // add DC offset to avoid denormal

            envdB = Run(overdB, envdB); // run attack/release envelope

            overdB = envdB - DC_OFFSET; // subtract DC offset

            // Regarding the DC offset: In this case, since the offset is added before 
            // the attack/release processes, the envelope will never fall below the offset,
            // thereby avoiding denormals. However, to prevent the offset from causing
            // constant gain reduction, we must subtract it from the envelope, yielding
            // a minimum value of 0dB.
    
            // transfer function
            double gr = overdB * (Ratio - 1.0);	// gain reduction (dB)
            gr = Decibels.DecibelsToLinear(gr) * Decibels.DecibelsToLinear(MakeUpGain); // convert dB -> linear

            // output gain
            in1 *= gr;	// apply gain reduction to input
            in2 *= gr;
        }
    }
}
