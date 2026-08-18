using System;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
public class WdlResamplingSampleProviderTests
{
    // Property-based tests: validate physical correctness of the resampler, not exact byte-for-byte
    // output. These survive future upstream syncs because legitimate algorithm improvements should
    // still satisfy the same physical properties.

    [TestCase(8000, 16000)]
    [TestCase(8000, 22050)]
    [TestCase(8000, 32000)]
    [TestCase(8000, 44100)]
    [TestCase(8000, 48000)]
    [TestCase(8000, 96000)]
    [TestCase(44100, 8000)]
    [TestCase(44100, 16000)]
    [TestCase(44100, 22050)]
    [TestCase(44100, 32000)]
    [TestCase(44100, 48000)]
    [TestCase(44100, 96000)]
    [TestCase(48000, 8000)]
    [TestCase(48000, 16000)]
    [TestCase(48000, 22050)]
    [TestCase(48000, 32000)]
    [TestCase(48000, 44100)]
    [TestCase(48000, 96000)]
    public void OutputLengthMatchesRatio(int from, int to)
    {
        const int seconds = 5;
        var output = ResampleSawtooth(from, to, channels: 1, seconds: seconds);
        int expected = to * seconds;
        // Allow a small tolerance for filter latency at the start/end (a few hundred samples).
        int tolerance = Math.Max(512, to / 50);
        Assert.That(output.Length, Is.EqualTo(expected).Within(tolerance),
            $"{from} -> {to}: expected ~{expected} output samples, got {output.Length}");
    }

    [TestCase(8000, 16000)]
    [TestCase(44100, 48000)]
    [TestCase(48000, 44100)]
    [TestCase(48000, 16000)]
    [TestCase(96000, 48000)]
    public void OutputContainsNoNaNOrInf(int from, int to)
    {
        var output = ResampleSawtooth(from, to, channels: 2, seconds: 1);
        for (int i = 0; i < output.Length; i++)
        {
            if (float.IsNaN(output[i]) || float.IsInfinity(output[i]))
                Assert.Fail($"Sample {i} is NaN/Inf at {from}->{to}");
        }
    }

    [TestCase(48000)]
    [TestCase(44100)]
    public void OneToOneRatioIsNearIdentity(int rate)
    {
        // Resampling at 1:1 should pass the signal through largely unchanged (modulo filter latency
        // and any small numerical error). Compare RMS of input and output.
        var input = GenerateSine(rate, channels: 1, seconds: 1, frequency: 1000, gain: 0.5);
        var source = new ArraySampleProvider(input, rate, 1);
        var resampler = new WdlResamplingSampleProvider(source, rate);

        var output = ReadAll(resampler, rate * 2);
        double inRms = Rms(input);
        double outRms = Rms(output, skipStart: 1024); // skip filter warmup
        Assert.That(outRms, Is.EqualTo(inRms).Within(0.05),
            $"1:1 RMS drift: in={inRms:F4} out={outRms:F4}");
    }

    [Test]
    public void DcInputProducesDcOutput()
    {
        // A constant input should resample to a constant (within filter response near DC).
        // This is a basic correctness check that catches gross filter design errors.
        const int from = 48000;
        const int to = 16000;
        const float dc = 0.25f;
        var input = new float[from * 2]; // 2 seconds of DC
        Array.Fill(input, dc);

        var source = new ArraySampleProvider(input, from, 1);
        var resampler = new WdlResamplingSampleProvider(source, to);
        var output = ReadAll(resampler, to * 4);

        // After settling, every sample should be close to the input DC value.
        // Skip the first ~200 ms while filters warm up.
        int skip = to / 5;
        Assert.That(output.Length, Is.GreaterThan(skip + 1000));
        for (int i = skip; i < output.Length; i++)
        {
            Assert.That(output[i], Is.EqualTo(dc).Within(0.01),
                $"DC output drift at sample {i}: got {output[i]}");
        }
    }

