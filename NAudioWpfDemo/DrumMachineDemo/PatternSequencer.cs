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
        private MixingSampleProvider mixer;
        private WaveFormat waveFormat;
        private List<SampleSource> sampleSources;
        private int samplesPerStep;
        private DrumPattern pattern;

        public PatternSequencer(DrumPattern pattern)
        {
            this.pattern = pattern;
            SampleSource kickSample = SampleSource.CreateFromWaveFile("Samples\\kick-trimmed.wav");
            SampleSource snareSample = SampleSource.CreateFromWaveFile("Samples\\snare-trimmed.wav");
            SampleSource closedHatsSample = SampleSource.CreateFromWaveFile("Samples\\closed-hat-trimmed.wav");
            SampleSource openHatsSample = SampleSource.CreateFromWaveFile("Samples\\open-hat-trimmed.wav");
            sampleSources = new List<SampleSource>();
            
            sampleSources.Add(kickSample);
            sampleSources.Add(snareSample);
            sampleSources.Add(closedHatsSample);
            sampleSources.Add(openHatsSample);

            int tempo = 100;
            int sampleRate = openHatsSample.SampleWaveFormat.SampleRate;
            int channels = 2;
            int samplesPerBeat = channels * (sampleRate * 60) / tempo;
            this.samplesPerStep = samplesPerBeat / 4;

            this.patternLength = samplesPerStep * pattern.Steps;
            this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
            mixer = new MixingSampleProvider(waveFormat);
        }

        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        private int GetPositionForStep(int step)
        {
            return step * samplesPerStep;
        }

        private int GetStepFromPosition(long position)
        {
            return (int)(position / samplesPerStep) % pattern.Steps;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int startStep = GetStepFromPosition(position);
            int endStep = GetStepFromPosition(position);
            
            for (int step = startStep; step <= endStep; step++)
            {
                for (int note = 0; note < pattern.Notes; note++)
                {
                    byte velocity = pattern[note, step];
                    if (velocity > 0)
                    {
                        MusicSampleProvider sp = new MusicSampleProvider(sampleSources[note]);
                        sp.DelayBy = (int)(GetPositionForStep(step) - position);
                        ISampleProvider mixerInput = sp;
                        if (mixerInput.WaveFormat.Channels == 1)
                        {
                            mixerInput = new MonoToStereoSampleProvider(mixerInput);
                        }
                        mixer.AddMixerInput(mixerInput);
                    }
                }
            }

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
    }
}
