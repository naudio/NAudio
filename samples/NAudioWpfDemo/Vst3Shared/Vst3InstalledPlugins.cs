using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NAudio.Vst3;

namespace NAudioWpfDemo.Vst3Shared;

/// <summary>
/// One picker entry: a specific class inside a discovered VST 3 module. Use
/// <see cref="Class"/>.<c>IsEffect</c> / <c>IsInstrument</c> / <c>Kind</c> to filter.
/// </summary>
sealed class Vst3InstalledPlugin
{
    public Vst3InstalledPlugin(Vst3ModuleInfo module, Vst3ClassInfo classInfo)
    {
        Module = module;
        Class = classInfo;
        // When the two names differ they're almost always the same plug-in with a cosmetic
        // suffix on one side ("(64 Bit)", "(x64)", "FX", "Plugin"...) — the shorter form is
        // the cleaner one to surface.
        Display = string.Equals(module.Name, classInfo.Name, StringComparison.OrdinalIgnoreCase)
            ? module.Name
            : (classInfo.Name.Length < module.Name.Length ? classInfo.Name : module.Name);
    }
    public Vst3ModuleInfo Module { get; }
    public Vst3ClassInfo Class { get; }
    public string Display { get; }
    public override string ToString() => Display;
}

/// <summary>
/// Process-wide, disk-cached catalogue of every installed VST 3 plug-in class. The first
/// caller loads the cache from disk if present, otherwise scans the install folders and
/// writes the cache. Subsequent app launches reuse the cached list — typically a few
/// hundred microseconds vs. several seconds for a full re-scan on systems with many
/// plug-ins. <see cref="RescanAsync"/> forces a fresh scan and overwrites the cache (the
/// UI surfaces a Rescan button so the user can pick up newly-installed plug-ins).
/// </summary>
/// <remarks>
/// Cache format is JSON; location is <c>%TEMP%\NAudio.Vst3.Catalog.json</c> — exposed
/// via <see cref="CacheFilePath"/> so a status line can show it and the user can find or
/// delete it. Entries whose <c>ModulePath</c> no longer exists on disk are silently
/// dropped at load time, so an uninstall plus restart leaves a tidy list without
/// requiring an explicit rescan; a new install does still need one.
/// </remarks>
static class Vst3InstalledPlugins
{
    private const int CacheVersion = 1;

    private static readonly object syncRoot = new();
    private static IReadOnlyList<Vst3InstalledPlugin> cached;
    private static Task<IReadOnlyList<Vst3InstalledPlugin>> inflight;

    /// <summary>Location of the JSON cache file on disk.</summary>
    public static string CacheFilePath { get; } =
        Path.Combine(Path.GetTempPath(), "NAudio.Vst3.Catalog.json");

    /// <summary>Returns the catalogue, using the in-memory cache, then the disk cache,
    /// then a live scan in that order. Concurrent callers share an in-flight scan.</summary>
    public static Task<IReadOnlyList<Vst3InstalledPlugin>> GetAsync()
    {
        lock (syncRoot)
        {
            if (cached != null) return Task.FromResult(cached);
            if (inflight != null) return inflight;
            inflight = Task.Run<IReadOnlyList<Vst3InstalledPlugin>>(() =>
            {
                var list = LoadFromDisk() ?? ScanAndSave();
                lock (syncRoot) { cached = list; inflight = null; }
                return list;
            });
            return inflight;
        }
    }

    /// <summary>Forces a fresh scan and rewrites the disk cache. Use this when the user
    /// has installed or uninstalled plug-ins and wants the picker to pick them up.</summary>
    public static Task<IReadOnlyList<Vst3InstalledPlugin>> RescanAsync()
    {
        lock (syncRoot)
        {
            cached = null;
            inflight = Task.Run<IReadOnlyList<Vst3InstalledPlugin>>(() =>
            {
                var list = ScanAndSave();
                lock (syncRoot) { cached = list; inflight = null; }
                return list;
            });
            return inflight;
        }
    }