    [Test]
    public void SineBelowNyquistSurvivesDownsample()
    {
        // A 1 kHz sine fed through 48k -> 16k must come out with comparable amplitude.
        // 1 kHz is well below the 8 kHz output Nyquist, so attenuation should be small.
        const int from = 48000;
        const int to = 16000;
        var input = GenerateSine(from, channels: 1, seconds: 2, frequency: 1000, gain: 0.5);

        var source = new ArraySampleProvider(input, from, 1);
        var resampler = new WdlResamplingSampleProvider(source, to);
        var output = ReadAll(resampler, to * 3);

        double inRms = Rms(input);
        double outRms = Rms(output, skipStart: to / 5);
        // Allow 10% RMS drift — pass-band ripple plus filter transition shouldn't exceed this.
        Assert.That(outRms, Is.EqualTo(inRms).Within(0.1),
            $"In-band sine attenuated too much: in={inRms:F4} out={outRms:F4}");
    }

    [Test]
    public void StereoChannelsRemainSeparate()
    {
        // Feed silence to the left channel and a sine to the right. After resampling, the left
        // channel should still be silent (no leakage from right channel into left).
        const int from = 48000;
        const int to = 22050;
        const int seconds = 1;
        var input = new float[from * seconds * 2];
        // Left = 0, right = 1 kHz sine
        double phase = 0;
        double phaseInc = 2 * Math.PI * 1000.0 / from;
        for (int i = 0; i < from * seconds; i++)
        {
            input[i * 2] = 0;
            input[i * 2 + 1] = (float)(0.5 * Math.Sin(phase));
            phase += phaseInc;
        }

        var source = new ArraySampleProvider(input, from, 2);
        var resampler = new WdlResamplingSampleProvider(source, to);
        var output = ReadAll(resampler, to * 2 * 2);

        // Extract left channel and verify it's silent.
        int frames = output.Length / 2;
        double leftEnergy = 0;
        for (int i = 0; i < frames; i++)
            leftEnergy += output[i * 2] * output[i * 2];
        leftEnergy = Math.Sqrt(leftEnergy / frames);
        Assert.That(leftEnergy, Is.LessThan(0.001),
            $"Left channel leaked from right: RMS={leftEnergy:F6}");
    }

    // ---- Direct WdlResampler tests (exercising sinc mode where the windowing fix lives) ----

    [Test]
    public void SincModeAttenuatesAboveNyquist()
    {
        // The 2015 Blackman-Harris fix (cos(6*) -> cos(3*)) is in BuildLowPass, only used in sinc
        // mode. This test verifies that a tone above the new Nyquist is heavily attenuated when
        // downsampling, which the broken windowing function would otherwise alias back into the
        // pass band.
        const int from = 48000;
        const int to = 16000;
        const int seconds = 2;

        // 12 kHz tone — well above 8 kHz output Nyquist. Should be killed by anti-alias filter.
        var input = GenerateSine(from, channels: 1, seconds: seconds, frequency: 12000, gain: 0.5);
        var output = RunSincResampler(input, from, to, channels: 1);

        double outRms = Rms(output, skipStart: to / 5);
        // Brick-wall isn't realistic; require at least 30 dB attenuation (factor of ~31).
        // Input RMS for a 0.5-amplitude sine is ~0.354.
        const double expectedMaxRms = 0.354 / 30.0;
        Assert.That(outRms, Is.LessThan(expectedMaxRms),
            $"Above-Nyquist tone not sufficiently attenuated: RMS={outRms:F4}, " +
            $"expected < {expectedMaxRms:F4} (>=30 dB attenuation)");
    }

    [Test]
    public void SincModePassesBelowNyquist()
    {
        // Counterpart to the above: an in-band tone in sinc mode should survive with low loss.
        const int from = 48000;
        const int to = 16000;
        var input = GenerateSine(from, channels: 1, seconds: 2, frequency: 1000, gain: 0.5);
        var output = RunSincResampler(input, from, to, channels: 1);

        double inRms = Rms(input);
        double outRms = Rms(output, skipStart: to / 5);
        Assert.That(outRms, Is.EqualTo(inRms).Within(0.05),
            $"In-band sine in sinc mode lost too much amplitude: in={inRms:F4} out={outRms:F4}");
    }

