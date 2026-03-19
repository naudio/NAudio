using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi;

static class PlayerTests
{
    public static void PlayFileShared() => PlayFile("Shared Mode", builder => builder.WithSharedMode());
    public static void PlayFileExclusive() => PlayFile("Exclusive Mode", builder => builder.WithExclusiveMode());
    public static void PlayFileLowLatency() => PlayFile("Low Latency (IAudioClient3)", builder => builder.WithSharedMode().WithLowLatency());

    private static void PlayFile(string modeDescription, Action<WasapiPlayerBuilder> configure)
    {
        AnsiConsole.MarkupLine($"[bold]Play Audio File — {modeDescription}[/]\n");

        var device = DeviceSelector.SelectRenderDevice();
        if (device == null) return;

        var filePath = AudioFileSelector.SelectAudioFile();
        if (filePath == null) return;

        SetSafeVolume(device);

        var builder = new WasapiPlayerBuilder()
            .WithDevice(device)
            .WithEventSync()
            .WithMmcssThreadPriority("Pro Audio");
        configure(builder);

        using var player = builder.Build();
        using var reader = new MediaFoundationReader(filePath);

        try
        {
            player.Init(reader);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Init failed: {Markup.Escape(ex.Message)}[/]");
            AnsiConsole.MarkupLine("[dim]Press any key...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        AnsiConsole.MarkupLine($"[grey]Output format: {player.OutputWaveFormat}[/]");

        player.Play();
        PlaybackMonitor.Monitor(player, device.FriendlyName, Path.GetFileName(filePath));
    }

    public static void PlaySineWave()
    {
        AnsiConsole.MarkupLine("[bold]Play Sine Wave[/]\n");

        var device = DeviceSelector.SelectRenderDevice();
        if (device == null) return;

        var frequency = AnsiConsole.Prompt(
            new TextPrompt<float>("Frequency (Hz):").DefaultValue(440f));
        var durationSec = AnsiConsole.Prompt(
            new TextPrompt<float>("Duration (seconds):").DefaultValue(5f));

        SetSafeVolume(device);

        using var player = new WasapiPlayerBuilder()
            .WithDevice(device)
            .WithSharedMode()
            .WithEventSync()
            .WithMmcssThreadPriority("Pro Audio")
            .Build();

        var sineSource = new SineWaveSource(frequency, amplitude: 0.25f);
        player.Init(sineSource);

        // Stop after duration
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSec));
        player.PlaybackStopped += (_, _) => { };

        player.Play();

        // Use a simpler monitor that auto-stops after duration
        var start = DateTime.UtcNow;
        var duration = TimeSpan.FromSeconds(durationSec);

        AnsiConsole.MarkupLine($"[bold green]Playing:[/] {frequency}Hz sine wave for {durationSec}s");
        AnsiConsole.MarkupLine($"[grey]Device:[/]  {Markup.Escape(device.FriendlyName)}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]  {player.OutputWaveFormat}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[dim]ESC = stop early[/]");
        AnsiConsole.MarkupLine("");

        while (player.PlaybackState != PlaybackState.Stopped && DateTime.UtcNow - start < duration)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape)
                {
                    player.Stop();
                    break;
                }
            }
            var elapsed = DateTime.UtcNow - start;
            var remaining = duration - elapsed;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            Console.Write($"\r  Remaining: {remaining:mm\\:ss\\.f}    ");
            Thread.Sleep(100);
        }

        player.Stop();
        Console.WriteLine();
        AnsiConsole.MarkupLine("[dim]Done[/]");
    }

    private static void SetSafeVolume(MMDevice device)
    {
        try
        {
            var vol = device.AudioEndpointVolume;
            if (vol.MasterVolumeLevelScalar > 0.5f)
            {
                vol.MasterVolumeLevelScalar = 0.5f;
                AnsiConsole.MarkupLine("[yellow]Volume capped at 50% for safety[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[dim]Volume: {vol.MasterVolumeLevelScalar:P0}[/]");
            }
        }
        catch
        {
            // Some devices don't support endpoint volume
        }
    }
}
