
using System;

using NAudio.MacOS.AudioToolbox;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using NUnit.Framework;

namespace NAudio.MacOS.Tests;

[TestFixture]
public class MacAudioConverterTests
{
    [TestCase(44100, 2, 32, Speakers.None)]
    [TestCase(48000, 4, 32, Speakers.Quad)]
    [TestCase(48000, 5, 16, Speakers.Quad | Speakers.TopCenter)]
    [TestCase(48000, 5, 24, Speakers.Quad | Speakers.LowFrequency)]
    public void CanResampleSignalToVariousFormats(int sampleRate, int channels, int bitRate, Speakers channelMask)
    {
        var sg = new SignalGenerator(44100, 2).Take(TimeSpan.FromSeconds(3)).ToWaveProvider();

        byte[] buffer = new byte[sg.WaveFormat.ConvertLatencyToByteSize(400)];

        WaveFormat outFormat = channelMask != Speakers.None ?
            new WaveFormatExtensible(
                sampleRate,
                bitRate,
                channels,
                (int)channelMask
            ) :
            new WaveFormat(sampleRate, bitRate, channels);

        MacAudioConverter cnv = new(sg, outFormat);

        if (sg.WaveFormat.SampleRate == sampleRate)
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

        Assert.Greater(totalRead, 0);

        cnv.Dispose();
    }
}