using System.Text.Json;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Remembers recently-used file paths per category, persisted to a small JSON file under the user's
/// local app-data folder. Backs the most-recently-used file picker in <see cref="FilePathPrompter"/>
/// so interactive testers can re-select a file instead of pasting a path every time.
/// </summary>
internal static class RecentFilesStore
{
    private const int MaxPerCategory = 8;

    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NAudioConsoleTest", "recent-files.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Returns the remembered paths for a category, most-recent first, filtered to files that still
    /// exist on disk (stale entries are silently dropped).
    /// </summary>
    public static IReadOnlyList<string> Get(string category)
    {
        var all = Load();
        if (!all.TryGetValue(category, out var list))
            return [];
        return list.Where(File.Exists).ToList();
    }

    /// <summary>
    /// Records <paramref name="path"/> as the most-recently-used file for the category, de-duplicated
    /// (case-insensitive) and capped. No-op for blank paths. Failures to persist are swallowed — the
    /// recent list is a convenience, never load-bearing.
    /// </summary>
    public static void Add(string category, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        string full;
        try { full = Path.GetFullPath(path); }
        catch { full = path; }

        var all = Load();
        if (!all.TryGetValue(category, out var list))
            all[category] = list = [];

        list.RemoveAll(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, full);
        if (list.Count > MaxPerCategory)
            list.RemoveRange(MaxPerCategory, list.Count - MaxPerCategory);

        Save(all);
    }

    private static Dictionary<string, List<string>> Load()
    {
        try
        {
            if (File.Exists(StorePath))
            {
                var json = File.ReadAllText(StorePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
                if (data is not null)
                    return data;
            }
        }
        catch
        {
            // Corrupt/unreadable store — start fresh rather than failing the test run.
        }
        return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }

    private static void Save(Dictionary<string, List<string>> data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            File.WriteAllText(StorePath, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch
        {
            // Best-effort persistence; ignore (e.g. read-only profile).
        }
    }
}
