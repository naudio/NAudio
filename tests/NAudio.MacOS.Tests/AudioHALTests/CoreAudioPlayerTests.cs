
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

    // Verifies that we can hear a three-second white noise through the player.
    // To do this test:
    // 1. Build this tests project.
    // 2. Use the command: dotnet test --output Detailed --filter CanPlayThreeSecondSignal.
    // 3. Wait to hear the white noise. If you cannot hear the noise after four seconds, consider the test failed.
    // The test is also failed if an assertion is raised or the playback sounds garbled.
    // 4. If you hear the white noise, make sure that all test cases provided below
    // do actually play the white noise the same. Even if you find one inconsistency
    // (e.g. garbled playback or the noise is not audible),
    // you should consider the test as failed.
    // However, even doing these steps does not necessarily mean that the test is failed;
    // if, for example, you hear audio a bit different on some runs but you hear the white noise, 
    // that is absolutely normal and not a failure. If you want to be sure that the player 
    // is fine, I would recommend running this test a couple of times, probably 5 times, 
    // and if the number of 'correct' tests is more than 3 the test probably passes.
    [TestCase(44100, 2, false)]
    [TestCase(48000, 2, false)]
    [TestCase(48000, 1, false)]
    [TestCase(52000, 1, false)]
    [TestCase(64000, 3, false)]
    [TestCase(48000, 2, true)]
    [TestCase(52000, 4, true)]
    [TestCase(64000, 4, true)]
    public void CanPlayThreeSecondSignal(int sampleRate, int channels, bool useIeeeFloat)
    {
        var sg = new SignalGenerator(sampleRate, channels);
        sg.Frequency = 100;
        sg.Type = SignalGeneratorType.White;
        sg.Gain = 0.7;

        IWaveProvider provider = useIeeeFloat ? sg.Take(TimeSpan.FromSeconds(3)).ToWaveProvider() : sg.Take(TimeSpan.FromSeconds(3)).ToWaveProvider16();

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

    // Verifies that the player is capable enough to handle
    // virtual format changes on a HAL audio stream.
    // It does that by spawning a thread that dispatches 
    // random virtual format changes.
    // This primarily excercises the initialization logic
    // to see whether it successfully resolves the target
    // format on each change, and whether the resampler 
    // is attached as required.
    [Test]
    public void IsHardenedAgainstVirtualFormatChanges()
    {
        CoreAudioPlayer player = new();

        var streams = player.Device.GetStreams(AudioObjectPropertyScopeConstants.Output);
        if (streams.Length > 1)
        {
            Assert.Ignore("This test requires an audio device with an interleaved output stream.");
        }

        var sg = new SignalGenerator(48000, 2) { Frequency = 500, Gain = 0.5 };

        Assert.DoesNotThrow(() => player.Init(sg.Take(TimeSpan.FromSeconds(30)).ToWaveProvider()));

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
        // A list of sample rates that the random number generator should pick
        // while tampering the stream's virtual format.
        int[] sampleRatesToSelectFrom = [18000, 6000, 48000, 44100, 14000];
        int times = 0;
        AudioStream stream = null;
        WaveFormat outFormat = instance.OutputWaveFormat;
        foreach (var s in instance.Device.GetStreams(AudioObjectPropertyScopeConstants.Output))
        {
            var vf = s.VirtualFormat;
            if (vf.Equals(outFormat))
            {
                stream = s;
                break;
            }
        }
        int newRate;
        while (stream is not null && times < 5)
        {
            newRate = sampleRatesToSelectFrom[(int)(Random.Shared.NextSingle() * sampleRatesToSelectFrom.Length)];
            var vf = stream.VirtualFormat;
            System.Console.WriteLine("Changing sample rate to: {0}", newRate);
            stream.VirtualFormat = new(newRate, vf.BitsPerSample, vf.Channels);
            Thread.Sleep(3400);
            System.Console.WriteLine("Reverting sample rate change.");
            stream.VirtualFormat = vf;
            Thread.Sleep(2000);
            times++;
        }
        System.Console.WriteLine("Arbitrary changes were performed and completed.");
    }
}