    [Test]
    public void FeedModeWithSmallInputChunksProducesExpectedOutput()
    {
        // Exercises the 2016 feed-mode accounting fix. In feed (input-driven) mode we hand
        // ResamplePrepare an input count and ResampleOut produces however many output samples that
        // input maps to. Without the clamp on isrcpos, m_fracpos drifts and output count is wrong.
        const int from = 48000;
        const int to = 44100;
        const int totalInputSamples = from * 2; // 2 seconds

        var resampler = new WdlResampler();
        resampler.SetMode(true, 2, false);
        resampler.SetFilterParms();
        resampler.SetFeedMode(true); // input-driven
        resampler.SetRates(from, to);

        var input = GenerateSine(from, channels: 1, seconds: 2, frequency: 440, gain: 0.5);
        var outBuf = new float[to * 4];
        int totalOut = 0;
        int totalIn = 0;
        var rng = new Random(42);

        while (totalIn < totalInputSamples)
        {
            int chunk = Math.Min(rng.Next(1, 257), totalInputSamples - totalIn);
            int needed = resampler.ResamplePrepare(chunk, 1, out Span<float> inSpan);
            Assert.That(needed, Is.EqualTo(chunk),
                $"In feed mode ResamplePrepare must return the input count we supplied (got {needed}, expected {chunk})");
            input.AsSpan(totalIn, chunk).CopyTo(inSpan);
            int produced = resampler.ResampleOut(outBuf.AsSpan(totalOut), chunk, outBuf.Length - totalOut, 1);
            totalOut += produced;
            totalIn += chunk;
        }

        int expected = (int)((double)totalInputSamples * to / from);
        // Feed-mode count should be near-exact, not drifting.
        Assert.That(totalOut, Is.EqualTo(expected).Within(2),
            $"feed-mode output count drift: expected ~{expected}, got {totalOut}");
        for (int i = 0; i < totalOut; i++)
            Assert.That(float.IsNaN(outBuf[i]) || float.IsInfinity(outBuf[i]), Is.False);
    }

    // --- Under-fed source (issue #1412) -------------------------------------------------------
    // WdlResamplingSampleProvider is output-driven: ResamplePrepare asks for however much input the
    // requested output needs, and a pull-based source (a BufferedWaveProvider fed by a capture
    // callback, say) routinely has less than that. ResampleOut then pads with zeros, produces the
    // output it can, and trims the padding-derived samples off the count. These tests pin that the
    // padding is not also charged against the caller's next read.

    [TestCase(160)]
    [TestCase(161)]
    [TestCase(192)]
    [TestCase(320)]
    [TestCase(2048)]
    public void OverRequestingFromAnUnderfedSourceStillReturnsAllAvailableOutput(int framesRequested)
    {
        // Reported repro: 48 kHz mono float in, 16 kHz out, 480 input frames (10 ms) handed over
        // per round, which is exactly 160 output frames. Every request size must keep yielding 160
        // -- before the fix, anything above 160 decayed and >= ~2x collapsed to 0 permanently.
        const int from = 48000;
        const int to = 16000;
        const int inputFramesPerRound = 480;
        const int expectedPerRound = 160;
        const int rounds = 6;

        var source = new ChunkedSampleProvider(GenerateSine(from, 1, 1, 1000, 0.5), from, 1, inputFramesPerRound);
        var resampler = new WdlResamplingSampleProvider(source, to);
        var buffer = new float[framesRequested];

        for (int round = 0; round < rounds; round++)
        {
            source.ReleaseChunk();
            int read = resampler.Read(buffer);
            Assert.That(read, Is.EqualTo(expectedPerRound),
                $"round {round}: asked for {framesRequested} frames with {expectedPerRound} available");
        }
    }

