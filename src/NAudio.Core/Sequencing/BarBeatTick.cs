namespace NAudio.Sequencing
{
    /// <summary>
    /// A human-readable musical position. <see cref="Bar"/> and <see cref="Beat"/> are 1-based
    /// (matching how musicians count); <see cref="TickInBeat"/> is 0-based.
    /// </summary>
    /// <remarks>
    /// This is a display / addressing type only. Storage and arithmetic happen in raw canonical
    /// ticks (<see cref="MusicalTime.CanonicalPpq"/>). Convert via <see cref="TimeSignatureMap"/>.
    /// </remarks>
    public readonly record struct BarBeatTick(int Bar, int Beat, int TickInBeat)
    {
        /// <summary>Bar 1, beat 1, tick 0 — the start of a sequence.</summary>
        public static BarBeatTick Start => new(1, 1, 0);
    }
}
