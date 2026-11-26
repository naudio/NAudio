using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Wave;

/// <summary>
/// Regression cover for issue #1515: <see cref="WaveOut.Stop"/> could run while the playback thread
/// was inside the source's Read, and the thread then queued that buffer again after
/// <c>waveOutReset</c>. The device kept playing it after Stop(), and Dispose() then unprepared and
/// freed buffers the driver still owned (a crash before #1502, a leaked buffer and device since).
/// <para>
/// These need a real WinMM output device, so they are integration tests. They play silence.
/// </para>
/// </summary>
[TestFixture]
[Category("IntegrationTest")]
[Platform("Win")]
public class WaveOutStopRaceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [SetUp]
    public void SetUp()
    {
        if (WaveOut.DeviceCount == 0)
        {
            Assert.Ignore("No WinMM output device available on this machine");
        }
    }

    /// <summary>
    /// Silence whose Read takes a while, so a test can call Stop() or Dispose() while the
    /// playback thread is inside it — the interleaving from the issue.
    /// </summary>
    private sealed class SlowSilence(int readDelayMilliseconds) : IWaveProvider
    {
        private int readsInProgress;
        private int readsStartedAfterClose;
        private volatile bool closed;

        public ManualResetEventSlim Reading { get; } = new();
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        public int ReadsInProgress => Volatile.Read(ref readsInProgress);
        public int ReadsStartedAfterClose => Volatile.Read(ref readsStartedAfterClose);

        /// <summary>Marks the point after which the player must no longer read from this source.</summary>
        public void Close() => closed = true;

        public int Read(Span<byte> buffer)
        {
            if (closed) Interlocked.Increment(ref readsStartedAfterClose);
            Interlocked.Increment(ref readsInProgress);
            try
            {
                Reading.Set();
                Thread.Sleep(readDelayMilliseconds);
                buffer.Clear();
                return buffer.Length;
            }
            finally
            {
                Interlocked.Decrement(ref readsInProgress);
            }
        }
    }

    private sealed class EmptyProvider : IWaveProvider
    {
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        public int Read(Span<byte> buffer) => 0;
    }

    [Test]
    public void Stop_WhileSourceIsReading_LeavesNoBufferQueued()
    {
        var source = new SlowSilence(60);
        using var output = new WaveOut();
        using var stopped = new ManualResetEventSlim();
        output.PlaybackStopped += (_, _) => stopped.Set();
        output.Init(source);
        output.Play();

        Assert.That(source.Reading.Wait(Timeout), Is.True, "playback thread never read from the source");
        output.Stop(); // waveOutReset while the playback thread is inside Read
        Assert.That(stopped.Wait(Timeout), Is.True, "PlaybackStopped was not raised");

        Assert.That(output.AnyBufferQueued, Is.False,
            "a buffer was queued on the device again after Stop() had reset it");
    }

    [Test]
    public void Dispose_WhileSourceIsReading_WaitsForThePlaybackThread()
    {
        var source = new SlowSilence(60);
        var output = new WaveOut();
        output.Init(source);
        output.Play();

        Assert.That(source.Reading.Wait(Timeout), Is.True, "playback thread never read from the source");
        output.Dispose();
        source.Close();

        Assert.That(source.ReadsInProgress, Is.Zero, "Dispose() returned while the playback thread was still in Read");
        Thread.Sleep(200);
        Assert.That(source.ReadsStartedAfterClose, Is.Zero, "the source was read after Dispose() returned");
    }

    [Test]
    public void Play_ImmediatelyAfterStop_KeepsPlaying()
    {
        var source = new SlowSilence(20);
        using var output = new WaveOut();
        output.Init(source);
        output.Play();

        Assert.That(source.Reading.Wait(Timeout), Is.True, "playback thread never read from the source");
        output.Stop();
        output.Play();

        // The previous playback thread used to finish its Read after this Play(), then set the
        // state back to Stopped on its way out, ending the new playback too.
        Thread.Sleep(300);
        Assert.That(output.PlaybackState, Is.EqualTo(PlaybackState.Playing));
    }

    [Test]
    public void Dispose_FromPlaybackStoppedHandler_DoesNotDeadlock()
    {
        // No synchronization context here, so PlaybackStopped is raised on the playback thread
        // itself, and Dispose() must not wait for the thread it is running on.
        var output = new WaveOut();
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        output.PlaybackStopped += (_, _) =>
        {
            output.Dispose();
            disposed.SetResult();
        };
        output.Init(new EmptyProvider());
        output.Play();

        Assert.That(disposed.Task.Wait(Timeout), Is.True, "Dispose() from PlaybackStopped deadlocked");
    }
}
