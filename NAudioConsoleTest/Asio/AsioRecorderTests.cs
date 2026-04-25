using NAudio.Wave;
using Spectre.Console;

namespace NAudioConsoleTest.Asio;

static class AsioRecorderTests
{
    public static void RecordToWav()
    {
        AnsiConsole.MarkupLine("[bold]Record to WAV via AsioDevice[/]\n");

        var driverName = AsioDeviceSelector.SelectDriver();
        if (driverName == null) return;

        using var device = AsioDevice.Open(driverName);
        if (device.Capabilities.NbInputChannels == 0)
        {
            AnsiConsole.MarkupLine("[red]This driver has no input channels.[/]");
            PressAnyKey();
            return;
        }

        var channels = AsioDeviceSelector.SelectChannels(
            "Select input channels to record:",
            device.Capabilities.NbInputChannels);
        if (channels == null) return;

        int sampleRate = device.CurrentSampleRate;
        AnsiConsole.MarkupLine($"[grey]Sample rate: {sampleRate} Hz (driver's current rate)[/]");

        int durationSec = AnsiConsole.Prompt(
            new TextPrompt<int>("Recording duration (seconds):").DefaultValue(10));

        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"NAudio_Asio_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        var filePath = AnsiConsole.Prompt(
            new TextPrompt<string>("Save to:").DefaultValue(defaultPath));

        try
        {
            device.InitRecording(new AsioRecordingOptions
            {
                InputChannels = channels,
                SampleRate = sampleRate
            });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Init failed: {Markup.Escape(ex.Message)}[/]");
            PressAnyKey();
            return;
        }

