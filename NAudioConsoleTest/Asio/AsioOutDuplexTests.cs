using NAudio.Wave;
using NAudio.Wave.Asio;
using Spectre.Console;

namespace NAudioConsoleTest.Asio;

/// <summary>
/// Regression test for the legacy <see cref="AsioOut"/> duplex pattern: subscribe to <see cref="AsioOut.AudioAvailable"/>,
/// write directly to the output IntPtrs from inside the handler, and set <c>WrittenToOutputBuffers = true</c> so the
/// playback convertor is short-circuited. Mirrors <see cref="AsioDuplexTests.Passthrough"/> but exercises the legacy API.
/// </summary>
static class AsioOutDuplexTests
{
    private const float DefaultGain = 0.5f;

    public static void LegacyAudioAvailablePassthrough()
    {
        AnsiConsole.MarkupLine("[bold]Legacy AsioOut duplex passthrough (AudioAvailable + WrittenToOutputBuffers)[/]\n");
        AnsiConsole.MarkupLine("[yellow]⚠ Routes input straight to outputs at 50% gain. Watch for feedback.[/]\n");

        var driverName = AsioDeviceSelector.SelectDriver();
        if (driverName == null) return;

        // Snapshot capabilities via AsioDevice — AsioOut itself only exposes a subset, and we want to validate the user's
        // contiguous-channel choice up-front before spending time on Init.
        int driverInputs, driverOutputs;
        using (var probe = AsioDevice.Open(driverName))
        {
            driverInputs = probe.Capabilities.NbInputChannels;
            driverOutputs = probe.Capabilities.NbOutputChannels;
        }

        if (driverInputs < 2 || driverOutputs < 2)
        {
            AnsiConsole.MarkupLine($"[red]Driver needs at least 2 inputs and 2 outputs (has {driverInputs} in, {driverOutputs} out).[/]");
            PressAnyKey();
            return;
        }

        int inputOffset = AnsiConsole.Prompt(
            new TextPrompt<int>($"Input channel offset (0..{driverInputs - 2}, count fixed at 2):")
                .DefaultValue(0)
                .Validate(v => v >= 0 && v <= driverInputs - 2 ? ValidationResult.Success() : ValidationResult.Error()));
        int outputOffset = AnsiConsole.Prompt(
            new TextPrompt<int>($"Output channel offset (0..{driverOutputs - 2}, count fixed at 2):")
                .DefaultValue(0)
                .Validate(v => v >= 0 && v <= driverOutputs - 2 ? ValidationResult.Success() : ValidationResult.Error()));

        const int channelCount = 2;

        // Silent stereo source — the playback convertor never runs because we set WrittenToOutputBuffers = true.
        // We still need a wave provider because that's the legacy AsioOut contract for record-and-play.
        var asio = new AsioOut(driverName)
        {
            ChannelOffset = outputOffset,
            InputChannelOffset = inputOffset,
        };
        int sampleRate = asio.IsSampleRateSupported(48000) ? 48000 : 44100;
        if (!asio.IsSampleRateSupported(sampleRate))
        {
            AnsiConsole.MarkupLine($"[red]Driver does not support 48kHz or 44.1kHz.[/]");
            asio.Dispose();
            PressAnyKey();
            return;
        }

        var silentSource = new SilenceProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount));

        var peaks = new float[channelCount];
        bool handlerFired = false;
        bool shortCircuitWorked = false;

        asio.AudioAvailable += (_, e) =>
        {
            handlerFired = true;
            // Native passthrough: copy each input buffer to its corresponding output buffer with gain, in-place per format.
            for (int ch = 0; ch < channelCount; ch++)
            {
                float peak = NativePassthroughChannel(
                    e.InputBuffers[ch], e.OutputBuffers[ch], e.SamplesPerBuffer, e.AsioSampleType, DefaultGain);
                peaks[ch] = peak;
            }
            e.WrittenToOutputBuffers = true;
            shortCircuitWorked = true;
        };

        try
        {
            // recordChannels = 2 → enables the input path. recordOnlySampleRate is ignored when waveProvider is non-null.
            asio.InitRecordAndPlayback(silentSource, channelCount, -1);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Init failed: {Markup.Escape(ex.Message)}[/]");
            asio.Dispose();
            PressAnyKey();
            return;
        }

        AnsiConsole.MarkupLine($"[grey]Input format: {asio.AsioInputChannelName(inputOffset)} ({DescribeFormat(asio)})[/]");
        AnsiConsole.MarkupLine($"[grey]Buffer: {asio.FramesPerBuffer} frames @ {sampleRate} Hz, output latency {asio.PlaybackLatency} frames[/]");
        AnsiConsole.MarkupLine("[dim]ESC to stop.[/]\n");

        var stopped = new ManualResetEventSlim();
        asio.PlaybackStopped += (_, _) => stopped.Set();

        asio.Play();

        // Reserve display rows.
        int topRow = Console.CursorTop;
        for (int i = 0; i < channelCount; i++) Console.WriteLine();

        try
        {
            while (!stopped.IsSet)
            {
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
                {
                    asio.Stop();
                    break;
                }
                for (int ch = 0; ch < channelCount; ch++)
                {
                    float peak = peaks[ch];
                    float db = peak > 1e-6f ? 20f * MathF.Log10(peak) : -999f;
                    Console.SetCursorPosition(0, topRow + ch);
                    Console.Write($"  in {inputOffset + ch,2} → out {outputOffset + ch,2}: {BarFor(peak)} {db,7:0.0} dBFS   ");
                }
                Thread.Sleep(80);
            }
        }
        finally
        {
            Console.SetCursorPosition(0, Math.Min(topRow + channelCount, Console.BufferHeight - 1));
            Console.WriteLine();
        }

        stopped.Wait(TimeSpan.FromSeconds(2));
        asio.Dispose();

        // Verdict.
        AnsiConsole.MarkupLine(handlerFired
            ? "[green]✓ AudioAvailable handler fired.[/]"
            : "[red]✗ AudioAvailable handler never fired.[/]");
        AnsiConsole.MarkupLine(shortCircuitWorked
            ? "[green]✓ WrittenToOutputBuffers short-circuit ran (verify by ear: passthrough, not silence).[/]"
            : "[red]✗ Short-circuit path never executed.[/]");

        PressAnyKey();
    }

    /// <summary>
    /// Reads <paramref name="frames"/> samples from <paramref name="input"/>, scales by <paramref name="gain"/>,
    /// writes to <paramref name="output"/>, returning the absolute peak. All buffers are in driver-native ASIO format.
    /// </summary>
    private static unsafe float NativePassthroughChannel(IntPtr input, IntPtr output, int frames, AsioSampleType type, float gain)
    {
        float peak = 0f;
        switch (type)
        {
            case AsioSampleType.Int16LSB:
                {
                    var src = (short*)input;
                    var dst = (short*)output;
                    for (int n = 0; n < frames; n++)
                    {
                        int s = (int)(src[n] * gain);
                        if (s > short.MaxValue) s = short.MaxValue;
                        else if (s < short.MinValue) s = short.MinValue;
                        dst[n] = (short)s;
                        float a = src[n] / (float)short.MaxValue;
                        a = a < 0 ? -a : a;
                        if (a > peak) peak = a;
                    }
                    break;
                }
            case AsioSampleType.Int24LSB:
                {
                    var src = (byte*)input;
                    var dst = (byte*)output;
                    for (int n = 0; n < frames; n++)
                    {
                        int s = src[0] | (src[1] << 8) | ((sbyte)src[2] << 16);
                        int o = (int)(s * gain);
                        // Int24 range is [-0x800000, 0x7FFFFF].
                        if (o > 0x7FFFFF) o = 0x7FFFFF;
                        else if (o < -0x800000) o = -0x800000;
                        dst[0] = (byte)o;
                        dst[1] = (byte)(o >> 8);
                        dst[2] = (byte)(o >> 16);
                        float a = s / 8388608f;
                        a = a < 0 ? -a : a;
                        if (a > peak) peak = a;
                        src += 3;
                        dst += 3;
                    }
                    break;
                }
            case AsioSampleType.Int32LSB:
                {
                    var src = (int*)input;
                    var dst = (int*)output;
                    for (int n = 0; n < frames; n++)
                    {
                        long s = (long)(src[n] * gain);
                        if (s > int.MaxValue) s = int.MaxValue;
                        else if (s < -int.MaxValue) s = -int.MaxValue;
                        dst[n] = (int)s;
                        float a = src[n] / (float)int.MaxValue;
                        a = a < 0 ? -a : a;
                        if (a > peak) peak = a;
                    }
                    break;
                }
            case AsioSampleType.Float32LSB:
                {
                    var src = (float*)input;
                    var dst = (float*)output;
                    for (int n = 0; n < frames; n++)
                    {
                        float s = src[n];
                        dst[n] = s * gain;
                        float a = s < 0 ? -s : s;
                        if (a > peak) peak = a;
                    }
                    break;
                }
            default:
                // Fall back to byte memcpy at unity gain — caller will see no per-channel peak (left at 0).
                Buffer.MemoryCopy((void*)input, (void*)output, frames * 4, frames * 4);
                break;
        }
        return peak;
    }

    private static string DescribeFormat(AsioOut asio)
    {
        // The legacy event uses InputChannelInfos[0].type for all input buffers — surface that to the user.
        return asio.OutputWaveFormat is { } wf ? $"output {wf}" : "format unknown";
    }

    private static string BarFor(float peak)
    {
        const int width = 16;
        int filled = Math.Min(width, (int)(peak * width));
        return new string('█', filled) + new string('░', width - filled);
    }

    private static void PressAnyKey()
    {
        AnsiConsole.MarkupLine("\n[dim]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }
}
