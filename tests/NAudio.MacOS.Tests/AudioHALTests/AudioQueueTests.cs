
using System;
using System.Threading;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.AudioToolbox;
using NAudio.Wave.SampleProviders;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.AudioHALTests;

[TestFixture]
[Category("IntegrationTest")]
public class AudioQueueTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast(10, 5);

    private sealed class ThrowingProvider(WaveFormat waveFormat) : IWaveProvider
    {
        public WaveFormat WaveFormat { get; } = waveFormat;

        public int Read(Span<byte> buffer) => throw new InvalidOperationException("read failure");
    }

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

    [Test]
    public void BuilderValuesReachThePlayer()
    {
        using var player = new AudioQueuePlayerBuilder().WithLatency(200).WithNumberOfBuffers(5).EnableLevelMetering().WithTimePitchAlgorithm(TimePitchAlgorithmConstants.Spectral).Build();
        Assert.That(player.LatencyMilliseconds, Is.EqualTo(200));
        Assert.That(player.NumberOfBuffers, Is.EqualTo(5));
        Assert.That(player.LevelMeteringEnabled, Is.True);
        Assert.That(player.TimePitchAlgorithm, Is.EqualTo(TimePitchAlgorithmConstants.Spectral));
        Assert.That(player.Device, Is.Null);
    }

    [Test]
    public void PlaysToTheEndAndReportsThePosition()
    {
        using var player = NewPlayer(out var stopped);
        var provider = Tone(0.5);
        player.Init(provider);
        player.Play();

        Assert.That(stopped.Wait(Timeout), Is.True);
        Assert.That(stopped.Result.Exception, Is.Null);
        Assert.That(player.PlaybackState, Is.EqualTo(PlaybackState.Stopped));
        Assert.That(player.GetPosition(), Is.EqualTo((long)(44100 * 0.5) * provider.WaveFormat.BlockAlign));
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
    public void PauseHoldsThePosition()
    {
        using var player = new AudioQueuePlayerBuilder().Build();
        player.Init(Tone(5));
        player.Play();
        Thread.Sleep(300);
        player.Pause();
        Thread.Sleep(50);

        long position = player.GetPosition();
        Thread.Sleep(200);
        Assert.That(player.GetPosition(), Is.EqualTo(position));

        player.Play();
        Thread.Sleep(300);
        Assert.That(player.GetPosition(), Is.GreaterThan(position));
    }

    [Test]
    public void ProviderFailureIsReported()
    {
        using var player = NewPlayer(out var stopped);
        player.Init(new ThrowingProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)));
        player.Play();

        Assert.That(stopped.Wait(Timeout), Is.True);
        Assert.That(stopped.Result.Exception, Is.TypeOf<InvalidOperationException>());
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
    public void TimePitchAndMeteringCanBeEnabled()
    {
        using var configured = new AudioQueuePlayerBuilder().EnableLevelMetering().WithTimePitchAlgorithm(TimePitchAlgorithmConstants.TimeDomain).Build();
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

    private static void RequireInputDevice()
    {
        if (AudioSystemObject.Instance.DefaultInputDevice is null) { Assert.Ignore("No input device is available."); }
    }

    [Test]
    public void RecorderDeliversAudioAndCanRestart()
    {
        RequireInputDevice();
        using var recorder = new AudioQueueRecorderBuilder().Build();
        Assert.That(recorder.CaptureFormat.Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));

        long bytes = 0;
        int stops = 0;
        recorder.DataAvailable += (data, _, _) => Interlocked.Add(ref bytes, data.Length);
        recorder.RecordingStopped += (_, _) => Interlocked.Increment(ref stops);

        for (int I = 0; I < 2; I++)
        {
            Interlocked.Exchange(ref bytes, 0);
            recorder.StartRecording();
            Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Capturing));
            Thread.Sleep(500);
            recorder.StopRecording();
            Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Stopped));
            Assert.That(Interlocked.Read(ref bytes), Is.GreaterThan(0L));
            Assert.That(Interlocked.Read(ref bytes) % recorder.CaptureFormat.BlockAlign, Is.EqualTo(0L));
        }

        // The events are posted to the synchronization context captured when building.
        Assert.That(SpinWait.SpinUntil(() => Volatile.Read(ref stops) == 2, Timeout), Is.True);
    }

    [Test]
    public void RecorderHandlerFailureIsReported()
    {
        RequireInputDevice();
        using var recorder = new AudioQueueRecorderBuilder().Build();
        var source = new TaskCompletionSource<StoppedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        recorder.DataAvailable += (_, _, _) => throw new InvalidOperationException("handler failure");
        recorder.RecordingStopped += (_, e) => source.TrySetResult(e);

        recorder.StartRecording();

        Assert.That(source.Task.Wait(Timeout), Is.True);
        Assert.That(source.Task.Result.Exception, Is.TypeOf<InvalidOperationException>());
    }
}
