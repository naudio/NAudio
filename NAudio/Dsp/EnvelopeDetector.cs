// based on EnvelopeDetector.cpp v1.10 © 2006, ChunkWare Music Software, OPEN-SOURCE
using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Utils;

namespace NAudio.Dsp
{
    class EnvelopeDetector
    {
        private double sampleRate;
        private double ms;
        private double coeff;

        public EnvelopeDetector() : this(1.0, 44100.0)
        {
        }

        public EnvelopeDetector( double ms, double sampleRate )
        {
	        System.Diagnostics.Debug.Assert( sampleRate > 0.0 );
	        System.Diagnostics.Debug.Assert( ms > 0.0 );
	        this.sampleRate = sampleRate;
	        this.ms = ms;
	        setCoef();
        }

        public double TimeConstant
        {
            get 
            { 
                return ms; 
            }
            set 
            {
	            System.Diagnostics.Debug.Assert( value > 0.0 );
	            this.ms = value;
	            setCoef();
            }
        
        }


        public double SampleRate
        {
            get 
            {
                return sampleRate; 
            }
            set
            {
	            System.Diagnostics.Debug.Assert( value > 0.0 );
	            this.sampleRate = value;
	            setCoef();
            }
        }

        public void run( double inValue, ref double state )
        {
            state = inValue + coeff * (state - inValue);
	    }

        private void setCoef()
        {
            coeff = Math.Exp(-1.0 / (0.001 * ms * sampleRate));
        }
    }

    class AttRelEnvelope
    {
        // DC offset to prevent denormal
        protected const double DC_OFFSET = 1.0E-25;


        
        private EnvelopeDetector attack;
        private EnvelopeDetector release;

        public AttRelEnvelope( double att_ms, double rel_ms, double sampleRate )
        {
            attack = new EnvelopeDetector(att_ms,sampleRate);
            release = new EnvelopeDetector(rel_ms,sampleRate);
        }

        public double Attack 
        {
            get { return attack.TimeConstant; }
            set { attack.TimeConstant = value; }
        }

        public double Release
        {
            get { return release.TimeConstant; }
            set { release.TimeConstant = value; }
        }

        public double SampleRate
        {
            get { return attack.SampleRate; }
            set { attack.SampleRate = release.SampleRate = value; }
        }

        public void Run(double inValue, ref double state)
        {
            // assumes that:
		    // positive delta = attack
		    // negative delta = release
		    // good for linear & log values
		    if ( inValue > state )
			    attack.run( inValue, ref state );	// attack
		    else
			    release.run( inValue, ref state );	// release
        }
        
    }
}
