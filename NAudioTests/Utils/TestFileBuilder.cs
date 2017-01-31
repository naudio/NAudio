using System;
using System.IO;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudioTests.Utils
{
    static class TestFileBuilder
    {
        public static string CreateMp3File(int durationSeconds, int sampleRate=44100, int channels=2)
        {
            var testSignal = new SignalGenerator(sampleRate, channels) { Frequency = 1000, Gain = 0.25 }
                .Take(TimeSpan.FromSeconds(durationSeconds))
                .ToWaveProvider();
            var path = Path.Combine(Path.GetTempPath(), "NAudioTests");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var file = Path.Combine(Path.GetTempPath(), @"NAudioTests\TestSignal.mp3");
            MediaFoundationApi.Startup();
            MediaFoundationEncoder.EncodeToMp3(testSignal, file, 96000);
            return file;
        }

    }
}
