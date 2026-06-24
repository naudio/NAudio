using System;
using System.IO;
using System.Text.Json;

namespace NAudioWpfDemo;

/// <summary>
/// Tiny persisted-settings layer for the demo app: the last-used ASIO driver, audio-input
/// selection (mono/stereo + base channel) and WinRT MIDI input device are remembered across
/// runs so the user doesn't keep re-picking the same combo each launch. Typical setups have
/// one ASIO interface + one keyboard wired up — auto-restoring those is a clear UX win.
/// </summary>
/// <remarks>
/// Stored as JSON at <c>%APPDATA%\NAudioWpfDemo\Settings.json</c>. Read once at first
/// access and written synchronously on every setter (the inputs are user-driven dropdowns,
/// not hot paths). Corrupt or missing files fall back to defaults silently.
/// </remarks>
internal static class DemoSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NAudioWpfDemo", "Settings.json");

    private static readonly SettingsData data = Load();

    public static string LastAsioDriver
    {
        get => data.LastAsioDriver;
        set { data.LastAsioDriver = value ?? string.Empty; Save(); }
    }

    public static int LastInputChannelsIndex
    {
        get => data.LastInputChannelsIndex;
        set { data.LastInputChannelsIndex = value; Save(); }
    }

    public static int LastInputChannelOffset
    {
        get => data.LastInputChannelOffset;
        set { data.LastInputChannelOffset = value; Save(); }
    }

    public static string LastMidiDeviceId
    {
        get => data.LastMidiDeviceId;
        set { data.LastMidiDeviceId = value ?? string.Empty; Save(); }
    }

    private static SettingsData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                using var stream = File.OpenRead(FilePath);
                return JsonSerializer.Deserialize<SettingsData>(stream) ?? new SettingsData();
            }
        }
        catch { /* corrupt file is not fatal — fall through to defaults */ }
        return new SettingsData();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { /* settings save failure is not fatal — user just re-picks next launch */ }
    }

    public sealed class SettingsData
    {
        public string LastAsioDriver { get; set; } = string.Empty;
        public int LastInputChannelsIndex { get; set; } = 1; // 0 = mono, 1 = stereo
        public int LastInputChannelOffset { get; set; } = 1;
        public string LastMidiDeviceId { get; set; } = string.Empty;
    }
}
