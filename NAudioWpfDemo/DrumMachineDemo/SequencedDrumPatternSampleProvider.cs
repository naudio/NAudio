using System;
using NAudio.Sequencing;
using NAudio.Wave;

namespace NAudioWpfDemo.DrumMachineDemo
{
    /// <summary>
    /// Drum-pattern playback built on the <c>NAudio.Sequencing</c> primitives. Functionally
    /// equivalent to <see cref="DrumPatternSampleProvider"/> but with the timing logic factored
    /// into a reusable core, and with extra knobs (swing) and an offline render path on top.
    /// </summary>
    class SequencedDrumPatternSampleProvider : ISampleProvider
    {
        private static readonly long Sixteenth = MusicalTime.TicksPerDivision(16);

        private readonly DrumKit drumKit;
        private readonly DrumPattern pattern;
        private readonly LiveTempoMap tempoMap;
        private readonly SwingTransform swing;
        private readonly SequencedSampleProvider<int> sequencer;

        public SequencedDrumPatternSampleProvider(DrumPattern pattern, DrumKit drumKit, int initialTempo)
        {
            this.pattern = pattern;
            this.drumKit = drumKit;

            tempoMap = new LiveTempoMap(initialTempo);
            var transport = new Transport(tempoMap, drumKit.WaveFormat.SampleRate)
            {
                Loop = new LoopRegion(0, pattern.Steps * Sixteenth),
            };

            var timeline = new EventTimeline<int>();
            BuildTimeline(timeline);

            swing = new SwingTransform(Sixteenth, 0.0);
            sequencer = new SequencedSampleProvider<int>(transport, timeline, drumKit.WaveFormat, Dispatch)
            {
                Transform = swing,
            };

            pattern.PatternChanged += (_, _) =>
            {
                timeline.Clear();
                BuildTimeline(timeline);
            };
        }

        public Transport Transport => sequencer.Transport;

        public int Tempo
        {
            get => (int)Math.Round(tempoMap.CurrentBpm);
            set => tempoMap.SetTempo(value, sequencer.Transport.CurrentTicks);
        }

        /// <summary>Swing amount as a fraction of a 16th: 0 = straight, 0.5 ≈ 32nd-note delay on the off-16ths.</summary>
        public double Swing
        {
            get => swing.Amount;
            set => swing.Amount = value;
        }

        public WaveFormat WaveFormat => sequencer.WaveFormat;

        public int Read(Span<float> buffer) => sequencer.Read(buffer);

        private void BuildTimeline(EventTimeline<int> timeline)
        {
            for (int step = 0; step < pattern.Steps; step++)
            {
                long tick = step * Sixteenth;
                for (int note = 0; note < pattern.Notes; note++)
                {
                    if (pattern[note, step] != 0) timeline.Add(tick, note);
                }
            }
        }

        private void Dispatch(SequencerEvent<int> ev, int frameOffset)
        {
            var voice = drumKit.GetSampleProvider(ev.Payload);
            voice.DelayBy = frameOffset * drumKit.WaveFormat.Channels;
            sequencer.Mixer.AddMixerInput(voice);
        }
    }
}
