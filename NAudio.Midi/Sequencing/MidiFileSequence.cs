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
    /// Note/CC/pitch-bend/aftertouch/program events are kept; meta and sysex
    /// events are dropped except <c>SetTempo</c>, which builds the tempo map.
    /// </summary>
    public sealed class MidiFileSequence
    {
        private MidiFileSequence(EventTimeline<MidiEvent> timeline, ITempoMap tempoMap, long endTick)
        {
            Timeline = timeline;
            TempoMap = tempoMap;
            EndTick = endTick;
        }

        /// <summary>The channel events at canonical PPQ.</summary>
        public EventTimeline<MidiEvent> Timeline { get; }

        /// <summary>The tempo map built from the file's <c>SetTempo</c> events.</summary>
        public ITempoMap TempoMap { get; }

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

                    if (IsChannelEvent(midiEvent))
                    {
                        timeline.Add(tick, midiEvent);
                        if (tick > endTick) endTick = tick;
                    }
                }
            }

            return new MidiFileSequence(timeline, BuildTempoMap(tempos), endTick);
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

        private static bool IsChannelEvent(MidiEvent midiEvent) =>
            midiEvent is NoteEvent ||                 // note on/off (NoteOnEvent derives from NoteEvent)
            midiEvent is ControlChangeEvent ||
            midiEvent is PitchWheelChangeEvent ||
            midiEvent is PatchChangeEvent ||
            midiEvent is ChannelAfterTouchEvent;

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
    }
}