    [TestCase(48000, 16000, 1)]
    [TestCase(44100, 16000, 1)]
    [TestCase(44100, 48000, 1)]
    [TestCase(22050, 44100, 1)]
    [TestCase(8000, 48000, 1)]
    [TestCase(44100, 44100, 1)]
    [TestCase(48000, 44100, 2)]
    [TestCase(96000, 8000, 2)]
    public void UnderfedSourceDoesNotLoseSamplesOverManyReads(int from, int to, int channels)
    {
        // Same shape as above but generalised: 10 ms of input released per round while the caller
        // always asks for far more than that. Total output must track total input by the rate
        // ratio; any per-call leakage compounds and shows up here.
        const int rounds = 200;
        int chunkFrames = from / 100;
        var input = GenerateSine(from, channels, 3, 1000, 0.5);
        var source = new ChunkedSampleProvider(input, from, channels, chunkFrames);
        var resampler = new WdlResamplingSampleProvider(source, to);
        var buffer = new float[8192 * channels];

        int totalSamples = 0;
        for (int round = 0; round < rounds; round++)
        {
            source.ReleaseChunk();
            totalSamples += resampler.Read(buffer);
        }

        int inFrames = Math.Min(rounds * chunkFrames, input.Length / channels);
        int expected = (int)((long)inFrames * to / from);
        Assert.That(totalSamples / channels, Is.EqualTo(expected).Within(2),
            $"{from} -> {to} ({channels}ch): expected ~{expected} output frames, got {totalSamples / channels}");
    }

    [TestCase(48000, 16000, 1)]
    [TestCase(48000, 24000, 2)]
    [TestCase(44100, 44100, 1)]
    public void ChunkedOverRequestedReadsMatchAWellFedRead(int from, int to, int channels)
    {
        // Restoring the sample count is not enough on its own: the fractional source position has
        // to stay aligned too, or the output drifts in phase at every short read. At integer rate
        // ratios a starved chunked read should be sample-for-sample identical to reading the same
        // signal from a source that can always fill the request.
        var input = GenerateSine(from, channels, 1, 1000, 0.5);

        var wellFed = ReadAll(new WdlResamplingSampleProvider(new ArraySampleProvider(input, from, channels), to), 4096 * channels);

        int chunkFrames = from / 100;
        var source = new ChunkedSampleProvider(input, from, channels, chunkFrames);
        var starved = new WdlResamplingSampleProvider(source, to);
        var accumulated = new System.Collections.Generic.List<float>();
        var buffer = new float[8192 * channels];
        for (int round = 0; round < 120; round++)
        {
            source.ReleaseChunk();
            int read = starved.Read(buffer);
            for (int i = 0; i < read; i++) accumulated.Add(buffer[i]);
        }

        Assert.That(accumulated.Count, Is.EqualTo(wellFed.Length),
            $"{from} -> {to}: starved read produced {accumulated.Count} samples, well-fed produced {wellFed.Length}");
        for (int i = 0; i < wellFed.Length; i++)
            Assert.That(accumulated[i], Is.EqualTo(wellFed[i]).Within(1e-6f),
                $"{from} -> {to}: sample {i} differs between starved and well-fed reads");
    }

