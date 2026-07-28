
using System;
using System.Threading;

using NAudio.MacOS.CoreAudio;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.AudioHALTests;

[TestFixture]
[Category("IntegrationTest")]
public class CoreAudioPlayerTests
{
    [TestCase(44100, 2, false)]
    [TestCase(48000, 2, false)]
    [TestCase(48000, 1, false)]
    [TestCase(48000, 1, false)]
    [TestCase(48000, 2, true)]
    [TestCase(52000, 4, true)]
    public void CanPlayTenSecondSignal(int sampleRate, int channels, bool useIeeeFloat)
    {
        var sg = new SignalGenerator(sampleRate, channels);

        IWaveProvider provider = useIeeeFloat ? sg.Take(TimeSpan.FromSeconds(10)).ToWaveProvider() : sg.Take(TimeSpan.FromSeconds(10)).ToWaveProvider16();

        CoreAudioPlayer player = new();

        Assert.DoesNotThrow(() => player.Init(provider));

        player.Play();

        player.PlaybackStopped += (object sender, StoppedEventArgs e) =>
        {
            if (e.Exception is not null)
            {
                throw e.Exception;
            }
        };

        while (player.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(500);
        }

        player.Stop();

        player.Dispose();
    }

    [Test]
    public void IsHardenedAgainstVirtualFormatChanges()
    {
        var sg = new SignalGenerator(48000, 2);

        IWaveProvider provider = sg.Take(TimeSpan.FromSeconds(10)).ToWaveProvider();

        CoreAudioPlayer player = new();

        player.Init(provider);

        player.Play();

        new Thread(() => PerformArbitraryChanges(player.OutputWaveFormat)).Start();

        while (player.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(500);
        }

        player.Stop();

        player.Dispose();
    }

    private static void PerformArbitraryChanges(WaveFormat outFormat)
    {
        foreach (var s in AudioSystemObject.Instance.DefaultOutputDevice.GetStreams(AudioObjectPropertyScopeConstants.Output))
        {
            var vf = s.VirtualFormat;
            if (vf.Equals(outFormat))
            {
                s.VirtualFormat = new(18000, vf.BitsPerSample, vf.Channels);
                s.VirtualFormat = new(vf.SampleRate, vf.BitsPerSample, vf.Channels);
                break;
            }
        }
    }
}