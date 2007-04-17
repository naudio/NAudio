// based on SimpleComp v1.10 © 2006, ChunkWare Music Software, OPEN-SOURCE
using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Utils;

namespace NAudio.Dsp
{
    class SimpleCompressor : AttRelEnvelope
    {
        // transfer function
	    private double threshdB;		// threshold (dB)
	    private double ratio;			// ratio (compression: < 1 ; expansion: > 1)
        private double makeUpGain;

	    // runtime variables
	    private double envdB;			// over-threshold envelope (dB)

        public SimpleCompressor(double attackTime, double releaseTime, double sampleRate)
            : base(attackTime, releaseTime, sampleRate)
        {
            this.threshdB = 0.0;
            this.ratio = 1.0;
            this.makeUpGain = 0.0;
            this.envdB = DC_OFFSET;
        }

        public SimpleCompressor()
            : base(10.0, 10.0, 44100.0)
        {
            this.threshdB = 0.0;
            this.ratio = 1.0;
            this.makeUpGain = 0.0;
            this.envdB = DC_OFFSET;
        }

        public double MakeUpGain
        {
            get { return makeUpGain; }
            set { makeUpGain = value; }
        }

        public double Threshold 
        {
            get { return threshdB; }
            set { threshdB = value; }
        }
            
        public double Ratio
        {
            get { return ratio; }
            set { ratio = value; }
        }

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
	        double rect1 = Math.Abs( in1 );	// n.b. was fabs
	        double rect2 = Math.Abs( in2 ); // n.b. was fabs

	        // if desired, one could use another EnvelopeDetector to smooth
	        // the rectified signal.

        	double link = Math.Max( rect1, rect2 );	// link channels with greater of 2

	        link += DC_OFFSET;					// add DC offset to avoid log( 0 )
	        double keydB = Decibels.LinearToDecibels( link );		// convert linear -> dB

	        // threshold
	        double overdB = keydB - threshdB;	// delta over threshold
	        if ( overdB < 0.0 )
		        overdB = 0.0;

	        // attack/release

	        overdB += DC_OFFSET;					// add DC offset to avoid denormal

	        Run( overdB, ref envdB );	// run attack/release envelope

	        overdB = envdB - DC_OFFSET;			// subtract DC offset

	        // Regarding the DC offset: In this case, since the offset is added before 
	        // the attack/release processes, the envelope will never fall below the offset,
	        // thereby avoiding denormals. However, to prevent the offset from causing
	        // constant gain reduction, we must subtract it from the envelope, yielding
	        // a minimum value of 0dB.
    
	        // transfer function
            double gr = overdB * (ratio - 1.0);	// gain reduction (dB)
            gr = Decibels.DecibelsToLinear(gr) * Decibels.DecibelsToLinear(makeUpGain); // convert dB -> linear

	        // output gain
	        in1 *= gr;	// apply gain reduction to input
	        in2 *= gr;
        }
    }
}

        


