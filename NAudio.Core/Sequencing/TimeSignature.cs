using System;

namespace NAudio.Sequencing
{
    /// <summary>
    /// A musical time signature, expressed as numerator over denominator
    /// (e.g. 4/4, 3/4, 6/8). The denominator is the actual note value (4 for quarter,
    /// 8 for eighth), not the MIDI-file power-of-two exponent.
    /// </summary>
    public readonly record struct TimeSignature(int Numerator, int Denominator)
    {
        /// <summary>Common time (4/4).</summary>
        public static TimeSignature FourFour => new(4, 4);

        /// <summary>
        /// The number of canonical ticks per beat in this time signature.
        /// For 4/4 this is <see cref="MusicalTime.CanonicalPpq"/>; for 6/8 it is half that.
        /// </summary>
        public long TicksPerBeat => MusicalTime.CanonicalPpq * 4L / Denominator;

        /// <summary>The number of canonical ticks per bar in this time signature.</summary>
        public long TicksPerBar => TicksPerBeat * Numerator;

        /// <summary>Throws if the time signature is structurally invalid.</summary>
        public void Validate()
        {
            if (Numerator <= 0) throw new ArgumentException("Numerator must be positive.", nameof(Numerator));
            if (Denominator <= 0) throw new ArgumentException("Denominator must be positive.", nameof(Denominator));
            if ((Denominator & (Denominator - 1)) != 0)
                throw new ArgumentException("Denominator must be a power of two.", nameof(Denominator));
        }
    }
}
