// based on SimpleGate v1.10 © 2006, ChunkWare Music Software, OPEN-SOURCE
using System;
using NAudio.Utils;

namespace NAudio.Dsp
{
    class SimpleGate : AttRelEnvelope
    {
        // transfer function
        private double threshdB;	// threshold (dB)
        private double thresh;		// threshold (linear)
        
        // runtime variables
        private double env;		// over-threshold envelope (linear)

        public SimpleGate()
            : base(10.0, 10.0, 44100.0)
        {
            threshdB = 0.0;
            thresh = 1.0;
            env = DC_OFFSET;
        }

        public void Process( ref double in1, ref double in2 )
        {
            // in/out pointers are assummed to reference stereo data

            // sidechain

            // rectify input
            double rect1 = Math.Abs( in1 );	// n.b. was fabs
            double rect2 = Math.Abs( in2 ); // n.b. was fabs

            // if desired, one could use another EnvelopeDetector to smooth
            // the rectified signal.

            double key = Math.Max( rect1, rect2 );	// link channels with greater of 2

            // threshold
            double over = ( key > thresh ) ? 1.0 : 0.0;	// key over threshold ( 0.0 or 1.0 )

            // attack/release
            over += DC_OFFSET;				// add DC offset to avoid denormal

            env = Run(over, env);	// run attack/release

            over = env - DC_OFFSET;		// subtract DC offset

            // Regarding the DC offset: In this case, since the offset is added before 
            // the attack/release processes, the envelope will never fall below the offset,
            // thereby avoiding denormals. However, to prevent the offset from causing
            // constant gain reduction, we must subtract it from the envelope, yielding
            // a minimum value of 0dB.

            // output gain
            in1 *= over;	// apply gain reduction to input
            in2 *= over;
        }

        public double Threshold 
        {
            get => threshdB;
            set 
            { 
                threshdB = value;
                thresh = Decibels.DecibelsToLinear(value);
            }
        }
    }
}
