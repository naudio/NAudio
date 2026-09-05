
using System;
using System.Threading;

using NAudio.MacOS.CoreAudio;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using System.Linq;

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

        // The tampering runs on its own thread, so anything it throws has to be
        // carried back here. Left unhandled it takes down the whole test host
        // and every test after this one never runs.
        Exception tamperingException = null;
        var tamperingThread = new Thread(() =>
        {
            try
            {
                PerformArbitraryChanges(player);
            }
            catch (Exception ex)
            {
                tamperingException = ex;
            }
        });
        tamperingThread.Start();

        while (player.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(500);
        }

        // Wait for the tampering to finish rather than bounding it here. Every
        // wait inside it is already bounded, and a timeout that expires would
        // leave the thread still changing the default device's format while the
        // tests that follow run against it.
        tamperingThread.Join();

        if (tamperingException is not null)
        {
            // Assert.Ignore inside the thread surfaces as IgnoreException. It
            // means the device cannot support this test, not that the test
            // failed, so re-raise it rather than reporting a failure.
            if (tamperingException is IgnoreException)
            {
                Assert.Ignore(tamperingException.Message);
            }

            Assert.Fail("Tampering with the virtual format failed: " + tamperingException);
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
        // The rates have to come from the device. A fixed list only works on
        // hardware that happens to accept those particular values: a Universal
        // Audio Apollo, for one, offers 44.1/48/88.2/96/176.4/192 and rejects
        // anything else with kAudioDeviceUnsupportedFormatError, which used to
        // abort the entire test run from this thread.
        int[] sampleRatesToSelectFrom = instance.Device.AvailableNomimalSampleRates
            .Where(static range => range.min == range.max)
            .Select(static range => (int)range.min)
            .Distinct()
            .ToArray();

        if (sampleRatesToSelectFrom.Length < 2)
        {
            Assert.Ignore("This test requires a device offering at least two discrete nominal sample rates.");
        }

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
        if (stream is null) { return; }

        // The format this stream was in before the test touched anything. It has
        // to be captured once, out here: capturing it inside the loop makes each
        // pass treat whatever the previous pass left as the original, so the
        // device drifts and is not put back where it started.
        WaveFormat originalStreamFormat = stream.VirtualFormat;

        int newRate;
        try
        {
            while (times < 5)
            {
                newRate = sampleRatesToSelectFrom[(int)(Random.Shared.NextSingle() * sampleRatesToSelectFrom.Length)];
                var vf = originalStreamFormat;
                System.Console.WriteLine("Changing sample rate to: {0}", newRate);
                stream.VirtualFormat = new(newRate, vf.BitsPerSample, vf.Channels);
                // Wait for the hardware to arrive at the new rate rather than
                // assuming a fixed interval. Virtual devices switch instantly;
                // a Universal Audio Apollo does it with a mechanical relay and
                // takes seconds. Driving the next step off a constant means the
                // faster hardware waits needlessly and the slower hardware is
                // still switching when the test moves on - which is what left the
                // device in flux for the tests that follow.
                WaitForStreamToSettle(stream);
                Thread.Sleep(SettleMilliseconds);
                System.Console.WriteLine("Reverting sample rate change.");
                stream.VirtualFormat = vf;
                WaitForStreamToSettle(stream);
                Thread.Sleep(SettleMilliseconds);
                times++;
            }

        }
        finally
        {
            // Put the stream back where it was found, whatever happened above,
            // and wait until it has actually got there. Interfaces do not all
            // switch instantly - a Universal Audio Apollo uses a mechanical
            // relay and takes seconds - and while one is still switching it
            // keeps emitting format-change notifications, which stop anything
            // started against it. Returning while that is still going on leaves
            // the tests that follow running against a device in flux.
            stream.VirtualFormat = originalStreamFormat;
            WaitForStreamToSettle(stream);
        }

        System.Console.WriteLine("Arbitrary changes were performed and completed.");
    }

    // A floor on how long each rate is held, on top of waiting for the reported
    // format to stop moving. The reported format does not track a mechanical
    // relay's physical movement, so "settled" can arrive while the hardware is
    // still switching; without a floor the test cycles a physical part faster
    // than the original fixed delays did. Two seconds keeps it no more
    // aggressive than the code this replaced.
    private const int SettleMilliseconds = 2000;

    // Waits until the stream's format stops moving, rather than for a
    // particular value. Asking whether the stream has reached a specific rate
    // does not work across hardware: on the interface this was written against
    // the stream does not report the requested rate back promptly even when the
    // change is accepted, so a target-value poll just burns its whole timeout.
    // What the following tests actually need is for the device to have finished
    // changing, whatever it settled on.
    private static void WaitForStreamToSettle(AudioStream stream)
    {
        const int TimeoutMilliseconds = 30000;
        const int PollMilliseconds = 250;
        const int StableReadingsRequired = 4; // roughly a second unchanged

        double previousRate = double.NaN;
        int stableReadings = 0;

        for (int waited = 0; waited < TimeoutMilliseconds; waited += PollMilliseconds)
        {
            double rate = stream.VirtualFormat.SampleRate;
            stableReadings = rate.Equals(previousRate) ? stableReadings + 1 : 0;
            if (stableReadings >= StableReadingsRequired) { return; }
            previousRate = rate;
            Thread.Sleep(PollMilliseconds);
        }

        System.Console.WriteLine(
            "Warning: the stream's format was still changing after {0} ms; it reads {1} Hz.",
            TimeoutMilliseconds, stream.VirtualFormat.SampleRate);
    }
}