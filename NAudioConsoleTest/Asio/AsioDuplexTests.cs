using NAudio.Wave;
using Spectre.Console;

namespace NAudioConsoleTest.Asio;

static class AsioDuplexTests
{
    // Conservative gain to keep monitor passthrough safe even on hot signal sources.
    private const float DefaultGain = 0.5f;

    public static void Passthrough()
    {
        AnsiConsole.MarkupLine("[bold]Duplex passthrough (input → gain → output) via AsioDevice[/]\n");
        PrintVolumeWarning();

        var driverName = AsioDeviceSelector.SelectDriver();
        if (driverName == null) return;

        using var device = AsioDevice.Open(driverName);

        if (device.Capabilities.NbInputChannels == 0 || device.Capabilities.NbOutputChannels == 0)
        {
            AnsiConsole.MarkupLine("[red]Driver must have both inputs and outputs for duplex mode.[/]");
            PressAnyKey();
            return;
        }

        var inputs = AsioDeviceSelector.SelectChannels(
            "Select input channel(s) to capture:",
            device.Capabilities.NbInputChannels);
        if (inputs == null) return;

        var outputs = AsioDeviceSelector.SelectChannels(
            $"Select {inputs.Length} output channel(s) (one-to-one with the inputs):",
            device.Capabilities.NbOutputChannels);
        if (outputs == null) return;

        if (outputs.Length != inputs.Length)
        {
            AnsiConsole.MarkupLine($"[red]Pick exactly {inputs.Length} output channel(s).[/]");
            PressAnyKey();
            return;
        }

        // Track peak per channel so we can show the user something useful while running.
        var peaks = new float[inputs.Length];

        try
        {
            device.InitDuplex(new AsioDuplexOptions
            {
                InputChannels = inputs,
                OutputChannels = outputs,
                SampleRate = device.CurrentSampleRate,
                Processor = (in AsioProcessBuffers b) =>
                {
                    int n = Math.Min(b.InputChannelCount, b.OutputChannelCount);
                    for (int ch = 0; ch < n; ch++)
                    {
                        var inSpan = b.GetInput(ch);
                        var outSpan = b.GetOutput(ch);
                        float peak = 0f;
                        for (int i = 0; i < b.Frames; i++)
                        {
                            float s = inSpan[i] * DefaultGain;
                            outSpan[i] = s;
                            float a = s < 0 ? -s : s;
                            if (a > peak) peak = a;
                        }
                        peaks[ch] = peak;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Init failed: {Markup.Escape(ex.Message)}[/]");
            PressAnyKey();
            return;
        }

        var completed = new ManualResetEventSlim();
        Exception? stopException = null;
        device.Stopped += (_, e) => { stopException = e.Exception; completed.Set(); };

        device.Start();
        AnsiConsole.MarkupLine($"\n[green]Duplex passthrough running[/] — {inputs.Length} channel(s)");
        AnsiConsole.MarkupLine($"[grey]Buffer: {device.FramesPerBuffer} frames, latency in/out: " +
                               $"{device.InputLatencySamples}/{device.OutputLatencySamples} frames @ {device.CurrentSampleRate} Hz[/]");
        AnsiConsole.MarkupLine("[dim]ESC to stop.[/]\n");

        // Reserve one row per channel for live peak display.
        int topRow = Console.CursorTop;
        for (int i = 0; i < inputs.Length; i++) Console.WriteLine();

        try
        {
            while (!completed.IsSet)
            {
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
                {
                    device.Stop();
                    break;
                }

                for (int ch = 0; ch < inputs.Length; ch++)
                {
                    float peak = peaks[ch];
                    float db = peak > 1e-6f ? 20f * MathF.Log10(peak) : -999f;
                    Console.SetCursorPosition(0, topRow + ch);
                    Console.Write($"  in {inputs[ch],2} → out {outputs[ch],2}: {BarFor(peak)} {db,7:0.0} dBFS   ");
                }
                Thread.Sleep(80);
            }
        }
        finally
        {
            Console.SetCursorPosition(0, Math.Min(topRow + inputs.Length, Console.BufferHeight - 1));
            Console.WriteLine();
        }

        completed.Wait(TimeSpan.FromSeconds(2));

        if (stopException != null)
            AnsiConsole.MarkupLine($"[red]Stopped with error: {Markup.Escape(stopException.Message)}[/]");
        else
            AnsiConsole.MarkupLine("[dim]Stopped cleanly.[/]");

        PressAnyKey();
    }

    private static string BarFor(float peak)
    {
        const int width = 16;
        int filled = Math.Min(width, (int)(peak * width));
        return new string('█', filled) + new string('░', width - filled);
    }

    private static void PrintVolumeWarning()
    {
        AnsiConsole.MarkupLine("[yellow]⚠ Duplex routes microphone/line input straight to the outputs.[/]");
        AnsiConsole.MarkupLine($"[yellow]  Gain is fixed at {DefaultGain:P0}. Watch for feedback if mic and speaker share a room.[/]\n");
    }

    private static void PressAnyKey()
    {
        AnsiConsole.MarkupLine("\n[dim]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }
}
