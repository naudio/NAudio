namespace NAudio.Sequencing
{
    /// <summary>
    /// Constants and helpers for the canonical tick resolution used by the sequencing primitives.
    /// All ticks in <see cref="ITempoMap"/>, <see cref="EventTimeline{T}"/>, <see cref="TimeSignatureMap"/>,
    /// and <see cref="Transport"/> are at <see cref="CanonicalPpq"/>, regardless of the PPQ in
    /// any source file (e.g. a MIDI file). External PPQs must be rescaled at the ingestion boundary.
    /// </summary>
    public static class MusicalTime
    {
        /// <summary>
        /// The canonical pulses-per-quarter-note used by the sequencing layer. 960 divides cleanly
        /// into common subdivisions (8ths, 16ths, 32nds, triplets) and matches the default of most DAWs.
        /// </summary>
        public const int CanonicalPpq = 960;

        /// <summary>
        /// Returns the number of canonical ticks per note of the given musical division
        /// (4 = quarter, 8 = eighth, 16 = sixteenth, etc).
        /// </summary>
        /// <param name="division">A power-of-two note division (1 = whole, 4 = quarter, 16 = sixteenth).</param>
        public static long TicksPerDivision(int division)
        {
            if (division <= 0) throw new System.ArgumentOutOfRangeException(nameof(division), "Division must be positive.");
            return CanonicalPpq * 4L / division;
        }

        /// <summary>Ticks per quarter-note triplet at the canonical PPQ.</summary>
        public static long QuarterTripletTicks => CanonicalPpq * 2L / 3L;

        /// <summary>
        /// Converts a tick value from an external PPQ (e.g. a MIDI file's <c>DeltaTicksPerQuarterNote</c>)
        /// to the canonical PPQ used by the sequencing layer. Apply at the ingestion boundary to event
        /// absolute times, tempo events, and time-signature events alike.
        /// </summary>
        public static long RescaleFromPpq(long fileTick, int fileDeltaTicksPerQuarterNote)
        {
            if (fileDeltaTicksPerQuarterNote <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(fileDeltaTicksPerQuarterNote), "PPQ must be positive.");
            if (fileTick < 0)
                throw new System.ArgumentOutOfRangeException(nameof(fileTick), "Ticks must not be negative.");
            return fileTick * CanonicalPpq / fileDeltaTicksPerQuarterNote;
        }
    }
}