    [TestCase(48000, 16000)]
    [TestCase(44100, 48000)]
    public void SincModeUnderfedSourceDoesNotLoseSamples(int from, int to)
    {
        // The flush path behaves differently in sinc mode: m_sincsize widens the zero padding and
        // outlatadj shifts the trim point, so cover it separately from the interpolating modes.
        var resampler = new WdlResampler();
        resampler.SetMode(false, 0, true, 64, 32);
        resampler.SetFeedMode(false);
        resampler.SetRates(from, to);

        var input = GenerateSine(from, 1, 2, 1000, 0.5);
        var outBuf = new float[4096];
        int chunkFrames = from / 100;
        int inPos = 0;
        int totalOut = 0;

        for (int round = 0; round < 150 && inPos < input.Length; round++)
        {
            int needed = resampler.ResamplePrepare(outBuf.Length, 1, out Span<float> inSpan);
            int give = Math.Min(Math.Min(needed, chunkFrames), input.Length - inPos);
            input.AsSpan(inPos, give).CopyTo(inSpan);
            inPos += give;
            totalOut += resampler.ResampleOut(outBuf, give, outBuf.Length, 1);
        }

        int expected = (int)((long)inPos * to / from);
        Assert.That(totalOut, Is.EqualTo(expected).Within(2),
            $"sinc {from} -> {to}: consumed {inPos} input frames, expected ~{expected} output frames, got {totalOut}");
    }

    [TestCase(48000, 44100)]
    [TestCase(44100, 48000)]
    [TestCase(48000, 16000)]
    public void FeedModeWithFewerSamplesThanPreparedDoesNotDrift(int from, int to)
    {
        // ResampleOut is documented as accepting fewer samples than ResamplePrepare returned ("it
        // will be flushed to produce all remaining valid samples"), which drives the same padding
        // path in input-driven mode. Supply short feeds deliberately and check the totals.
        var resampler = new WdlResampler();
        resampler.SetMode(true, 2, false);
        resampler.SetFilterParms();
        resampler.SetFeedMode(true);
        resampler.SetRates(from, to);

        var input = GenerateSine(from, 1, 2, 440, 0.5);
        var outBuf = new float[to * 4];
        var rng = new Random(7);
        int totalIn = 0;
        int totalOut = 0;

        while (totalIn < input.Length)
        {
            int prepared = Math.Min(rng.Next(64, 512), input.Length - totalIn);
            int supplied = Math.Max(1, prepared - rng.Next(0, 32));
            resampler.ResamplePrepare(prepared, 1, out Span<float> inSpan);
            input.AsSpan(totalIn, supplied).CopyTo(inSpan);
            totalOut += resampler.ResampleOut(outBuf.AsSpan(totalOut), supplied, outBuf.Length - totalOut, 1);
            totalIn += supplied;
        }

        int expected = (int)((long)totalIn * to / from);
        Assert.That(totalOut, Is.EqualTo(expected).Within(2),
            $"feed mode {from} -> {to}: supplied {totalIn} input frames, expected ~{expected} output frames, got {totalOut}");
    }

    [Test]
    public void ResamplePrepareWithoutResampleOutIsSafe()
    {
        // Documented on ResamplePrepare: "it is safe to call ResamplePrepare without calling
        // ResampleOut (the next call of ResamplePrepare will function as normal)".
        const int from = 48000;
        const int to = 16000;

        var resampler = new WdlResampler();
        resampler.SetMode(true, 2, false);
        resampler.SetFilterParms();
        resampler.SetFeedMode(false);
        resampler.SetRates(from, to);

        var input = GenerateSine(from, 1, 1, 1000, 0.5);
        var outBuf = new float[1024];

        for (int i = 0; i < 3; i++)
            resampler.ResamplePrepare(outBuf.Length, 1, out _);

        int needed = resampler.ResamplePrepare(outBuf.Length, 1, out Span<float> inSpan);
        int give = Math.Min(needed, input.Length);
        input.AsSpan(0, give).CopyTo(inSpan);
        int produced = resampler.ResampleOut(outBuf, give, outBuf.Length, 1);

        Assert.That(produced, Is.EqualTo(outBuf.Length).Within(2),
            "abandoned ResamplePrepare calls should not affect the next full cycle");
    }

