
using System;
using System.Threading;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.MacOS.AudioToolbox;
using NAudio.Wave.SampleProviders;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.AudioQueueTests;

[TestFixture]
[Category("IntegrationTest")]
public class AudioQueuePlayerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast(10, 5);

    private static IWaveProvider Tone(double seconds)
    {
        var generator = new SignalGenerator(44100, 2) { Type = SignalGeneratorType.Sin, Gain = 0.05 };
        return generator.Take(TimeSpan.FromSeconds(seconds)).ToWaveProvider();
    }

    private static AudioQueuePlayer NewPlayer(out Task<StoppedEventArgs> stopped)
    {
        var player = new AudioQueuePlayerBuilder().Build();
        var source = new TaskCompletionSource<StoppedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        player.PlaybackStopped += (_, e) => source.TrySetResult(e);
        stopped = source.Task;
        return player;
    }

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

        AudioQueuePlayer player = new AudioQueuePlayerBuilder().Build();

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
    public void BuilderValuesReachThePlayer()
    {
        using var player = new AudioQueuePlayerBuilder().WithLatency(200).WithNumberOfBuffers(5).WithTimePitchAlgorithm(TimePitchAlgorithmConstants.Spectral).Build();
        Assert.That(player.LatencyMilliseconds, Is.EqualTo(200));
        Assert.That(player.NumberOfBuffers, Is.EqualTo(5));
        Assert.That(player.TimePitchAlgorithm, Is.EqualTo(TimePitchAlgorithmConstants.Spectral));
        Assert.That(player.Device, Is.Null);
    }

    [Test]
    public void CanStopAndPlayAgain()
    {
        using var player = new AudioQueuePlayerBuilder().Build();
        int stops = 0;
        player.PlaybackStopped += (_, _) => Interlocked.Increment(ref stops);
        player.Init(Tone(5));

        player.Play();
        Thread.Sleep(200);
        player.Stop();
        Assert.That(player.PlaybackState, Is.EqualTo(PlaybackState.Stopped));

        player.Play();
        Thread.Sleep(200);
        Assert.That(player.PlaybackState, Is.EqualTo(PlaybackState.Playing));
        Assert.That(player.GetPosition(), Is.GreaterThan(0L));
        player.Stop();
        player.Stop();

        // The events are posted to the synchronization context captured when building.
        Assert.That(SpinWait.SpinUntil(() => Volatile.Read(ref stops) == 2, Timeout), Is.True);
    }

    [Test]
    public void VolumeIsValidatedAndKept()
    {
        using var player = new AudioQueuePlayerBuilder().Build();
        player.Volume = 0.25f;
        player.Init(Tone(0.2));
        Assert.That(player.Volume, Is.EqualTo(0.25f));
        player.Volume = 0.5f;
        Assert.That(player.Volume, Is.EqualTo(0.5f));
        Assert.Throws<ArgumentOutOfRangeException>(() => player.Volume = 1.5f);
    }

    [Test]
    public void TimePitchCanBeEnabled()
    {
        using var configured = new AudioQueuePlayerBuilder().WithTimePitchAlgorithm(TimePitchAlgorithmConstants.TimeDomain).Build();
        Assert.DoesNotThrow(() => configured.Init(Tone(0.2)));
    }

    [Test]
    public void DisposeWhilePlayingIsSafeAndRepeatable()
    {
        var player = NewPlayer(out var stopped);
        player.Init(Tone(5));
        player.Play();
        Thread.Sleep(100);

        player.Dispose();
        Assert.DoesNotThrow(player.Dispose);
        Assert.That(stopped.Wait(Timeout), Is.True);
        Assert.Throws<ObjectDisposedException>(player.Play);
    }

    [Test]
    public async Task BuildAsyncCreatesAPlayer()
    {
        await using var player = await new AudioQueuePlayerBuilder().BuildAsync();
        Assert.That(player.PlaybackState, Is.EqualTo(PlaybackState.Stopped));
    }
}