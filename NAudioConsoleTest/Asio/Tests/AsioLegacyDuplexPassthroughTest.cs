using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

/// <summary>
/// Regression test for the legacy <see cref="AsioOut"/> duplex pattern: subscribe to
/// <see cref="AsioOut.AudioAvailable"/>, write directly to the output <see cref="IntPtr"/>s in
/// driver-native format, and set <c>WrittenToOutputBuffers = true</c> to short-circuit the
/// playback convertor. Mirrors <see cref="AsioDuplexPassthroughTest"/> but exercises the
/// legacy API surface.
/// </summary>
sealed class AsioLegacyDuplexPassthroughTest : IConsoleTest
{
    public string Id => "Asio.LegacyDuplexPassthrough";
    public string Description => "Legacy AsioOut duplex (AudioAvailable + WrittenToOutputBuffers)";
    public MenuPath? MenuLocation =>
        new("ASIO (AsioDevice — NAudio 3)",
            "Legacy AsioOut duplex (AudioAvailable + WrittenToOutputBuffers)",
            Group: "Duplex", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
        new("inputOffset", typeof(int), Required: false, Default: 0,
            Help: "starting input channel (2 channels used, contiguous)"),
        new("outputOffset", typeof(int), Required: false, Default: 0,
            Help: "starting output channel (2 channels used, contiguous)"),
        new("gain", typeof(float), Required: false, Default: 0.5f, Help: "passthrough gain (0..1)"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(10)),
    ];

