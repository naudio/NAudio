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
            string folder = @"c:\Users\Mark\recording\";
            SampleSource kickSample = SampleSource.CreateFromWaveFile(folder + @"sfz\SL Acoustic Kit Sample Set\AcousticKit\Kicks\Kick 01.wav");
            SampleSource snareSample = SampleSource.CreateFromWaveFile(folder + @"sfz\SL Acoustic Kit Sample Set\AcousticKit\Snares\Snare 01.wav");
            SampleSource closedHatsSample = SampleSource.CreateFromWaveFile(folder + @"sfz\SL Acoustic Kit Sample Set\AcousticKit\Hi Hat Cymbals\Hi Hat Closed Edge 01.wav");
            SampleSource openHatsSample = SampleSource.CreateFromWaveFile(folder + @"sfz\SL Acoustic Kit Sample Set\AcousticKit\Hi Hat Cymbals\Hi Hat Open 01a.wav");
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
            int channel = 1;
            drumBeats = new List<NoteOnEvent>();
            int[] midiNoteNumbers = { KickDrumNote, SnareDrumNote, ClosedHatsNote, OpenHatsNote };
            for (int step = 0; step < pattern.Steps; step++)
            {
                for (int note = 0; note < pattern.Notes; note++)
                {
                    var velocity = pattern[note,step];
                    if (velocity > 0)
                    {
                        var noteOn = new NoteOnEvent(samplesPer16th * step, channel, midiNoteNumbers[note], velocity, 0);
                        drumBeats.Add(noteOn);
                    }
                }
            }
            //drumBeats.Sort((x, y) => x.AbsoluteTime.CompareTo(y.AbsoluteTime));

            this.patternLength = samplesPer16th * pattern.Steps;
            this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            mixer = new MixingSampleProvider(waveFormat);
        }

        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            bool finished = false;
            do
            {
                var note = GetNextEvent();

                if (note.AbsoluteTime >= position && note.AbsoluteTime < position + count)
                {
                    MusicSampleProvider sp = new MusicSampleProvider(sampleSources[note.NoteNumber]);
                    sp.DelayBy = (int)(note.AbsoluteTime - position);
                    ISampleProvider mixerInput = sp;
                    if (mixerInput.WaveFormat.Channels == 1)
                    {
                        mixerInput = new MonoToStereoSampleProvider(mixerInput);
                    }
                    mixer.AddMixerInput(mixerInput);
                    MoveToNextNote();
                }
                else
                {
                    finished = true;
                }
            } while (!finished);

            // now we just need to read from the mixer
            var samplesRead = mixer.Read(buffer, offset, count);
            position += samplesRead;
            position = position % patternLength; // loop indefinitely
            return samplesRead;
        }

        private void MoveToNextNote()
        {
            patternIndex++;
            patternIndex = patternIndex % drumBeats.Count;
        }

        private NoteOnEvent GetNextEvent()
        {
            return drumBeats[patternIndex];
        }
    }
}
