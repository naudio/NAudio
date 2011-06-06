using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudio.Midi;
using NAudio.Wave.SampleProviders;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class PatternSequencer : ISampleProvider
    {
        private long position;
        private long patternLength;
        private List<NoteOnEvent> drumBeats;
        private MixingSampleProvider mixer;
        private WaveFormat waveFormat;
        private int patternIndex = 0;
        private Dictionary<int, SampleSource> sampleSources;

        private const int KickDrumNote = 36;
        private const int SnareDrumNote = 38;
        private const int ClosedHatsNote = 42;
        private const int OpenHatsNote = 46;

        public PatternSequencer(DrumPattern pattern)
        {
            SampleSource kickSample = SampleSource.CreateFromWaveFile("Samples\\kick-trimmed.wav");
            SampleSource snareSample = SampleSource.CreateFromWaveFile("Samples\\snare-trimmed.wav");
            SampleSource closedHatsSample = SampleSource.CreateFromWaveFile("Samples\\closed-hat-trimmed.wav");
            SampleSource openHatsSample = SampleSource.CreateFromWaveFile("Samples\\open-hat-trimmed.wav");
            sampleSources = new Dictionary<int, SampleSource>();
            
            sampleSources.Add(KickDrumNote, kickSample);
            sampleSources.Add(SnareDrumNote, snareSample);
            sampleSources.Add(ClosedHatsNote, closedHatsSample);
            sampleSources.Add(OpenHatsNote, openHatsSample);

            int tempo = 100;
            int sampleRate = openHatsSample.SampleWaveFormat.SampleRate;
            int channels = 2;
            int samplesPerBeat = channels * (sampleRate * 60) / tempo;
            int samplesPer16th = samplesPerBeat / 4;

            // we'll use MIDI events since they are convenient
            // for now, channel, note number, velocity mean nothing
            // absolute time is measured in sample frames
            CreateDrumBeats(pattern, samplesPer16th);
            pattern.PatternChanged += (s, e) => CreateDrumBeats(pattern, samplesPer16th);
            //drumBeats.Sort((x, y) => x.AbsoluteTime.CompareTo(y.AbsoluteTime));

            this.patternLength = samplesPer16th * pattern.Steps;
            this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            mixer = new MixingSampleProvider(waveFormat);
        }

        private void CreateDrumBeats(DrumPattern pattern, int samplesPer16th)
        {
            int channel = 1;
            drumBeats = new List<NoteOnEvent>();
            int[] midiNoteNumbers = { KickDrumNote, SnareDrumNote, ClosedHatsNote, OpenHatsNote };
            for (int step = 0; step < pattern.Steps; step++)
            {
                for (int note = 0; note < pattern.Notes; note++)
                {
                    var velocity = pattern[note, step];
                    if (velocity > 0)
                    {
                        var noteOn = new NoteOnEvent(samplesPer16th * step, channel, midiNoteNumbers[note], velocity, 0);
                        drumBeats.Add(noteOn);
                    }
                }
            }
            // sync back to the right place in the pattern
            patternIndex = 0;
            for (int i = 0; i < drumBeats.Count; i++)
            {
                if (drumBeats[i].AbsoluteTime >= position)
                {
                    patternIndex = i;
                    break;
                }
            }
        }

        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            bool finished = false;
            int initialIndex = patternIndex;
            do
            {
                var note = NextEvent;

                if (note != null && note.AbsoluteTime >= position && note.AbsoluteTime < position + count)
                {
                    MusicSampleProvider sp = new MusicSampleProvider(sampleSources[note.NoteNumber]);
                    sp.DelayBy = (int)(note.AbsoluteTime - position);
                    ISampleProvider mixerInput = sp;
                    if (mixerInput.WaveFormat.Channels == 1)
                    {
                        mixerInput = new MonoToStereoSampleProvider(mixerInput);
                    }
                    mixer.AddMixerInput(mixerInput);
                    patternIndex++;
                    patternIndex = patternIndex % drumBeats.Count;
                    if (patternIndex == initialIndex)
                    {
                        finished = true;
                    }                    
                }
                else
                {
                    finished = true;
                }
            } while (!finished);

            // now we just need to read from the mixer
            var samplesRead = mixer.Read(buffer, offset, count);
            if (samplesRead < count)
            {
                Array.Clear(buffer, offset + samplesRead, count - samplesRead);
                samplesRead = count;
            }
            position += samplesRead;
            position = position % patternLength; // loop indefinitely
            return samplesRead;
        }

        private void MoveToNextNote()
        {
            patternIndex++;
            patternIndex = patternIndex % drumBeats.Count;
        }

        private NoteOnEvent NextEvent
        {
            get
            {
                if (drumBeats.Count == 0) return null;
                return drumBeats[patternIndex];
            }
        }
    }
}
