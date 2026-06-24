using System.Text;
using System.Text.Json;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Serialises <see cref="HostDiagnostics"/> to JSON or markdown. JSON is the canonical machine
/// form (used for batch report headers); markdown is for humans skimming a fresh run.
/// </summary>
public static class DiagnosticsRenderer
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string ToJson(HostDiagnostics d) => JsonSerializer.Serialize(d, JsonOptions);

    public static string ToMarkdown(HostDiagnostics d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Host audio diagnostics");
        sb.AppendLine();
        sb.AppendLine($"_Collected {d.TimestampUtc}_");
        sb.AppendLine();

        sb.AppendLine("## OS");
        sb.AppendLine($"- Description: {d.Os.OsDescription}");
        sb.AppendLine($"- Architecture: {d.Os.Architecture}");
        sb.AppendLine($"- Runtime: {d.Os.RuntimeVersion}");
        sb.AppendLine($"- Machine: {d.Os.MachineName}");
        sb.AppendLine();

        sb.AppendLine("## ASIO drivers");
        if (d.Asio.Error is not null) sb.AppendLine($"_Error: {d.Asio.Error}_");
        else if (d.Asio.Drivers.Count == 0) sb.AppendLine("_None installed._");
        else foreach (var name in d.Asio.Drivers) sb.AppendLine($"- {name}");
        sb.AppendLine();

        sb.AppendLine("## WASAPI endpoints");
        if (d.Wasapi.Error is not null)
        {
            sb.AppendLine($"_Error: {d.Wasapi.Error}_");
        }
        else
        {
            foreach (var e in d.Wasapi.Endpoints)
            {
                var marker = e.Id == d.Wasapi.DefaultRender ? " **(default render)**"
                    : e.Id == d.Wasapi.DefaultCapture ? " **(default capture)**" : "";
                sb.AppendLine($"- [{e.DataFlow}] {e.Name}{marker}");
            }
            if (d.Wasapi.Endpoints.Count == 0) sb.AppendLine("_No active endpoints._");
        }
        sb.AppendLine();

        sb.AppendLine("## WinMM devices");
        sb.AppendLine($"**WaveOut ({d.WinMm.WaveOut.Count}):**");
        foreach (var n in d.WinMm.WaveOut) sb.AppendLine($"- {n}");
        sb.AppendLine($"**WaveIn ({d.WinMm.WaveIn.Count}):**");
        foreach (var n in d.WinMm.WaveIn) sb.AppendLine($"- {n}");
        sb.AppendLine();

        sb.AppendLine("## DirectSound devices");
        if (d.DirectSound.Error is not null) sb.AppendLine($"_Error: {d.DirectSound.Error}_");
        else foreach (var ds in d.DirectSound.Devices) sb.AppendLine($"- {ds.Description}");
        sb.AppendLine();

        sb.AppendLine("## NAudio assemblies");
        foreach (var kv in d.NAudio.AssemblyVersions) sb.AppendLine($"- {kv.Key} {kv.Value}");

        return sb.ToString();
    }
}
