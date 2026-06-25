namespace NAudio.Sequencing;

/// <summary>
/// An event placed at a musical position (in canonical ticks) with a consumer-defined payload.
/// </summary>
/// <typeparam name="T">Payload type. The sequencer core never inspects the payload — the consuming
/// sink (audio mixer, MIDI out, VST3 host, sampler voice allocator) interprets it.</typeparam>
public readonly record struct SequencerEvent<T>(long Tick, T Payload);
