using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class DrumKit
    {
        private readonly List<SampleSource> sampleSources;

        public DrumKit()
        {
            // Resolve relative to the executable so the kit loads regardless of working
            // directory (e.g. when running via `dotnet run` the CWD is the project dir).
            var samplesDir = Path.Combine(AppContext.BaseDirectory, "Samples");
            var kickSample = SampleSource.CreateFromWaveFile(Path.Combine(samplesDir, "kick-trimmed.wav"));
            var snareSample = SampleSource.CreateFromWaveFile(Path.Combine(samplesDir, "snare-trimmed.wav"));
            var closedHatsSample = SampleSource.CreateFromWaveFile(Path.Combine(samplesDir, "closed-hat-trimmed.wav"));
            var openHatsSample = SampleSource.CreateFromWaveFile(Path.Combine(samplesDir, "open-hat-trimmed.wav"));
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
