using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.Asio;

static class AsioPlayerTests
{
    // Hard cap on all audio played by this test harness. ASIO bypasses the Windows mixer, so the
    // driver's own panel volume is the only attenuation the user has — this safety factor keeps
    // levels sane even if the driver is set to unity gain.
    private const float SafetyVolume = 0.25f;

    public static void PlayAudioFile()
    {
        AnsiConsole.MarkupLine("[bold]Play Audio File via AsioDevice[/]\n");
        PrintVolumeWarning();

        var driverName = AsioDeviceSelector.SelectDriver();
        if (driverName == null) return;

        using var device = AsioDevice.Open(driverName);
        var filePath = AudioFileSelector.SelectAudioFile();
        if (filePath == null) return;

        using var reader = new MediaFoundationReader(filePath);
        int sourceChannels = reader.WaveFormat.Channels;

        if (device.Capabilities.NbOutputChannels < sourceChannels)
        {
            AnsiConsole.MarkupLine($"[red]Driver has {device.Capabilities.NbOutputChannels} outputs but the file needs {sourceChannels}.[/]");
            PressAnyKey();
            return;
        }

        var channels = AsioDeviceSelector.SelectChannels(
            $"Select {sourceChannels} output channel(s):",
            device.Capabilities.NbOutputChannels);
        if (channels == null) return;
        if (channels.Length != sourceChannels)
        {
            AnsiConsole.MarkupLine($"[red]Pick exactly {sourceChannels} channel(s).[/]");
            PressAnyKey();
            return;
        }

        if (!device.IsSampleRateSupported(reader.WaveFormat.SampleRate))
        {
            AnsiConsole.MarkupLine($"[red]Driver does not support {reader.WaveFormat.SampleRate} Hz (file's native rate).[/]");
            PressAnyKey();
            return;
        }

        // Apply the safety attenuation — convert to samples, scale, then back to IWaveProvider.
        var safeSource = new VolumeSampleProvider(reader.ToSampleProvider()) { Volume = SafetyVolume };

        try
        {
            device.InitPlayback(new AsioPlaybackOptions
            {
                Source = safeSource.ToWaveProvider(),
                OutputChannels = channels,
                AutoStopOnEndOfStream = true
            });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Init failed: {Markup.Escape(ex.Message)}[/]");
            PressAnyKey();
            return;
        }

        RunPlayback(device, $"{Path.GetFileName(filePath)} → channels [{string.Join(", ", channels)}]");
    }

    public static void PlayShortTestTone()
    {
        AnsiConsole.MarkupLine("[bold]Play Short Test Tone (quiet 440Hz sine, 2s)[/]\n");
        PrintVolumeWarning();

        var driverName = AsioDeviceSelector.SelectDriver();
        if (driverName == null) return;

        using var device = AsioDevice.Open(driverName);
        if (device.Capabilities.NbOutputChannels == 0)
        {
            AnsiConsole.MarkupLine("[red]Driver has no output channels.[/]");
            PressAnyKey();
            return;
        }

        var channels = AsioDeviceSelector.SelectChannels(
            "Select output channel(s) for the test tone:",
            device.Capabilities.NbOutputChannels);
        if (channels == null) return;

        int sampleRate = device.CurrentSampleRate;
        // Very low amplitude — the sine is a continuous tone so even 0.1 is loud through monitors.
        var tone = new SineWaveSource(frequency: 440f, amplitude: 0.05f,
            sampleRate: sampleRate, channels: channels.Length);
        var limited = new OffsetSampleProvider(tone) { Take = TimeSpan.FromSeconds(2) };

        try
        {
            device.InitPlayback(new AsioPlaybackOptions
            {
                Source = limited.ToWaveProvider(),
                OutputChannels = channels,
                AutoStopOnEndOfStream = true
            });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Init failed: {Markup.Escape(ex.Message)}[/]");
            PressAnyKey();
            return;
        }

        RunPlayback(device, $"440Hz sine (2s, amp 0.05) → channels [{string.Join(", ", channels)}]");
    }

    public static void DisposeFromStoppedHandler()
    {
        AnsiConsole.MarkupLine("[bold]Regression test: Dispose() from inside Stopped handler[/]\n");
        AnsiConsole.MarkupLine("[grey]Plays a 1s silent stream, then Dispose()s the device from the Stopped handler.[/]");
        AnsiConsole.MarkupLine("[grey]Historical bug (Phase 0 F1): would self-deadlock or crash. Should complete cleanly.[/]\n");

        var driverName = AsioDeviceSelector.SelectDriver();
        if (driverName == null) return;

        var device = AsioDevice.Open(driverName);
        int sampleRate = device.CurrentSampleRate;

        var silent = new SilenceProvider(
            WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1)).ToSampleProvider();
        var limited = new OffsetSampleProvider(silent) { Take = TimeSpan.FromSeconds(1) };

        device.InitPlayback(new AsioPlaybackOptions
        {
            Source = limited.ToWaveProvider(),
            OutputChannels = [0],
            AutoStopOnEndOfStream = true
        });

        var completed = new ManualResetEventSlim();
        Exception? handlerException = null;

        device.Stopped += (_, e) =>
        {
            try
            {
                device.Dispose();
                AnsiConsole.MarkupLine("[green]Stopped handler disposed device cleanly.[/]");
            }
            catch (Exception ex)
            {
                handlerException = ex;
            }
            completed.Set();
        };

        device.Start();
        if (!completed.Wait(TimeSpan.FromSeconds(5)))
            AnsiConsole.MarkupLine("[red]TIMED OUT — Stopped handler never fired (possible deadlock).[/]");
        else if (handlerException != null)
            AnsiConsole.MarkupLine($"[red]Handler threw: {Markup.Escape(handlerException.Message)}[/]");
        else
            AnsiConsole.MarkupLine("[green]Test passed.[/]");

        PressAnyKey();
    }

    private static void RunPlayback(AsioDevice device, string description)
    {
        var completed = new ManualResetEventSlim();
        Exception? stopException = null;
        device.Stopped += (_, e) =>
        {
            stopException = e.Exception;
            completed.Set();
        };

        device.Start();

        AnsiConsole.MarkupLine($"[green]Playing:[/] {Markup.Escape(description)}");
        AnsiConsole.MarkupLine($"[grey]Buffer:[/] {device.FramesPerBuffer} frames, " +
                               $"output latency {device.OutputLatencySamples} frames");
        AnsiConsole.MarkupLine("[dim]ESC = stop early[/]\n");

        while (!completed.IsSet)
        {
            if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
            {
                device.Stop();
                break;
            }
            Thread.Sleep(50);
        }

        completed.Wait(TimeSpan.FromSeconds(2));

        if (stopException != null)
            AnsiConsole.MarkupLine($"[red]Stopped with error: {Markup.Escape(stopException.Message)}[/]");
        else
            AnsiConsole.MarkupLine("[dim]Stopped cleanly.[/]");

        PressAnyKey();
    }

    private static void PrintVolumeWarning()
    {
        AnsiConsole.MarkupLine("[yellow]⚠ ASIO bypasses the Windows volume mixer. Check your driver's output[/]");
        AnsiConsole.MarkupLine("[yellow]  gain and monitor volume BEFORE playing. Levels are attenuated to[/]");
        AnsiConsole.MarkupLine($"[yellow]  {SafetyVolume:P0} of source for files, and test tones use amplitude 0.05.[/]\n");
    }

    private static void PressAnyKey()
    {
        AnsiConsole.MarkupLine("\n[dim]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }
}
