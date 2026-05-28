namespace NAudio.Sequencing
{
    /// <summary>
    /// Maps musical position (in canonical ticks) to and from real time (in seconds).
    /// Implementations may be immutable (a static curve built from a MIDI file) or mutable
    /// (a live tempo knob that changes the future from now forward).
    /// </summary>
    public interface ITempoMap
    {
        /// <summary>The real time in seconds at which the given tick falls.</summary>
        double SecondsFromTicks(long ticks);

        /// <summary>The tick at which the given real time falls.</summary>
        long TicksFromSeconds(double seconds);

        /// <summary>The tempo in BPM in force at the given tick.</summary>
        double BpmAtTicks(long ticks);

        /// <summary>
        /// The tick at which the next tempo change occurs after <paramref name="tick"/>, or null
        /// if no further change is recorded (for a live map, no further change has been scheduled yet).
        /// </summary>
        /// <remarks>
        /// Consumers that need a single tempo per process block (e.g. a VST3 host populating
        /// <c>ProcessContext.tempo</c>) use this to decide whether to split a block at the
        /// tempo-change boundary or to use the start-of-block tempo and accept the inaccuracy.
        /// </remarks>
        long? NextChangeAfter(long tick);
    }
}
