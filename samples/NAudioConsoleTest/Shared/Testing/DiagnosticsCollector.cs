using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Snapshots the host's audio infrastructure: OS, .NET runtime, ASIO drivers, WASAPI endpoints,
/// WinMM devices, DirectSound devices, NAudio assembly versions. Doubles as the report header
/// for the batch runner. Enumeration only — never opens a device.
/// </summary>
public static class DiagnosticsCollector
{
    public static HostDiagnostics Collect() => new(
        TimestampUtc: DateTime.UtcNow.ToString("o"),
        Os: CollectOs(),
        Asio: CollectAsio(),
        Wasapi: CollectWasapi(),
        WinMm: CollectWinMm(),
        DirectSound: CollectDirectSound(),
        NAudio: CollectNAudio());

    private static OsInfo CollectOs() => new(
        OsDescription: RuntimeInformation.OSDescription,
        Architecture: RuntimeInformation.OSArchitecture.ToString(),
        RuntimeVersion: RuntimeInformation.FrameworkDescription,
        MachineName: Environment.MachineName);

    private static AsioInfo CollectAsio()
    {
        try
        {
            return new AsioInfo(AsioDevice.GetDriverNames());
        }
        catch (Exception ex)
        {
            return new AsioInfo([], Error: $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static WasapiInfo CollectWasapi()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active)
                .Select(d => new WasapiEndpoint(
                    Name: d.FriendlyName,
                    DataFlow: d.DataFlow.ToString(),
                    State: d.State.ToString(),
                    Id: d.ID))
                .ToList();

            string? defaultRender = null, defaultCapture = null;
            try { defaultRender = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID; } catch { }
            try { defaultCapture = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia).ID; } catch { }
            return new WasapiInfo(endpoints, defaultRender, defaultCapture);
        }
        catch (Exception ex)
        {
            return new WasapiInfo([], null, null, Error: $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static WinMmInfo CollectWinMm()
    {
        var outs = new List<string>();
        var ins = new List<string>();
        try
        {
            for (int i = 0; i < WaveOut.DeviceCount; i++)
                outs.Add(WaveOut.GetCapabilities(i).ProductName);
        }
        catch { /* leave partial */ }
        try
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
                ins.Add(WaveIn.GetCapabilities(i).ProductName);
        }
        catch { /* leave partial */ }
        return new WinMmInfo(outs, ins);
    }

    private static DirectSoundInfo CollectDirectSound()
    {
        try
        {
            return new DirectSoundInfo(DirectSoundOut.Devices
                .Select(d => new DirectSoundDevice(d.Guid.ToString(), d.Description ?? "", d.ModuleName ?? ""))
                .ToList());
        }
        catch (Exception ex)
        {
            return new DirectSoundInfo([], Error: $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static NAudioInfo CollectNAudio()
    {
        // Walk loaded assemblies — gives us whatever NAudio packages are actually wired into this build.
        var versions = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("NAudio", StringComparison.Ordinal) == true)
            .OrderBy(a => a.GetName().Name)
            .ToDictionary(
                a => a.GetName().Name!,
                a => a.GetName().Version?.ToString() ?? "(unknown)");
        return new NAudioInfo(versions);
    }
}

public sealed record HostDiagnostics(
    string TimestampUtc,
    OsInfo Os,
    AsioInfo Asio,
    WasapiInfo Wasapi,
    WinMmInfo WinMm,
    DirectSoundInfo DirectSound,
    NAudioInfo NAudio);

public sealed record OsInfo(string OsDescription, string Architecture, string RuntimeVersion, string MachineName);
public sealed record AsioInfo(IReadOnlyList<string> Drivers, string? Error = null);
public sealed record WasapiInfo(IReadOnlyList<WasapiEndpoint> Endpoints,
    string? DefaultRender, string? DefaultCapture, string? Error = null);
public sealed record WasapiEndpoint(string Name, string DataFlow, string State, string Id);
public sealed record WinMmInfo(IReadOnlyList<string> WaveOut, IReadOnlyList<string> WaveIn);
public sealed record DirectSoundInfo(IReadOnlyList<DirectSoundDevice> Devices, string? Error = null);
public sealed record DirectSoundDevice(string Guid, string Description, string Module);
public sealed record NAudioInfo(IReadOnlyDictionary<string, string> AssemblyVersions);