    private static IReadOnlyList<Vst3InstalledPlugin> LoadFromDisk()
    {
        try
        {
            if (!File.Exists(CacheFilePath)) return null;
            using var stream = File.OpenRead(CacheFilePath);
            var doc = JsonSerializer.Deserialize<CacheFile>(stream);
            if (doc == null || doc.Version != CacheVersion || doc.Plugins == null) return null;

            var result = new List<Vst3InstalledPlugin>(doc.Plugins.Count);
            foreach (var entry in doc.Plugins)
            {
                // Drop entries pointing at modules that have been uninstalled since the scan.
                // VST 3 modules on Windows can be either a single .vst3 file (e.g. Raum,
                // Reaktor) or a bundle directory containing the binary at
                // <bundle>/Contents/x86_64-win/<name>.vst3 (e.g. Pianoteq, Surge XT). Path.Exists
                // accepts either; File.Exists alone silently drops every bundle-style plug-in.
                if (entry.ModulePath == null || !Path.Exists(entry.ModulePath)) continue;
                var info = new Vst3ModuleInfo(entry.ModulePath, entry.ModuleName ?? string.Empty);
                var cls = new Vst3ClassInfo(
                    entry.ClassId ?? string.Empty,
                    entry.Category ?? string.Empty,
                    entry.Name ?? string.Empty,
                    entry.Vendor ?? string.Empty,
                    entry.Version ?? string.Empty,
                    entry.SdkVersion ?? string.Empty,
                    entry.SubCategories ?? string.Empty);
                result.Add(new Vst3InstalledPlugin(info, cls));
            }
            return result.OrderBy(p => p.Display, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch
        {
            // Corrupt cache file is not a fatal — fall through to a fresh scan.
            return null;
        }
    }

    private static IReadOnlyList<Vst3InstalledPlugin> ScanAndSave()
    {
        var result = Scan();
        try
        {
            var doc = new CacheFile
            {
                Version = CacheVersion,
                Plugins = result.Select(p => new CacheEntry
                {
                    ModulePath = p.Module.Path,
                    ModuleName = p.Module.Name,
                    ClassId = p.Class.ClassId,
                    Category = p.Class.Category,
                    Name = p.Class.Name,
                    Vendor = p.Class.Vendor,
                    Version = p.Class.Version,
                    SdkVersion = p.Class.SdkVersion,
                    SubCategories = p.Class.SubCategories,
                }).ToList(),
            };
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);
            var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CacheFilePath, json);
        }
        catch
        {
            // Disk write failure is not fatal — caller still gets the live results.
        }
        return result;
    }

    private static IReadOnlyList<Vst3InstalledPlugin> Scan()
    {
        var result = new List<Vst3InstalledPlugin>();
        try
        {
            foreach (var info in Vst3PluginScanner.EnumerateInstalled())
            {
                Vst3Module module = null;
                try
                {
                    module = Vst3Module.Load(info.Path);
                    foreach (var cls in module.GetClasses())
                    {
                        // Only audio-module classes are useful to the demos (drops factory
                        // 'Component' entries and the like).
                        if (cls.IsAudioModule)
                            result.Add(new Vst3InstalledPlugin(info, cls));
                    }
                }
                catch
                {
                    // Skip plug-ins that fail to enumerate — better than aborting the whole scan.
                }
                finally
                {
                    module?.Dispose();
                }
            }
        }
        catch
        {
            // Scanner-level failure (e.g. no install folders) leaves the list empty.
        }
        return result.OrderBy(p => p.Display, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // JSON DTOs. Public for the serializer; not API.
    public sealed class CacheFile
    {
        public int Version { get; set; }
        public List<CacheEntry> Plugins { get; set; }
    }

    public sealed class CacheEntry
    {
        public string ModulePath { get; set; }
        public string ModuleName { get; set; }
        public string ClassId { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Vendor { get; set; }
        public string Version { get; set; }
        public string SdkVersion { get; set; }
        public string SubCategories { get; set; }
    }
}
