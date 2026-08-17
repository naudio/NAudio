
using System;
using System.IO;

using NAudio.Wave;
using NAudio.MacOS.AudioToolbox;
using NAudio.Wave.SampleProviders;

using NUnit.Framework;

namespace NAudio.MacOS.Tests;

[TestFixture]
public class MacAudioConverterTests
{
    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast();

    private static byte[] ReadAllBytes(IWaveProvider source, int chunkSize)
    {
        var readBuffer = new byte[chunkSize];
        using var output = new MemoryStream();
        int bytesRead;
        while ((bytesRead = source.Read(readBuffer.AsSpan())) > 0)
        {
            output.Write(readBuffer, 0, bytesRead);
        }
        return output.ToArray();
    }

    private static float[] BytesToFloatSamples(byte[] bytes)
    {
        var samples = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, samples, 0, samples.Length * sizeof(float));
        return samples;
    }

    private static double EstimateFrequencyByPositiveZeroCrossings(float[] samples, int sampleRate)
    {
        if (samples.Length < 3)
        {
            return 0;
        }

        int start = samples.Length / 10;
        int end = samples.Length - start;
        int positiveCrossings = 0;
        for (int n = Math.Max(start + 1, 1); n < end; n++)
        {
            if (samples[n - 1] <= 0 && samples[n] > 0)
            {
                positiveCrossings++;
            }
        }

        double analyzedSeconds = (end - start) / (double)sampleRate;
        if (analyzedSeconds <= 0)
        {
            return 0;
        }
        return positiveCrossings / analyzedSeconds;
    }

    [TestCase(44100, 2, 32, Speakers.None)]
    [TestCase(44100, 2, 24, Speakers.None)]
    [TestCase(44100, 3, 16, Speakers.Stereo | Speakers.LowFrequency)]
    [TestCase(48000, 4, 32, Speakers.Quad)]
    [TestCase(48000, 5, 16, Speakers.Quad | Speakers.TopCenter)]
    [TestCase(48000, 5, 24, Speakers.Quad | Speakers.LowFrequency)]
    public void CanResampleSignalToVariousFormats(int sampleRate, int channels, int bitRate, Speakers channelMask)
    {
        var source = CreateSineWaveSource(44100, 2, 3d, 4000d);

        WaveFormat outFormat = channelMask != Speakers.None ?
            new WaveFormatExtensible(
                sampleRate,
                bitRate,
                channels,
                (int)channelMask
            ) :
            new WaveFormat(sampleRate, bitRate, channels);

        MacAudioConverter cnv = new(source, outFormat);

        byte[] buffer = new byte[outFormat.ConvertLatencyToByteSize(400)];

        if (source.WaveFormat.SampleRate == sampleRate)
        {
            Assert.Throws<AudioConverterException>(() => cnv.Complexity = AudioConverterSampleRateComplexityConstants.Mastering);
            Assert.Throws<AudioConverterException>(() => cnv.Quality = AudioConverterQuality.Medium);
        }
        else
        {
            Assert.DoesNotThrow(() => cnv.Complexity = AudioConverterSampleRateComplexityConstants.Mastering);
            Assert.DoesNotThrow(() => cnv.Quality = AudioConverterQuality.Medium);
        }

        int totalRead = 0;
        int br;
        do
        {
            br = cnv.Read(buffer);
            totalRead += br;
        } while (br > 0);

        Assert.That(totalRead, Is.GreaterThan(0));

        Assert.DoesNotThrow(cnv.Dispose);

        Assert.DoesNotThrow(source.Dispose);
    }

    private static RawSourceWaveStream CreateSineWaveSource(int sampleRate, int channels, double durationSeconds, double frequency)
    {
        var signal = new SignalGenerator(sampleRate, channels)
        {
            Type = SignalGeneratorType.Sin,
            Frequency = frequency,
            Gain = 0.8
        };

        var sampleCount = (int)(sampleRate * channels * durationSeconds);
        var sampleBuffer = new float[sampleCount];
        var read = signal.Read(sampleBuffer.AsSpan());
        Assert.That(read, Is.EqualTo(sampleBuffer.Length));

        var bytes = new byte[sampleBuffer.Length * sizeof(float)];
        Buffer.BlockCopy(sampleBuffer, 0, bytes, 0, bytes.Length);
        return new RawSourceWaveStream(new MemoryStream(bytes), WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
    }

    [Test]
    public void RepositionAfterRewindingSourceRepeatsOutput()
    {
        var source = CreateSineWaveSource(44100, 1, 1.0, 700);
        var resampler = new MacAudioConverter(source, WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));
        var first = new byte[4096];
        var second = new byte[4096];

        var firstRead = resampler.Read(first.AsSpan());
        Assert.That(firstRead, Is.GreaterThan(0));

        source.Position = 0L;
        resampler.Reset();

        var secondRead = resampler.Read(second.AsSpan());
        Assert.That(secondRead, Is.EqualTo(firstRead));
        Assert.That(second, Is.EqualTo(first), "After source rewind + reposition, initial output should repeat");

        Assert.DoesNotThrow(resampler.Dispose);

        Assert.DoesNotThrow(source.Dispose);
    }

    [TestCase(44100, 48000)]
    [TestCase(48000, 44100)]
    [TestCase(16000, 44100)]
    [TestCase(44100, 16000)]
    [TestCase(96000, 22050)]
    [TestCase(22050, 96000)]
    [TestCase(44100, 44100)]
    public void ReadResamplesAndPreservesFrequency(int inputRate, int outputRate)
    {
        const double frequency = 1000;
        const double durationSeconds = 1.0;

        var source = CreateSineWaveSource(inputRate, 1, durationSeconds, frequency);
        var resampler = new MacAudioConverter(source, WaveFormat.CreateIeeeFloatWaveFormat(outputRate, 1));
        var outputBytes = ReadAllBytes(resampler, resampler.WaveFormat.AverageBytesPerSecond / 100);
        var outputSamples = BytesToFloatSamples(outputBytes);

        Assert.That(outputSamples.Length, Is.GreaterThan(outputRate / 2), "Expected substantial output samples");

        var estimatedFrequency = EstimateFrequencyByPositiveZeroCrossings(outputSamples, outputRate);
        Assert.That(estimatedFrequency, Is.InRange(frequency - 30, frequency + 30),
            "Estimated frequency should remain near source frequency after resampling");

        Assert.DoesNotThrow(resampler.Dispose);

        Assert.DoesNotThrow(source.Dispose);
    }

}