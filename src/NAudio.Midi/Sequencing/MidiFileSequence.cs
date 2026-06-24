using System;
using System.Collections.Generic;
using NAudio.Sequencing;

namespace NAudio.Midi
{
    /// <summary>
    /// A MIDI file loaded into the sequencing core: its channel events placed on
    /// an <see cref="EventTimeline{T}"/> at the canonical PPQ
    /// (<see cref="MusicalTime.CanonicalPpq"/>), and its tempo changes built into
    /// an <see cref="ITempoMap"/>. Feed it through a <see cref="SequencedMidiPlayer"/>
    /// (or <see cref="OfflineMidiRenderer"/>) to play it on any
    /// <see cref="IMidiInstrument"/> — the NAudio.Sampler engine, a synthesiser,
    /// a hosted VST instrument.
    ///
    /// Note/CC/pitch-bend/aftertouch/program and sysex events are kept; other
    /// meta events are dropped except <c>SetTempo</c>, which builds the tempo map.
    /// </summary>
    public sealed class MidiFileSequence
    {
        private MidiFileSequence(EventTimeline<MidiEvent> timeline, ITempoMap tempoMap,
            TimeSignatureMap timeSignatureMap, long endTick)
        {
            Timeline = timeline;
            TempoMap = tempoMap;
            TimeSignatureMap = timeSignatureMap;
            EndTick = endTick;
        }

        /// <summary>The channel events at canonical PPQ.</summary>
        public EventTimeline<MidiEvent> Timeline { get; }

        /// <summary>The tempo map built from the file's <c>SetTempo</c> events.</summary>
        public ITempoMap TempoMap { get; }

        /// <summary>
        /// The time-signature map built from the file's time-signature meta events (4/4 from tick 0
        /// when the file specifies none). Always non-null.
        /// </summary>
        public TimeSignatureMap TimeSignatureMap { get; }

        /// <summary>The canonical tick of the last event (0 if empty).</summary>
        public long EndTick { get; }

        /// <summary>Loads and parses a MIDI file.</summary>
        public static MidiFileSequence FromFile(string path) =>
            FromMidiFile(new MidiFile(path, false));

        /// <summary>Builds a sequence from an already-parsed <see cref="MidiFile"/>.</summary>
        public static MidiFileSequence FromMidiFile(MidiFile midiFile)
        {
            if (midiFile == null) throw new ArgumentNullException(nameof(midiFile));
            int ppq = midiFile.DeltaTicksPerQuarterNote;
            if (ppq <= 0) throw new ArgumentException("MIDI file has an invalid PPQ.", nameof(midiFile));

            var timeline = new EventTimeline<MidiEvent>();
            var tempos = new SortedDictionary<long, double>();
            var timeSignatures = new SortedDictionary<long, TimeSignature>();
            long endTick = 0;

            foreach (var track in midiFile.Events)
            {
                foreach (var midiEvent in track)
                {
                    long tick = MusicalTime.RescaleFromPpq(midiEvent.AbsoluteTime, ppq);

                    if (midiEvent is TempoEvent tempo)
                    {
                        tempos[tick] = tempo.Tempo; // last tempo at a given tick wins
                        continue;
                    }

                    if (midiEvent is TimeSignatureEvent timeSig)
                    {
                        // NAudio stores the denominator as a power-of-two exponent (3 = 8); the
                        // sequencing TimeSignature wants the actual note value. Last change at a tick wins.
                        timeSignatures[tick] = new TimeSignature(timeSig.Numerator, 1 << timeSig.Denominator);
                        continue;
                    }

                    if (IsPlayableEvent(midiEvent))
                    {
                        timeline.Add(tick, midiEvent);
                        if (tick > endTick) endTick = tick;
                    }
                }
            }

            return new MidiFileSequence(
                timeline, BuildTempoMap(tempos), BuildTimeSignatureMap(timeSignatures), endTick);
        }

        /// <summary>
        /// The number of audio frames needed to render the whole sequence plus a
        /// release tail, at the given sample rate.
        /// </summary>
        public long DurationFrames(int sampleRate, double tailSeconds)
        {
            double seconds = TempoMap.SecondsFromTicks(EndTick) + Math.Max(0.0, tailSeconds);
            return (long)Math.Ceiling(seconds * sampleRate);
        }

        // Events we forward to the instrument: per-channel performance events plus sysex. Meta events
        // (other than SetTempo, handled above) stay out of the timeline.
        private static bool IsPlayableEvent(MidiEvent midiEvent) =>
            midiEvent is NoteEvent ||                 // note on/off (NoteOnEvent derives from NoteEvent)
            midiEvent is ControlChangeEvent ||
            midiEvent is PitchWheelChangeEvent ||
            midiEvent is PatchChangeEvent ||
            midiEvent is ChannelAfterTouchEvent ||
            midiEvent is SysexEvent;

        private static ITempoMap BuildTempoMap(SortedDictionary<long, double> tempos)
        {
            // StaticTempoMap requires a strictly-increasing series starting at tick 0
            if (!tempos.ContainsKey(0))
                tempos[0] = tempos.Count > 0 ? FirstValue(tempos) : 120.0;

            var entries = new List<(long, double)>(tempos.Count);
            foreach (var pair in tempos) entries.Add((pair.Key, pair.Value));
            return new StaticTempoMap(entries);
        }

        private static double FirstValue(SortedDictionary<long, double> tempos)
        {
            foreach (var pair in tempos) return pair.Value; // earliest tempo, used as the pre-roll tempo
            return 120.0;
        }

        private static TimeSignatureMap BuildTimeSignatureMap(SortedDictionary<long, TimeSignature> signatures)
        {
            // The map must start at tick 0; MIDI assumes 4/4 until told otherwise, so backfill a 4/4
            // intro when the first change lands later (or there are none at all).
            if (!signatures.ContainsKey(0))
                signatures[0] = TimeSignature.FourFour;

            var entries = new List<(long, TimeSignature)>(signatures.Count);
            foreach (var pair in signatures) entries.Add((pair.Key, pair.Value));

            try
            {
                return new TimeSignatureMap(entries);
            }
            catch (ArgumentException)
            {
                // A malformed file can place a change off a bar boundary (which TimeSignatureMap rejects)
                // or carry an invalid signature. Fall back to the initial signature for the whole file
                // rather than failing the load.
                return new TimeSignatureMap(entries[0].Item2);
            }
        }
    }
}