    [Test]
    public void ResetRestoresStartOfStreamBehaviour()
    {
        // Reset is the documented way to reuse a resampler for a new stream; after it, the same
        // input must give the same output as a freshly constructed instance.
        const int from = 48000;
        const int to = 44100;
        var input = GenerateSine(from, 1, 1, 1000, 0.5);

        var first = new WdlResampler();
        first.SetMode(true, 2, false);
        first.SetFilterParms();
        first.SetFeedMode(false);
        first.SetRates(from, to);

        var reference = RunOutputDriven(first, input, 1024);
        first.Reset();
        var afterReset = RunOutputDriven(first, input, 1024);

        Assert.That(afterReset.Length, Is.EqualTo(reference.Length), "output length changed after Reset");
        for (int i = 0; i < reference.Length; i++)
            Assert.That(afterReset[i], Is.EqualTo(reference[i]).Within(1e-6f), $"sample {i} differs after Reset");
    }

    [Test]
    public void GetCurrentLatencyReportsSubSamplePrecision()
    {
        // The 2026 fix subtracts m_fracpos so reported latency reflects the fractional source
        // position, not just whole-sample counts. We verify this on a non-integer rate ratio
        // (48k -> 44.1k) where m_fracpos is non-zero most of the time.
        // - Without the fix: latency * sratein is always a whole-sample integer.
        // - With the fix: at least some readings are fractional.
        const int from = 48000;
        const int to = 44100;

        var resampler = new WdlResampler();
        resampler.SetMode(true, 2, false);
        resampler.SetFilterParms();
        resampler.SetFeedMode(false);
        resampler.SetRates(from, to);

        var outBuf = new float[1024];
        var inSrc = GenerateSine(from, 1, 1, 1000, 0.5);
        int srcPos = 0;
        bool sawFractional = false;
        double maxLatencySeconds = 0;

        for (int iter = 0; iter < 30; iter++)
        {
            int needed = resampler.ResamplePrepare(outBuf.Length, 1, out Span<float> inSpan);
            int avail = Math.Min(needed, inSrc.Length - srcPos);
            inSrc.AsSpan(srcPos, avail).CopyTo(inSpan);
            srcPos += avail;
            resampler.ResampleOut(outBuf, avail, outBuf.Length, 1);

            double latency = resampler.GetCurrentLatency();
            Assert.That(latency, Is.GreaterThanOrEqualTo(0.0), $"Negative latency at iter {iter}");
            if (latency > maxLatencySeconds) maxLatencySeconds = latency;

            double latencySamples = latency * from;
            double frac = latencySamples - Math.Floor(latencySamples);
            if (frac > 0.05 && frac < 0.95) sawFractional = true;
        }

        Assert.That(sawFractional, Is.True,
            "Expected at least one fractional latency reading at non-integer rate ratio.");
        Assert.That(maxLatencySeconds, Is.LessThan(0.1),
            $"Latency unexpectedly large: {maxLatencySeconds * 1000:F1} ms");
    }

    [Test]
    public void ChannelCountChangeMidStreamKeepsChannelsAligned()
    {
        // 2022 upstream fix (wdl_rs_reinterleave_buffer): when the channel count changes
        // while input samples are still buffered, the retained samples must be reinterleaved
        // from the old layout to the new one. We use point-sampling mode (no interpolation or
        // anti-alias filtering, so output is a direct copy of input) and constant per-channel
        // DC, so any mis-reinterleaved buffered sample shows up directly in the output. Then
        // we switch channel counts mid-stream, decreasing then increasing.
        const int from = 8000;
        const int to = 48000; // upsample so a few input frames stay buffered between calls

        var resampler = new WdlResampler();
        resampler.SetMode(false, 0, false); // point sampling
        resampler.SetFeedMode(false);
        resampler.SetRates(from, to);

        // Phase 1: stereo, L = +0.5 DC, R = -0.5 DC. Leaves a few stereo frames buffered.
        PumpDc(resampler, nch: 2, dc: new[] { 0.5f, -0.5f }, cycles: 6, outFramesPerCycle: 256);

        // Phase 2: switch to mono (decreasing). The buffered stereo frames must be repacked
        // to channel 0 (+0.5). With the fix, every mono sample is the buffered L level or the
        // fed level (both >= 0.25); without it, the buffered right channel (-0.5) leaks out.
        var mono = PumpDc(resampler, nch: 1, dc: new[] { 0.25f }, cycles: 6, outFramesPerCycle: 256);
        foreach (var v in mono)
        {
            Assert.That(float.IsFinite(v), Is.True, "non-finite mono output");
            Assert.That(v, Is.GreaterThanOrEqualTo(0.2f),
                $"mono output dipped to {v}: the buffered right channel (-0.5) leaked through a missing reinterleave");
        }
        AssertTailDc(mono, 1, new[] { 0.25f });

        // Phase 3: switch to 3 channels (increasing). The buffered mono frames must be
        // repacked into channel 0, with the two new channels zero-filled.
        var three = PumpDc(resampler, nch: 3, dc: new[] { 0.1f, 0.2f, 0.3f }, cycles: 6, outFramesPerCycle: 256);
        AssertTailDc(three, 3, new[] { 0.1f, 0.2f, 0.3f });
    }

