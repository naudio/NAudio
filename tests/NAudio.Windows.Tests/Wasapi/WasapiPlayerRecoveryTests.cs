using System;
using System.Threading;
using NUnit.Framework;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Windows.Tests.Utils;

namespace NAudio.Windows.Tests.Wasapi;

/// <summary>
/// Regression cover for issue #1442: a play thread that left without reaching the orderly shutdown at
/// the end of its loop used to strand the player. It reported PlaybackState.Playing with no thread
/// behind it, and — when the exit was an exception thrown from inside FillBuffer's buffer lease — left
/// the render buffer released as fully written, so the next Play() asked GetBuffer for more frames than
/// were free and died with AUDCLNT_E_BUFFER_TOO_LARGE.
/// <para>
/// These need a real render endpoint, so they are integration tests. They play a brief quiet sine.
/// </para>
/// </summary>
[TestFixture]
[Category("IntegrationTest")]
[Platform("Win")]
public class WasapiPlayerRecoveryTests
{
    [SetUp]
    public void SetUp()
    {
        OSUtils.RequireVista();

        using var enumerator = new MMDeviceEnumerator();
        if (!enumerator.TryGetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out var device))
        {
            Assert.Ignore("No default render endpoint available on this machine");
            return;
        }
        device.Dispose();
    }

    /// <summary>Throws on the first Read only, then plays normally — the issue's exact shape.</summary>
    private sealed class ThrowOnFirstRead(IWaveProvider inner) : IWaveProvider
    {
        private bool thrown;

        public WaveFormat WaveFormat => inner.WaveFormat;

        public int Read(Span<byte> buffer)
        {
            if (!thrown)
            {
                thrown = true;
                throw new InvalidOperationException("simulated source failure");
            }
            return inner.Read(buffer);
        }
    }

    /// <summary>A source that is immediately at its end, so the very first FillBuffer returns true.</summary>
    private sealed class EmptyProvider(WaveFormat waveFormat) : IWaveProvider
    {
        public WaveFormat WaveFormat => waveFormat;

        public int Read(Span<byte> buffer) => 0;
    }

    [Test]
    public void SourceThrowingOnFirstRead_StopsAndCanPlayAgain()
    {
        var source = new ThrowOnFirstRead(
            new SignalGenerator(44100, 2) { Frequency = 440, Gain = 0.05 }.ToWaveProvider());

        using var player = new WasapiPlayerBuilder().Build();
        using var stopped = new ManualResetEventSlim(false);
        Exception reported = null;
        player.PlaybackStopped += (_, e) => { reported = e.Exception; stopped.Set(); };

        player.Init(source);
        player.Play();

        Assert.That(stopped.Wait(5000), Is.True, "PlaybackStopped never fired");
        Assert.That(reported, Is.TypeOf<InvalidOperationException>());
        Assert.That(player.PlaybackState, Is.EqualTo(PlaybackState.Stopped),
            "the failed play thread left PlaybackState reporting Playing");

        // The regression itself: this second Play() used to fail with AUDCLNT_E_BUFFER_TOO_LARGE
        // because the abandoned buffer lease had been released as fully written. Waiting on the
        // event rather than reading `reported` after a sleep keeps the cross-thread read ordered.
        stopped.Reset();
        player.Play();
        if (stopped.Wait(500))
            Assert.Fail($"the second Play() stopped unexpectedly: {reported}");

        Assert.That(player.PlaybackState, Is.EqualTo(PlaybackState.Playing));
        player.Stop();
        Assert.That(player.PlaybackState, Is.EqualTo(PlaybackState.Stopped));
    }

    [Test]
    public void SourceEndingBeforeFirstBuffer_ReportsStopped()
    {
        // The other path that skipped the old shutdown code: the initial FillBuffer returning true
        // takes an early return out of the play thread before the audio client is ever started.
        using var player = new WasapiPlayerBuilder().Build();
        using var stopped = new ManualResetEventSlim(false);
        Exception reported = null;
        player.PlaybackStopped += (_, e) => { reported = e.Exception; stopped.Set(); };

        player.Init(new EmptyProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)));
        player.Play();

        Assert.That(stopped.Wait(5000), Is.True, "PlaybackStopped never fired");
        Assert.That(reported, Is.Null);
        Assert.That(player.PlaybackState, Is.EqualTo(PlaybackState.Stopped),
            "a source that ended before the first buffer left PlaybackState reporting Playing");
    }
}
