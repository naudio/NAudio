// based on EnvelopeDetector.cpp v1.10 © 2006, ChunkWare Music Software, OPEN-SOURCE
using System;

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
            SetCoef();
        }

        public double TimeConstant
        {
            get => ms;
            set 
            {
                System.Diagnostics.Debug.Assert( value > 0.0 );
                this.ms = value;
                SetCoef();
            }
        }

        public double SampleRate
        {
            get => sampleRate;
            set
            {
                System.Diagnostics.Debug.Assert( value > 0.0 );
                this.sampleRate = value;
                SetCoef();
            }
        }

        public double Run( double inValue, double state )
        {
            return inValue + coeff * (state - inValue);
        }

        private void SetCoef()
        {
            coeff = Math.Exp(-1.0 / (0.001 * ms * sampleRate));
        }
    }

    class AttRelEnvelope
    {
        // DC offset to prevent denormal
        protected const double DC_OFFSET = 1.0E-25;
        
        private readonly EnvelopeDetector attack;
        private readonly EnvelopeDetector release;

        public AttRelEnvelope( double attackMilliseconds, double releaseMilliseconds, double sampleRate )
        {
            attack = new EnvelopeDetector(attackMilliseconds,sampleRate);
            release = new EnvelopeDetector(releaseMilliseconds,sampleRate);
        }

        public double Attack 
        {
            get => attack.TimeConstant;
            set => attack.TimeConstant = value;
        }

        public double Release
        {
            get => release.TimeConstant;
            set => release.TimeConstant = value;
        }

        public double SampleRate
        {
            get => attack.SampleRate;
            set => attack.SampleRate = release.SampleRate = value;
        }

        public double Run(double inValue, double state)
        {
            // assumes that:
            // positive delta = attack
            // negative delta = release
            // good for linear & log values
            return inValue > state ? attack.Run( inValue, state ) : release.Run( inValue, state );
        }
    }
}
