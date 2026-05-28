using System;
using System.Collections.Generic;
using NAudio.Sequencing;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudioWpfDemo.DrumMachineDemo
{
    /// <summary>
    /// Drum-pattern playback built on the <c>NAudio.Sequencing</c> primitives. Exposes tempo and
    /// swing knobs, supports a hi-hat choke group, and serves as both the live-playback source
    /// and the offline render source (driven by either <c>WaveOut</c> or a render loop).
    /// </summary>
    /// <remarks>
    /// Choked voices are faded out via <see cref="FadeInOutSampleProvider"/> but remain in the mixer
    /// (emitting silence) until their underlying sample naturally ends. With short kit samples this
    /// is fine; a proper voice manager that drops faded voices immediately will land alongside the
    /// future SoundFont/sfz sampler.
    /// </remarks>
    class DrumPatternSampleProvider : ISampleProvider
    {
        private static readonly long Sixteenth = MusicalTime.TicksPerDivision(16);
        private const double ChokeFadeMs = 10.0;

        // Notes in the same group cut each other. Group 1 is the hi-hat group: any closed-hat
        // trigger chokes any ringing open-hat (and vice versa), mirroring the physical pedal action.
        private static readonly Dictionary<int, int> ChokeGroups = new()
        {
            { 2, 1 }, // closed hat
            { 3, 1 }, // open hat
        };

        private readonly DrumKit drumKit;
        private readonly DrumPattern pattern;
        private readonly LiveTempoMap tempoMap;
        private readonly SwingTransform swing;
        private readonly SequencedSampleProvider<int> sequencer;
        private readonly Dictionary<int, FadeInOutSampleProvider> activeChokeVoices = new();

        public DrumPatternSampleProvider(DrumPattern pattern, DrumKit drumKit, int initialTempo)
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

        // Runs on the audio thread; single-threaded access to activeChokeVoices is safe here.
        private void Dispatch(SequencerEvent<int> ev, int frameOffset)
        {
            var voice = drumKit.GetSampleProvider(ev.Payload);
            voice.DelayBy = frameOffset * drumKit.WaveFormat.Channels;

            if (ChokeGroups.TryGetValue(ev.Payload, out var group))
            {
                if (activeChokeVoices.TryGetValue(group, out var previous))
                {
                    previous.BeginFadeOut(ChokeFadeMs);
                }
                var chokeable = new FadeInOutSampleProvider(voice);
                activeChokeVoices[group] = chokeable;
                sequencer.Mixer.AddMixerInput(chokeable);
            }
            else
            {
                sequencer.Mixer.AddMixerInput(voice);
            }
        }
    }
}