        var writer = new WaveFileWriter(
            filePath, WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels.Length));

        device.AudioCaptured += (_, e) =>
        {
            // Interleave per-channel spans into the WAV. The callback fires on the ASIO thread;
            // WaveFileWriter.WriteSample is not thread-safe but only this handler writes to it.
            var localWriter = writer;
            if (localWriter == null) return;
            for (int frame = 0; frame < e.Frames; frame++)
            {
                for (int ch = 0; ch < e.ChannelCount; ch++)
                {
                    localWriter.WriteSample(e.GetChannel(ch)[frame]);
                }
            }
        };

        var completed = new ManualResetEventSlim();
        Exception? stopException = null;
        device.Stopped += (_, e) => { stopException = e.Exception; completed.Set(); };

        device.Start();
        // Literal brackets must be doubled to escape Spectre markup parsing.
        AnsiConsole.MarkupLine($"\n[green]Recording[/] {channels.Length} channel(s): [[{string.Join(", ", channels)}]]");
        AnsiConsole.MarkupLine("[dim]ESC = stop early[/]\n");

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(durationSec);
        while (!completed.IsSet && DateTime.UtcNow < deadline)
        {
            if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
            {
                device.Stop();
                break;
            }
            var remaining = deadline - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            Console.Write($"\r  Remaining: {remaining:mm\\:ss\\.f}    ");
            Thread.Sleep(100);
        }
        Console.WriteLine();

        if (!completed.IsSet)
        {
            device.Stop();
            completed.Wait(TimeSpan.FromSeconds(2));
        }

        // Close the writer NOW, before printing the "Saved" message and waiting for a keypress —
        // otherwise the file stays locked while the user is reading the result.
        writer.Dispose();

        if (stopException != null)
            AnsiConsole.MarkupLine($"[red]Stopped with error: {Markup.Escape(stopException.Message)}[/]");

        AnsiConsole.MarkupLine($"[green]Saved:[/] {Markup.Escape(filePath)}");
        AnsiConsole.MarkupLine($"[grey]Size: {new FileInfo(filePath).Length / 1024}KB[/]");
        PressAnyKey();
    }

    public static void ShowChannelLevels()
    {
        AnsiConsole.MarkupLine("[bold]Show per-channel RMS levels (no WAV saved)[/]\n");
        AnsiConsole.MarkupLine("[grey]Useful for verifying which physical channel each InputChannels entry maps to.[/]\n");

        var driverName = AsioDeviceSelector.SelectDriver();
        if (driverName == null) return;

        using var device = AsioDevice.Open(driverName);
        if (device.Capabilities.NbInputChannels == 0)
        {
            AnsiConsole.MarkupLine("[red]This driver has no input channels.[/]");
            PressAnyKey();
            return;
        }

        var channels = AsioDeviceSelector.SelectChannels(
            "Select input channels to monitor:",
            device.Capabilities.NbInputChannels);
        if (channels == null) return;

        try
        {
            device.InitRecording(new AsioRecordingOptions
            {
                InputChannels = channels,
                SampleRate = device.CurrentSampleRate
            });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Init failed: {Markup.Escape(ex.Message)}[/]");
            PressAnyKey();
            return;
        }

        // Shared level storage, updated on the callback thread, read from the UI thread.
        var levels = new float[channels.Length];
        device.AudioCaptured += (_, e) =>
        {
            for (int ch = 0; ch < e.ChannelCount; ch++)
            {
                var span = e.GetChannel(ch);
                double sumSq = 0;
                for (int i = 0; i < span.Length; i++) sumSq += span[i] * span[i];
                levels[ch] = (float)Math.Sqrt(sumSq / span.Length);
            }
        };

        device.Start();
        AnsiConsole.MarkupLine("[dim]ESC to stop.[/]\n");

        // Reserve one line per channel, then keep repositioning to the same rows.
        int topRow = Console.CursorTop;
        for (int i = 0; i < channels.Length; i++) Console.WriteLine();

        try
        {
            while (true)
            {
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
                    break;

                for (int ch = 0; ch < channels.Length; ch++)
                {
                    float rms = levels[ch];
                    float db = rms > 1e-6f ? 20f * MathF.Log10(rms) : -999f;
                    Console.SetCursorPosition(0, topRow + ch);
                    Console.Write($"  ch{channels[ch],2}: {BarFor(rms)} {db,7:0.0} dBFS   ");
                }
                Thread.Sleep(100);
            }
        }
        finally
        {
            // Move below the meter lines before continuing output.
            Console.SetCursorPosition(0, Math.Min(topRow + channels.Length, Console.BufferHeight - 1));
            Console.WriteLine();
            device.Stop();
        }

        PressAnyKey();
    }

    public static void StopFromCallbackGuard()
    {
        AnsiConsole.MarkupLine("[bold]Regression test: Stop() from inside AudioCaptured handler[/]\n");
        AnsiConsole.MarkupLine("[grey]Phase 0 F1 — calling Stop() on the ASIO callback thread would self-deadlock.[/]");
        AnsiConsole.MarkupLine("[grey]The new AsioDevice must throw InvalidOperationException loudly instead.[/]\n");

        var driverName = AsioDeviceSelector.SelectDriver();
        if (driverName == null) return;

        using var device = AsioDevice.Open(driverName);
        if (device.Capabilities.NbInputChannels == 0)
        {
            AnsiConsole.MarkupLine("[red]This driver has no input channels.[/]");
            PressAnyKey();
            return;
        }

        device.InitRecording(new AsioRecordingOptions
        {
            InputChannels = [0],
            SampleRate = device.CurrentSampleRate
        });

        bool sawGuard = false;
        Exception? actualException = null;
        var fired = new ManualResetEventSlim();

        device.AudioCaptured += (_, _) =>
        {
            if (fired.IsSet) return;
            fired.Set();
            try
            {
                device.Stop();          // must throw immediately
            }
            catch (InvalidOperationException)
            {
                sawGuard = true;
            }
            catch (Exception ex)
            {
                actualException = ex;
            }
        };

        device.Start();
        fired.Wait(TimeSpan.FromSeconds(3));
        Thread.Sleep(100);              // let one more callback run to make sure guard held
        device.Stop();

        if (sawGuard)
            AnsiConsole.MarkupLine("[green]Test passed — Stop() from callback threw InvalidOperationException.[/]");
        else if (actualException != null)
            AnsiConsole.MarkupLine($"[red]Threw unexpected exception: {actualException.GetType().Name}: {Markup.Escape(actualException.Message)}[/]");
        else if (!fired.IsSet)
            AnsiConsole.MarkupLine("[red]No callback fired — test inconclusive (is the driver producing audio?).[/]");
        else
            AnsiConsole.MarkupLine("[red]Test FAILED — Stop() did not throw (potential deadlock hazard).[/]");

        PressAnyKey();
    }

    private static string BarFor(float rms)
    {
        const int width = 16;
        int filled = Math.Min(width, (int)(rms * 4 * width));
        return new string('█', filled) + new string('░', width - filled);
    }

    private static void PressAnyKey()
    {
        AnsiConsole.MarkupLine("\n[dim]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }
}