    // ---- helpers ----

    private static float[] PumpDc(WdlResampler resampler, int nch, float[] dc, int cycles, int outFramesPerCycle)
    {
        var outAll = new System.Collections.Generic.List<float>();
        for (int c = 0; c < cycles; c++)
        {
            int needed = resampler.ResamplePrepare(outFramesPerCycle, nch, out Span<float> inSpan);
            for (int f = 0; f < needed; f++)
                for (int ch = 0; ch < nch; ch++)
                    inSpan[f * nch + ch] = dc[ch];
            var outBuf = new float[outFramesPerCycle * nch];
            int produced = resampler.ResampleOut(outBuf, needed, outFramesPerCycle, nch);
            for (int i = 0; i < produced * nch; i++) outAll.Add(outBuf[i]);
        }
        return outAll.ToArray();
    }

    private static void AssertTailDc(float[] data, int nch, float[] dc)
    {
        int frames = data.Length / nch;
        Assert.That(frames, Is.GreaterThan(200), "not enough output produced to assess steady state");
        int start = frames * 3 / 4; // assess the settled tail, past any filter warmup/transition
        for (int f = start; f < frames; f++)
            for (int ch = 0; ch < nch; ch++)
            {
                float v = data[f * nch + ch];
                Assert.That(float.IsNaN(v) || float.IsInfinity(v), Is.False, $"non-finite output at frame {f} ch {ch}");
                Assert.That(v, Is.EqualTo(dc[ch]).Within(0.02f), $"ch {ch} DC drift at frame {f}: got {v}");
            }
    }

    private static float[] ResampleSawtooth(int from, int to, int channels, int seconds)
    {
        var gen = new SignalGenerator(from, channels)
        {
            Type = SignalGeneratorType.SawTooth,
            Frequency = 512,
            Gain = 0.3
        };
        var offset = new OffsetSampleProvider(gen) { TakeSamples = from * channels * seconds };
        var resampler = new WdlResamplingSampleProvider(offset, to);
        return ReadAll(resampler, to * channels);
    }

    private static float[] GenerateSine(int rate, int channels, int seconds, double frequency, double gain)
    {
        var buf = new float[rate * channels * seconds];
        double phase = 0;
        double phaseInc = 2 * Math.PI * frequency / rate;
        for (int i = 0; i < rate * seconds; i++)
        {
            float v = (float)(gain * Math.Sin(phase));
            for (int c = 0; c < channels; c++)
                buf[i * channels + c] = v;
            phase += phaseInc;
        }
        return buf;
    }

    private static float[] RunOutputDriven(WdlResampler resampler, float[] input, int outFrames)
    {
        var output = new System.Collections.Generic.List<float>();
        var outBuf = new float[outFrames];
        int inPos = 0;
        while (inPos < input.Length)
        {
            int needed = resampler.ResamplePrepare(outFrames, 1, out Span<float> inSpan);
            int give = Math.Min(needed, input.Length - inPos);
            input.AsSpan(inPos, give).CopyTo(inSpan);
            inPos += give;
            int produced = resampler.ResampleOut(outBuf, give, outFrames, 1);
            for (int i = 0; i < produced; i++) output.Add(outBuf[i]);
        }
        return output.ToArray();
    }

