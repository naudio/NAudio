using NAudio.CoreAudioApi;
using NAudio.Wasapi;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi;

static class RecorderTests
{
    public static void RecordAndPlayback()
    {
        AnsiConsole.MarkupLine("[bold]Record and Playback[/]\n");

        var captureDevice = DeviceSelector.SelectCaptureDevice();
        if (captureDevice == null) return;

        var duration = TimeSpan.FromSeconds(15);
        AnsiConsole.MarkupLine($"[grey]Will record for {duration.TotalSeconds}s then play back[/]\n");

        // Capture to memory
        using var recorder = new WasapiRecorderBuilder()
            .WithDevice(captureDevice)
            .WithSharedMode()
            .WithEventSync()
            .Build();

        var capturedData = new MemoryStream();
        var waveFormat = recorder.WaveFormat;

        recorder.DataAvailable += (buffer, flags) =>
        {
            if ((flags & AudioClientBufferFlags.Silent) == 0)
            {
                // Copy Span to stream (we need to keep the data beyond the callback)
                var array = buffer.ToArray();
                capturedData.Write(array, 0, array.Length);
            }
        };

        recorder.StartRecording();
        var completed = RecordingMonitor.MonitorWithCountdown(
            captureDevice.FriendlyName, duration, recorder.StopRecording);

        if (!completed)
        {
            AnsiConsole.MarkupLine("[dim]Recording cancelled, no playback[/]");
            return;
        }

        var capturedBytes = capturedData.ToArray();
        AnsiConsole.MarkupLine($"[grey]Captured {capturedBytes.Length / 1024}KB of audio ({waveFormat})[/]\n");

        if (capturedBytes.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No audio data captured[/]");
            AnsiConsole.MarkupLine("[dim]Press any key...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        // Play back
        AnsiConsole.MarkupLine("[bold]Playing back captured audio...[/]\n");

        var renderDevice = GetDefaultRenderDevice();
        if (renderDevice == null) return;

        SetSafeVolume(renderDevice);

        using var player = new WasapiPlayerBuilder()
            .WithDevice(renderDevice)
            .WithSharedMode()
            .WithEventSync()
            .Build();

        var rawStream = new RawSourceWaveStream(new MemoryStream(capturedBytes), waveFormat);
        player.Init(rawStream);
        player.Play();
        PlaybackMonitor.Monitor(player, renderDevice.FriendlyName, "Captured audio playback");
    }

    public static void RecordToWavFile()
    {
        AnsiConsole.MarkupLine("[bold]Record to WAV File[/]\n");

        var captureDevice = DeviceSelector.SelectCaptureDevice();
        if (captureDevice == null) return;

        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"NAudio_Recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

        var filePath = AnsiConsole.Prompt(
            new TextPrompt<string>("Save to:").DefaultValue(defaultPath));

        using var recorder = new WasapiRecorderBuilder()
            .WithDevice(captureDevice)
            .WithSharedMode()
            .WithEventSync()
            .Build();

        var writer = new WaveFileWriter(filePath, recorder.WaveFormat);
        var duration = TimeSpan.Zero;

        recorder.DataAvailable += (buffer, flags) =>
        {
            if ((flags & AudioClientBufferFlags.Silent) == 0)
            {
                var array = buffer.ToArray();
                writer.Write(array, 0, array.Length);
            }
        };

        recorder.StartRecording();
        var completed = RecordingMonitor.MonitorUntilStopped(
            captureDevice.FriendlyName, recorder.StopRecording);

        // Close the writer NOW, before prompting/waiting — otherwise the file stays locked
        // while the user is reading the result or trying to open it externally.
        duration = writer.TotalTime;
        writer.Dispose();

        if (completed)
        {
            AnsiConsole.MarkupLine($"[green]Saved to: {Markup.Escape(filePath)}[/]");
            AnsiConsole.MarkupLine($"[grey]Size: {new FileInfo(filePath).Length / 1024}KB, Duration: {duration:mm\\:ss\\.f}[/]");
        }
        else
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            AnsiConsole.MarkupLine("[dim]Recording cancelled, file deleted[/]");
        }

        AnsiConsole.MarkupLine("[dim]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }

    private static MMDevice? GetDefaultRenderDevice()
    {
        using var enumerator = new MMDeviceEnumerator();
        if (enumerator.TryGetDefaultAudioEndpoint(DataFlow.Render, Role.Console, out var device))
            return device;
        AnsiConsole.MarkupLine("[red]No default render device found[/]");
        return null;
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
        }
        catch { }
    }
}
