using System.Collections.Generic;
using NAudio.Wave;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class DrumKit
    {
        private readonly List<SampleSource> sampleSources;

        public DrumKit()
        {
            var kickSample = SampleSource.CreateFromWaveFile("Samples\\kick-trimmed.wav");
            var snareSample = SampleSource.CreateFromWaveFile("Samples\\snare-trimmed.wav");
            var closedHatsSample = SampleSource.CreateFromWaveFile("Samples\\closed-hat-trimmed.wav");
            var openHatsSample = SampleSource.CreateFromWaveFile("Samples\\open-hat-trimmed.wav");
            sampleSources = new List<SampleSource> {kickSample, snareSample, closedHatsSample, openHatsSample};

            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(openHatsSample.SampleWaveFormat.SampleRate, openHatsSample.SampleWaveFormat.Channels);
        }

        public virtual WaveFormat WaveFormat { get; }

        public MusicSampleProvider GetSampleProvider(int note)
        {
            return new MusicSampleProvider(sampleSources[note]);
        }
    }
}
