using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NAudio.Wave.SampleProviders;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class PatternSequencer
    {
        private readonly DrumPattern drumPattern;
        private readonly DrumKit drumKit;
        private int tempo;
        private int samplesPerStep;

        public PatternSequencer(DrumPattern drumPattern, DrumKit kit)
        {
            this.drumKit = kit;
            this.drumPattern = drumPattern;
            this.Tempo = 120;
        }

        public int Tempo
        {
            get
            {
                return this.tempo;
            }
            set
            {
                this.tempo = value;
                int samplesPerBeat = (this.drumKit.WaveFormat.Channels * this.drumKit.WaveFormat.SampleRate * 60) / tempo;
                this.samplesPerStep = samplesPerBeat / 4;
            }
        }

        private int currentStep = 0;
        private double patternPosition = 0;

        public IList<MusicSampleProvider> GetNextMixerInputs(int sampleCount)
        {
            List<MusicSampleProvider> mixerInputs = new List<MusicSampleProvider>();
            int samplePos = 0;           
            while (samplePos < sampleCount)
            {
                for (int note = 0; note < drumPattern.Notes; note++)
                {
                    if (drumPattern[note, currentStep] != 0)
                    {
                        var sampleProvider = drumKit.GetSampleProvider(note);
                        Debug.WriteLine("beat at step {0}, patternPostion={1}", currentStep, patternPosition);
                        double offsetFromCurrent = (currentStep - patternPosition);
                        if (offsetFromCurrent < 0) offsetFromCurrent += drumPattern.Steps;
                        sampleProvider.DelayBy = (int)(this.samplesPerStep * offsetFromCurrent);
                        mixerInputs.Add(sampleProvider);
                    }
                }

                samplePos += samplesPerStep;
                currentStep++;
                currentStep = currentStep % drumPattern.Steps;
            }
            this.patternPosition += ((double)sampleCount / samplesPerStep);
            if (this.patternPosition > drumPattern.Steps)
            {
                this.patternPosition -= drumPattern.Steps;
            }
            return mixerInputs;
        }
    }
}
