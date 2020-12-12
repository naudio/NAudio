using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class DrumPatternSampleProvider : ISampleProvider
    {
        private readonly MixingSampleProvider mixer;
        private readonly WaveFormat waveFormat;
        private readonly PatternSequencer sequencer;

        public DrumPatternSampleProvider(DrumPattern pattern)
        {
            var kit = new DrumKit();
            sequencer = new PatternSequencer(pattern, kit);
            waveFormat = kit.WaveFormat;
            mixer = new MixingSampleProvider(waveFormat);
        }

        public int Tempo
        {
            get => sequencer.Tempo;
            set => sequencer.Tempo = value;
        }

        public WaveFormat WaveFormat => waveFormat;

        public int Read(Span<float> buffer)
        {
            var count = buffer.Length;
            foreach (var mixerInput in sequencer.GetNextMixerInputs(count))
            {
                mixer.AddMixerInput(mixerInput);
            }

            // now we just need to read from the mixer
            var samplesRead = mixer.Read(buffer);
            while (samplesRead < count)
            {
                buffer[samplesRead++] = 0;
            }
            return samplesRead;
        }
    }
}
