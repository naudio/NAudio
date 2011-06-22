using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudio.Midi;
using NAudio.Wave.SampleProviders;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class DrumPatternSampleProvider : ISampleProvider
    {
        private MixingSampleProvider mixer;
        private WaveFormat waveFormat;
        private PatternSequencer sequencer;

        public DrumPatternSampleProvider(DrumPattern pattern)
        {
            var kit = new DrumKit();
            this.sequencer = new PatternSequencer(pattern, kit);
            this.waveFormat = kit.WaveFormat;
            mixer = new MixingSampleProvider(waveFormat);
        }

        public int Tempo
        {
            get
            {
                return sequencer.Tempo;
            }
            set
            {
                sequencer.Tempo = value;
            }
        }

        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        public int Read(float[] buffer, int offset, int count)
        {

            foreach (var mixerInput in sequencer.GetNextMixerInputs(count))
            {
                //mixerInput = new MonoToStereoSampleProvider(mixerInput);
                mixer.AddMixerInput(mixerInput);
            }

            // now we just need to read from the mixer
            var samplesRead = mixer.Read(buffer, offset, count);
            if (samplesRead < count)
            {
                Array.Clear(buffer, offset + samplesRead, count - samplesRead);
                samplesRead = count;
            }
            return samplesRead;
        }
    }
}
