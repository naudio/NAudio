using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NAudio.Vst3;

/// <summary>
/// A VST 3 module discovered on disk but not yet loaded. Cheap to enumerate; call
/// <see cref="Vst3Module.Load(string)"/> with <see cref="Path"/> to actually open the module.
/// </summary>
/// <param name="Path">Absolute path to the <c>.vst3</c> entry (either a bundle folder or a flat
/// DLL — both shapes are accepted by <see cref="Vst3Module.Load(string)"/>).</param>
/// <param name="Name">Display name (file name without the <c>.vst3</c> extension).</param>
public sealed record Vst3ModuleInfo(string Path, string Name);

/// <summary>
/// Scans the standard VST 3 plug-in folders for installed modules.
/// </summary>
/// <remarks>
/// <para>
/// Phase 1 surface — pure file-system enumeration. The scanner does <em>not</em> load any of the
/// discovered modules, so it's safe to call at startup against a large plug-in collection without
/// paying native-load costs.
/// </para>
/// <para>
/// The default search paths follow Steinberg's documented locations
/// (<a href="https://steinbergmedia.github.io/vst3_dev_portal/pages/Technical+Documentation/Locations+Format/Plugin+Locations.html">Plugin Locations</a>):
/// </para>
/// <list type="bullet">
///   <item><description><c>%CommonProgramFiles%\VST3</c> — system-wide</description></item>
///   <item><description><c>%LOCALAPPDATA%\Programs\Common\VST3</c> — per-user</description></item>
/// </list>
/// </remarks>
public static class Vst3PluginScanner
{
    private static readonly IReadOnlyList<string> _defaultSearchPaths = ComputeDefaultSearchPaths();

    /// <summary>
    /// The standard VST 3 search paths used by <see cref="EnumerateInstalled"/>. Exposed so
    /// callers can display them or pass them to <see cref="EnumerateIn(string)"/> individually.
    /// </summary>
    public static IReadOnlyList<string> DefaultSearchPaths => _defaultSearchPaths;

    /// <summary>
    /// Enumerates every <c>.vst3</c> entry across the <see cref="DefaultSearchPaths"/>. Results
    /// are ordered case-insensitively by name within each folder; entries that appear in more
    /// than one folder are returned multiple times — callers that want a unique list should
    /// deduplicate by <see cref="Vst3ModuleInfo.Name"/>.
    /// </summary>
    public static IReadOnlyList<Vst3ModuleInfo> EnumerateInstalled()
    {
        var result = new List<Vst3ModuleInfo>();
        foreach (var folder in _defaultSearchPaths)
        {
            result.AddRange(EnumerateIn(folder));
        }
        return result;
    }

    /// <summary>
    /// Enumerates the <c>.vst3</c> entries in a folder and any of its sub-folders, treating each
    /// <c>.vst3</c> entry as a leaf (we never descend into a bundle, even though the bundle is
    /// itself a directory). Vendor sub-folders like <c>VST3\iZotope\Ozone 11.vst3</c> and
    /// <c>VST3\Line 6\Helix Native.vst3</c> are part of the VST 3 spec, so a flat
    /// top-level-only scan would miss them.
    /// </summary>
    /// <remarks>
    /// Returns an empty list if the folder does not exist. I/O errors on individual sub-folders
    /// (e.g. access-denied) are silently skipped so a single permission glitch can't blackhole the
    /// whole scan.
    /// </remarks>
    public static IReadOnlyList<Vst3ModuleInfo> EnumerateIn(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        if (!Directory.Exists(folder))
        {
            return Array.Empty<Vst3ModuleInfo>();
        }

        var results = new List<Vst3ModuleInfo>();
        EnumerateRecursive(folder, results);
        results.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    // VST 3 trees are shallow (vendor/plug-in nesting is one or two levels). This cap exists only to
    // stop a pathological directory junction / symlink cycle from recursing forever into an
    // uncatchable StackOverflowException — real installs never approach it.
    private const int MaxScanDepth = 16;

    private static void EnumerateRecursive(string folder, List<Vst3ModuleInfo> results, int depth = 0)
    {
        if (depth > MaxScanDepth)
        {
            return;
        }

        string[] entries;
        try
        {
            entries = Directory.GetFileSystemEntries(folder, "*.vst3");
        }
        catch (UnauthorizedAccessException) { return; }
        catch (DirectoryNotFoundException) { return; }

        foreach (var entry in entries)
        {
            results.Add(new Vst3ModuleInfo(entry, System.IO.Path.GetFileNameWithoutExtension(entry)));
        }

        string[] subDirs;
        try
        {
            subDirs = Directory.GetDirectories(folder);
        }
        catch (UnauthorizedAccessException) { return; }
        catch (DirectoryNotFoundException) { return; }

        foreach (var sub in subDirs)
        {
            // Don't descend into .vst3 bundles — they're leaves to us; the DLL inside them is
            // also named .vst3 and would double-count.
            if (System.IO.Path.GetExtension(sub).Equals(".vst3", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            EnumerateRecursive(sub, results, depth + 1);
        }
    }

    private static IReadOnlyList<string> ComputeDefaultSearchPaths()
    {
        return new[]
        {
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                "VST3"),
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Common", "VST3"),
        };
    }
}