    private static float[] ReadAll(ISampleProvider source, int chunkSize)
    {
        var output = new System.Collections.Generic.List<float>();
        var buf = new float[chunkSize];
        while (true)
        {
            int read = source.Read(buf.AsSpan());
            if (read <= 0) break;
            for (int i = 0; i < read; i++) output.Add(buf[i]);
            if (output.Count > 10_000_000) break; // safety cap
        }
        return output.ToArray();
    }

    private static double Rms(float[] data, int skipStart = 0)
    {
        if (data.Length <= skipStart) return 0;
        double sum = 0;
        for (int i = skipStart; i < data.Length; i++) sum += data[i] * data[i];
        return Math.Sqrt(sum / (data.Length - skipStart));
    }

    private static float[] RunSincResampler(float[] input, int from, int to, int channels)
    {
        var resampler = new WdlResampler();
        // sinc mode: filter size 64, oversample 32 — exercises BuildLowPass where the
        // Blackman-Harris windowing fix lives.
        resampler.SetMode(false, 0, true, 64, 32);
        resampler.SetFeedMode(false);
        resampler.SetRates(from, to);

        int outFrames = (int)((long)(input.Length / channels) * to / from) + 1024;
        var output = new float[outFrames * channels];
        int outPos = 0;
        int inPos = 0;
        int totalInFrames = input.Length / channels;

        while (outPos < output.Length)
        {
            int wantOutFrames = Math.Min(1024, (output.Length - outPos) / channels);
            if (wantOutFrames <= 0) break;
            int needed = resampler.ResamplePrepare(wantOutFrames, channels, out Span<float> inSpan);
            int availFrames = Math.Min(needed, totalInFrames - inPos);
            if (availFrames <= 0 && needed > 0) break;
            input.AsSpan(inPos * channels, availFrames * channels).CopyTo(inSpan);
            inPos += availFrames;
            int produced = resampler.ResampleOut(output.AsSpan(outPos), availFrames, wantOutFrames, channels);
            if (produced <= 0) break;
            outPos += produced * channels;
        }

        var trimmed = new float[outPos];
        Array.Copy(output, trimmed, outPos);
        return trimmed;
    }

    /// <summary>
    /// A source that only ever hands out the frames explicitly released to it, simulating a
    /// pull-based capture pipeline where the resampler routinely asks for more than has arrived.
    /// </summary>
    private sealed class ChunkedSampleProvider : ISampleProvider
    {
        private readonly float[] data;
        private readonly int chunkFrames;
        private int pos;
        private int releasedFrames;

        public ChunkedSampleProvider(float[] data, int sampleRate, int channels, int chunkFrames)
        {
            this.data = data;
            this.chunkFrames = chunkFrames;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        }

        public WaveFormat WaveFormat { get; }

        public void ReleaseChunk() => releasedFrames += chunkFrames;

        public int Read(Span<float> buffer)
        {
            int limit = Math.Min(data.Length, releasedFrames * WaveFormat.Channels);
            int take = Math.Min(buffer.Length, limit - pos);
            if (take <= 0) return 0;
            take -= take % WaveFormat.Channels;
            data.AsSpan(pos, take).CopyTo(buffer);
            pos += take;
            return take;
        }
    }

    private sealed class ArraySampleProvider : ISampleProvider
    {
        private readonly float[] data;
        private int pos;
        public ArraySampleProvider(float[] data, int sampleRate, int channels)
        {
            this.data = data;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        }
        public WaveFormat WaveFormat { get; }
        public int Read(Span<float> buffer)
        {
            int take = Math.Min(buffer.Length, data.Length - pos);
            if (take <= 0) return 0;
            data.AsSpan(pos, take).CopyTo(buffer);
            pos += take;
            return take;
        }
    }
}
