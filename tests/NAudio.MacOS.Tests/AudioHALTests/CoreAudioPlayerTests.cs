
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
    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast(10, 5);

    [TestCase(44100, 2, false)]
    [TestCase(48000, 2, false)]
    [TestCase(48000, 1, false)]
    [TestCase(52000, 1, false)]
    [TestCase(48000, 2, true)]
    [TestCase(52000, 4, true)]
    public void CanPlayTenSecondSignal(int sampleRate, int channels, bool useIeeeFloat)
    {
        var sg = new SignalGenerator(sampleRate, channels);
        sg.Frequency = 100;
        sg.Type = SignalGeneratorType.White;
        sg.Gain = 0.7;

        IWaveProvider provider = useIeeeFloat ? sg.Take(TimeSpan.FromSeconds(10)).ToWaveProvider() : sg.Take(TimeSpan.FromSeconds(10)).ToWaveProvider16();

        CoreAudioPlayer player = new();

        Assert.DoesNotThrow(() => player.Init(provider));

        Exception capturedException = null;

        player.PlaybackStopped += (object sender, StoppedEventArgs e) =>
        {
            capturedException = e.Exception;
        };

        Assert.DoesNotThrow(player.Play);

        while (player.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(500);
        }

        if (capturedException is not null)
        {
            Assert.Fail("The player was stopped abruptly: " + capturedException);
        }

        Assert.DoesNotThrow(player.Stop);

        Assert.DoesNotThrow(player.Dispose);
    }

    [Test]
    public void IsHardenedAgainstVirtualFormatChanges()
    {
        CoreAudioPlayer player = new();

        var streams = player.Device.GetStreams(AudioObjectPropertyScopeConstants.Output);
        if (streams.Length > 1)
        {
            Assert.Ignore("This test requires an audio device with an interleaved output stream.");
        }

        var sg = new SignalGenerator(48000, 2);

        Assert.DoesNotThrow(() => player.Init(sg.Take(TimeSpan.FromSeconds(5)).ToWaveProvider()));

        Exception capturedException = null;

        player.PlaybackStopped += (object sender, StoppedEventArgs e) =>
        {
            capturedException = e.Exception;
        };

        Assert.DoesNotThrow(player.Play);

        new Thread(() => PerformArbitraryChanges(player)).Start();

        while (player.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(500);
        }

        if (capturedException is not null)
        {
            Assert.Fail("The player was stopped abruptly: " + capturedException);
        }

        Assert.DoesNotThrow(player.Stop);

        Assert.DoesNotThrow(player.Dispose);
    }

    private static void PerformArbitraryChanges(CoreAudioPlayer instance)
    {
        WaveFormat outFormat = instance.OutputWaveFormat;
        foreach (var s in instance.Device.GetStreams(AudioObjectPropertyScopeConstants.Output))
        {
            var vf = s.VirtualFormat;
            if (vf.Equals(outFormat))
            {
                s.VirtualFormat = new(18000, vf.BitsPerSample, vf.Channels);
                Thread.Sleep(500);
                s.VirtualFormat = vf;
                break;
            }
        }
    }
}