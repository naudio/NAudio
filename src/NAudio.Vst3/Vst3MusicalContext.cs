namespace NAudio.Vst3;

/// <summary>
/// A per-block snapshot of musical/transport state for a hosted instrument's <c>ProcessContext</c> —
/// tempo, time signature, musical position and whether the transport is playing. Pushed to a
/// <see cref="Vst3Plugin"/> via <see cref="Vst3Plugin.SetMusicalContext"/> before each
/// <see cref="Vst3Plugin.Process"/> call so tempo-following plug-ins (arpeggiators, tempo-synced
/// delays, LFOs) lock to the host timeline. <see cref="Vst3MidiInstrument"/> builds one of these from
/// the sequencer's <c>Transport</c> / <c>ITempoMap</c> / <c>TimeSignatureMap</c>; advanced callers
/// driving <see cref="Vst3Plugin"/> directly can populate it themselves.
/// </summary>
/// <param name="IsPlaying">Whether the transport is running (sets the VST 3 <c>kPlaying</c> state flag).</param>
/// <param name="ProjectTimeSamples">Playhead position in samples (project + continuous time).</param>
/// <param name="ProjectTimeMusic">Playhead position in quarter notes.</param>
/// <param name="BarPositionMusic">The current bar's start position in quarter notes.</param>
/// <param name="Tempo">Tempo in BPM at the playhead.</param>
/// <param name="TimeSigNumerator">Time-signature numerator (beats per bar).</param>
/// <param name="TimeSigDenominator">Time-signature denominator (note value of a beat).</param>
public readonly record struct Vst3MusicalContext(
    bool IsPlaying,
    long ProjectTimeSamples,
    double ProjectTimeMusic,
    double BarPositionMusic,
    double Tempo,
    int TimeSigNumerator,
    int TimeSigDenominator);