    public TestResult Run(TestContext ctx)
    {
        var driverName = ctx.Get<string>("driver");
        int driverInputs, driverOutputs;
        using (var probe = AsioDrivers.TryOpen(driverName))
        {
            if (probe is null) return TestResult.Fail($"ASIO driver not installed: {driverName}");
            driverInputs = probe.Capabilities.NbInputChannels;
            driverOutputs = probe.Capabilities.NbOutputChannels;
        }

        if (driverInputs < 2 || driverOutputs < 2)
            return TestResult.Fail($"Driver needs ≥2 inputs and outputs (has {driverInputs} in, {driverOutputs} out)");

        var inputOffset = ctx.Get<int>("inputOffset");
        var outputOffset = ctx.Get<int>("outputOffset");
        if (inputOffset < 0 || inputOffset > driverInputs - 2)
            return TestResult.Fail($"inputOffset {inputOffset} out of range (0..{driverInputs - 2})");
        if (outputOffset < 0 || outputOffset > driverOutputs - 2)
            return TestResult.Fail($"outputOffset {outputOffset} out of range (0..{driverOutputs - 2})");

        var gain = ctx.Get<float>("gain");
        var duration = ctx.Get<TimeSpan>("duration");
        const int channelCount = 2;

        AnsiConsole.MarkupLine($"[yellow]⚠ Routes input straight to outputs at {gain:P0} gain. Watch for feedback.[/]\n");

        var asio = new AsioOut(driverName)
        {
            ChannelOffset = outputOffset,
            InputChannelOffset = inputOffset,
        };
        var sampleRate = asio.IsSampleRateSupported(48000) ? 48000
            : asio.IsSampleRateSupported(44100) ? 44100
            : 0;
        if (sampleRate == 0)
        {
            asio.Dispose();
            return TestResult.Fail("Driver supports neither 48 kHz nor 44.1 kHz");
        }

        var silentSource = new SilenceProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount));
        var peaks = new float[channelCount];        // running max
        var currentPeaks = new float[channelCount]; // per-callback for the live meter
        var handlerFired = false;
        var shortCircuitWorked = false;

        asio.AudioAvailable += (_, e) =>
        {
            handlerFired = true;
            for (var ch = 0; ch < channelCount; ch++)
            {
                var peak = NativePassthroughChannel(
                    e.InputBuffers[ch], e.OutputBuffers[ch], e.SamplesPerBuffer, e.AsioSampleType, gain);
                currentPeaks[ch] = peak;
                if (peak > peaks[ch]) peaks[ch] = peak;
            }
            e.WrittenToOutputBuffers = true;
            shortCircuitWorked = true;
        };

        try
        {
            asio.InitRecordAndPlayback(silentSource, channelCount, -1);
        }
        catch (Exception ex)
        {
            asio.Dispose();
            return TestResult.Fail($"Init failed: {ex.Message}");
        }

        AnsiConsole.MarkupLine($"[grey]Input format:[/] {Markup.Escape(asio.AsioInputChannelName(inputOffset))}");
        AnsiConsole.MarkupLine($"[grey]Buffer:[/] {asio.FramesPerBuffer} frames @ {sampleRate} Hz, " +
                               $"output latency {asio.PlaybackLatency} frames\n");

        var stopped = new ManualResetEventSlim();
        asio.PlaybackStopped += (_, _) => stopped.Set();
        asio.Play();

        var start = DateTime.UtcNow;
        var cancelled = false;

        if (ctx.Interactive)
        {
            using var meter = new LiveMeterRenderer(channelCount);
            while (!stopped.IsSet && DateTime.UtcNow - start < duration)
            {
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
                if (ctx.Cancellation.WaitHandle.WaitOne(100)) { cancelled = true; break; }
                for (var i = 0; i < channelCount; i++)
                    meter.Update(i, $"in {inputOffset + i} → out {outputOffset + i}", currentPeaks[i]);
            }
        }
        else
        {
            while (!stopped.IsSet && DateTime.UtcNow - start < duration)
            {
                if (ctx.Cancellation.WaitHandle.WaitOne(100)) { cancelled = true; break; }
            }
        }
        try { asio.Stop(); } catch { }
        stopped.Wait(TimeSpan.FromSeconds(2));
        asio.Dispose();

        var diagnostics = new Dictionary<string, string>
        {
            ["driver"] = driverName,
            ["inputOffset"] = inputOffset.ToString(),
            ["outputOffset"] = outputOffset.ToString(),
            ["sampleRate"] = sampleRate.ToString(),
            ["handlerFired"] = handlerFired ? "true" : "false",
            ["shortCircuitWorked"] = shortCircuitWorked ? "true" : "false",
            ["peakCh0"] = peaks[0].ToString("F4"),
            ["peakCh1"] = peaks[1].ToString("F4"),
        };

        if (cancelled) return TestResult.Skipped("Cancelled", diagnostics);
        if (!handlerFired) return TestResult.Fail("AudioAvailable handler never fired", diagnostics);
        if (!shortCircuitWorked) return TestResult.Fail("WrittenToOutputBuffers short-circuit never executed", diagnostics);
        return TestResult.Pass("AudioAvailable + WrittenToOutputBuffers path exercised cleanly", diagnostics);
    }

    /// <summary>
    /// Reads <paramref name="frames"/> samples from <paramref name="input"/>, scales by
    /// <paramref name="gain"/>, writes to <paramref name="output"/>, and returns the absolute peak.
    /// All buffers are in driver-native ASIO format.
    /// </summary>
    private static unsafe float NativePassthroughChannel(IntPtr input, IntPtr output, int frames,
        AsioSampleType type, float gain)
    {
        var peak = 0f;
        switch (type)
        {
            case AsioSampleType.Int16LSB:
            {
                var src = (short*)input;
                var dst = (short*)output;
                for (var n = 0; n < frames; n++)
                {
                    var s = (int)(src[n] * gain);
                    if (s > short.MaxValue) s = short.MaxValue;
                    else if (s < short.MinValue) s = short.MinValue;
                    dst[n] = (short)s;
                    var a = src[n] / (float)short.MaxValue;
                    a = a < 0 ? -a : a;
                    if (a > peak) peak = a;
                }
                break;
            }
            case AsioSampleType.Int24LSB:
            {
                var src = (byte*)input;
                var dst = (byte*)output;
                for (var n = 0; n < frames; n++)
                {
                    var s = src[0] | (src[1] << 8) | ((sbyte)src[2] << 16);
                    var o = (int)(s * gain);
                    if (o > 0x7FFFFF) o = 0x7FFFFF;
                    else if (o < -0x800000) o = -0x800000;
                    dst[0] = (byte)o;
                    dst[1] = (byte)(o >> 8);
                    dst[2] = (byte)(o >> 16);
                    var a = s / 8388608f;
                    a = a < 0 ? -a : a;
                    if (a > peak) peak = a;
                    src += 3; dst += 3;
                }
                break;
            }
            case AsioSampleType.Int32LSB:
            {
                var src = (int*)input;
                var dst = (int*)output;
                for (var n = 0; n < frames; n++)
                {
                    var s = (long)(src[n] * gain);
                    if (s > int.MaxValue) s = int.MaxValue;
                    else if (s < -int.MaxValue) s = -int.MaxValue;
                    dst[n] = (int)s;
                    var a = src[n] / (float)int.MaxValue;
                    a = a < 0 ? -a : a;
                    if (a > peak) peak = a;
                }
                break;
            }
            case AsioSampleType.Float32LSB:
            {
                var src = (float*)input;
                var dst = (float*)output;
                for (var n = 0; n < frames; n++)
                {
                    var s = src[n];
                    dst[n] = s * gain;
                    var a = s < 0 ? -s : s;
                    if (a > peak) peak = a;
                }
                break;
            }
            default:
                Buffer.MemoryCopy((void*)input, (void*)output, frames * 4, frames * 4);
                break;
        }
        return peak;
    }
}
